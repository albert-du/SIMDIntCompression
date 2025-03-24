
using System.Runtime.InteropServices;

namespace SIMDIntCompression;

// Binary format
// [Codec1Length][Codec1Data][Codec2Data]
//
// [Codec1Length] 4 bytes                      Length of the data in bytes encoded by Codec1
// [Codec1Data]   Codec1Length bytes           Data encoded by Codec1
// [Codec2Data]   Remaining bytes              Data encoded with Codec2

/// <summary>
/// Composition of two Codecs.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="Codec1">The codec with larger blocks.</typeparam>
/// <typeparam name="Codec2">The codec for leftover items.</typeparam>
public class CompositeCodec<T, Codec1, Codec2> : IIntCompressionCodec<T>
    where T : unmanaged
    where Codec1 : IIntCompressionCodec<T>
    where Codec2 : IIntCompressionCodec<T>
{
    /// <inheritdoc/>
    public static int BlockSize => Codec2.BlockSize;

    /// <inheritdoc/>
    public static int GetMaxCompressedLength(ReadOnlySpan<T> input)
    {
        var length = input.Length;
        var c1size = length / Codec1.BlockSize * Codec1.BlockSize;

        var codec1Length = Codec1.GetMaxCompressedLength(input[..c1size]);
        var remainingLength = Codec2.GetMaxCompressedLength(input[c1size..]);

        return 4 + codec1Length + remainingLength;
    }

    /// <inheritdoc/>
    public static int GetDecompressedLength(ReadOnlySpan<byte> input)
    {
        var codec1Length = BitConverter.ToInt32(input);

        var codec1Data = input[4..(4 + codec1Length)];
        var codec2Data = input[(4 + codec1Length)..];

        var codec1Decompressed = Codec1.GetDecompressedLength(codec1Data);
        var codec2Decompressed = Codec2.GetDecompressedLength(codec2Data);

        return codec1Decompressed + codec2Decompressed;
    }

    /// <inheritdoc/>
    public static int Encode(ReadOnlySpan<T> input, Span<byte> output)
    {
        var codec1Length = input.Length / Codec1.BlockSize * Codec1.BlockSize;

        // leave 4 bytes for the length of the first codec
        var encoded = Codec1.Encode(input[..codec1Length], output[4..]);

        // write the length of the first codec
        MemoryMarshal.Write(output, encoded);

        // Remaining data 
        return 4 + encoded + Codec2.Encode(input[codec1Length..], output[(4 + encoded)..]);
    }

    /// <inheritdoc />
    public static int Decode(ReadOnlySpan<byte> input, Span<T> output)
    {
        var codec1Length = BitConverter.ToInt32(input);

        var codec1Data = input[4..(4 + codec1Length)];
        var codec2Data = input[(4 + codec1Length)..];

        var codec1Decompressed = Codec1.Decode(codec1Data, output);
        var codec2Decompressed = Codec2.Decode(codec2Data, output[codec1Decompressed..]);

        return codec1Decompressed + codec2Decompressed;
    }

}