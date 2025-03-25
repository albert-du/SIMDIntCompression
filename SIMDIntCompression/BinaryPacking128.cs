
using System.Runtime.CompilerServices;

namespace SIMDIntCompression;

/// <summary>
/// Binary Packing 128 with StreamVByte as the fallback encoding.
/// 
/// Compatible with signed and unsigned integers.
/// </summary>
public sealed class BinaryPacking128 : CompositeCodec<uint, S4_BP128_D4, StreamVByte_D1>, IIntCompressionCodec<int>
{
    /// <summary>
    /// Decode a byte array into a span of signed int32.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public static int Decode(ReadOnlySpan<byte> input, Span<int> output) =>
        // call unsigned decode and then cast to int
        Decode(input, Unsafe.BitCast<Span<int>, Span<uint>>(output));

    /// <summary>
    /// Encode a span of signed int32 into a byte array.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static int Encode(ReadOnlySpan<int> input, Span<byte> output)
    {
        if (!SpanNonNegative(input))
            throw new ArgumentException("Input must be non-negative.");

        // call unsigned encode and then cast to int
        return Encode(Unsafe.BitCast<ReadOnlySpan<int>, ReadOnlySpan<uint>>(input), output);
    }

    /// <summary>
    /// Get the decompressed length in int32 for a given input.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static int GetMaxCompressedLength(ReadOnlySpan<int> input)
    {
        if (!SpanNonNegative(input))
            throw new ArgumentException("Input must be non-negative.");

        // call unsigned get max compressed length
        return GetMaxCompressedLength(Unsafe.BitCast<ReadOnlySpan<int>, ReadOnlySpan<uint>>(input));
    }
}