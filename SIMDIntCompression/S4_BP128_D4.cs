using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace SIMDIntCompression;

/// <summary> 
/// S4-BP128-D4 : 4 Integer SIMD - Binary Packing 128 ints/block - Delta of 4
/// 
/// Integrated Differential Coding and Bit Packing for SIMD-Friendly Inverted Indexes
/// 
/// <remarks>
/// This codec only works with blocks of 128 integers.
/// </remarks>
///
/// Reference:
/// Daniel Lemire, Leonid Boytsov, Nathan Kurz, SIMD Compression and the Intersection of Sorted Integers, Software: Practice and Experience 46 (6), 2016.
/// https://arxiv.org/abs/1401.6399
/// </summary>
public unsafe class S4_BP128_D4 : IIntCompressionCodec<uint>
{

    const uint MiniBlockSize = 128;
    const int HowManyMiniBlocks = 16;

    private static void PackBlock(uint* input, uint* output, uint bit, ref Vector128<uint> initOffset)
    {
        var nextOffset = Vector128.Load(input + 128 - 4);
        SimdBitPacking32D4.Pack(initOffset, input, (Vector128<uint>*)output, (int)bit);

        initOffset = nextOffset;
    }

    private static void UnpackBlock(uint* input, uint* output, int bit, ref Vector128<uint> initOffset) =>
        initOffset = SimdBitPacking32D4.Unpack(initOffset, (Vector128<uint>*)input, output, bit);

    /// <inheritdoc/>
    public static int BlockSize => (int)MiniBlockSize;

    /// <summary>
    /// Encode an array of integers into a byte array.
    /// 
    /// Prefer using <see cref="Encode(ReadOnlySpan{T}, Span{byte})"/> for safety.
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="length"></param>
    /// <param name="output"></param>
    /// <param name="nValue"=>number of bytes encoded</param>

    public static unsafe void Encode(uint* input, nint length, uint* output, out nint nValue)
    {
        // Check divisible by blockSize
        if (length % MiniBlockSize != 0)
            throw new ArgumentException("Input length must be divisible by BlockSize.");

        uint* initOutput = output;

        *output++ = (uint)length;

        Span<uint> bs = stackalloc uint[HowManyMiniBlocks];

        var init = Vector128<uint>.Zero;

        var final = input + length;

        for (; input + HowManyMiniBlocks * MiniBlockSize <= final; input += HowManyMiniBlocks * MiniBlockSize)
        {
            var tmpInit = init;
            for (var i = 0; i < HowManyMiniBlocks; i++)
                bs[i] = SimdBitPacking32D4.MaxBits(input + i * MiniBlockSize, ref tmpInit);

            // write bit widths to the output
            *output++ = (bs[0] << 24) | (bs[1] << 16) | (bs[2] << 8) | bs[3];
            *output++ = (bs[4] << 24) | (bs[5] << 16) | (bs[6] << 8) | bs[7];
            *output++ = (bs[8] << 24) | (bs[9] << 16) | (bs[10] << 8) | bs[11];
            *output++ = (bs[12] << 24) | (bs[13] << 16) | (bs[14] << 8) | bs[15];

            for (var i = 0; i < HowManyMiniBlocks; i++)
            {
                PackBlock(input + i * MiniBlockSize, output, bs[i], ref init);
                output += MiniBlockSize / 32 * bs[i];
            }
        }
        if (input < final)
        {
            var howMany = (final - input) / MiniBlockSize;
            var tmpInit = init;

            for (var i = 0; i < howMany; i++)
                bs[i] = SimdBitPacking32D4.MaxBits(input + i * MiniBlockSize, ref tmpInit);

            *output++ = (bs[0] << 24) | (bs[1] << 16) | (bs[2] << 8) | bs[3];
            *output++ = (bs[4] << 24) | (bs[5] << 16) | (bs[6] << 8) | bs[7];
            *output++ = (bs[8] << 24) | (bs[9] << 16) | (bs[10] << 8) | bs[11];
            *output++ = (bs[12] << 24) | (bs[13] << 16) | (bs[14] << 8) | bs[15];

            for (int i = 0; i < howMany; ++i)
            {
                PackBlock(input + i * MiniBlockSize, output, bs[i], ref init);
                output += MiniBlockSize / 32 * bs[i];
            }

            input += howMany * MiniBlockSize;

            Debug.Assert(input == final);
        }
        nValue = (nint)(output - initOutput);
    }

    /// <inheritdoc/>
    public static unsafe uint* Decode(uint* input, nint length, uint* output, out nint nValue)
    {
        var actualLength = *input++;

        var initOutput = output;

        Span<uint> bs = stackalloc uint[HowManyMiniBlocks];

        var init = Vector128<uint>.Zero;
        for (; output < initOutput + actualLength / (HowManyMiniBlocks * MiniBlockSize) * HowManyMiniBlocks * MiniBlockSize; output += HowManyMiniBlocks * MiniBlockSize)
        {
            for (var i = 0; i < 4; i++, input++)
            {
                bs[0 + 4 * i] = (byte)(input[0] >> 24);
                bs[1 + 4 * i] = (byte)(input[0] >> 16);
                bs[2 + 4 * i] = (byte)(input[0] >> 8);
                bs[3 + 4 * i] = (byte)input[0];
            }
            for (var i = 0; i < HowManyMiniBlocks; i++)
            {
                UnpackBlock(input, output + i * MiniBlockSize, (int)bs[i], ref init);
                input += MiniBlockSize / 32 * bs[i];
            }
        }
        if (output < initOutput + actualLength)
        {
            var howMany = (initOutput + actualLength - output) / MiniBlockSize;
            for (var i = 0; i < 4; i++, input++)
            {
                bs[0 + 4 * i] = (byte)(input[0] >> 24);
                bs[1 + 4 * i] = (byte)(input[0] >> 16);
                bs[2 + 4 * i] = (byte)(input[0] >> 8);
                bs[3 + 4 * i] = (byte)input[0];
            }
            for (var i = 0; i < howMany; i++)
            {
                UnpackBlock(input, output + i * MiniBlockSize, (int)bs[i], ref init);
                input += MiniBlockSize / 32 * bs[i];
            }
            output += howMany * MiniBlockSize;
            Debug.Assert(output == initOutput + actualLength);
        }
        nValue = (nint)(output - initOutput);
        return input;
    }

    /// <inheritdoc/>
    public static int Encode(ReadOnlySpan<uint> input, Span<byte> output)
    {
        fixed (uint* pInput = input)
        fixed (byte* pOutput = output)
        {
            Encode(pInput, input.Length, (uint*)pOutput, out var nValue);
            return (int)nValue * sizeof(uint);
        }
    }

    /// <summary>
    /// Decode a byte array into an array of integers.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <returns>number of bytes written, and read</returns>
    public static (int Written, int Read) DecodePart(ReadOnlySpan<byte> input, Span<uint> output)
    {
        fixed (byte* pInput = input)
        fixed (uint* pOutput = output)
        {
            var ptr = Decode((uint*)pInput, input.Length / sizeof(uint), pOutput, out var nValue);
            return ((int)nValue, (int)((byte*)ptr - pInput));
        }
    }

    /// <inheritdoc/>
    public static int Decode(ReadOnlySpan<byte> input, Span<uint> output)
    {
        var (w, r) = DecodePart(input, output);
        if (r != input.Length)
            throw new InvalidDataException("Length mismatch");

        return w;
    }

    /// <inheritdoc/>
    public static int GetMaxCompressedLength(ReadOnlySpan<uint> input) =>
        input.Length * sizeof(uint) + 1024; // Really should investigate this further.

    /// <inheritdoc/>
    public static int GetDecompressedLength(ReadOnlySpan<byte> input) =>
        // the first 4 bytes are the length
        (int)MemoryMarshal.Read<uint>(input);
}