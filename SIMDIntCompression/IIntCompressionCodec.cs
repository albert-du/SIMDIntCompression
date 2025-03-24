using System.Text;

namespace SIMDIntCompression;

/// <summary>
/// Interface for integer compression codecs.
/// 
/// Compatible with blocks of <see cref="BlockSize"/> Ts.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IIntCompressionCodec<T> where T : unmanaged
{
    /// <summary>
    /// The block size of the format.
    /// 
    /// For example, if the format packs 128 integers into a block, this should return 128.
    /// 
    /// Attempting to encode or decode an array that is not divisible by this block size may throw an exception.
    /// </summary>
    static abstract int BlockSize { get; }

    /// <summary>
    /// Get the maximum compressed length in bytes for a given input.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    static abstract int GetMaxCompressedLength(ReadOnlySpan<T> input);

    /// <summary>
    /// Get the decompressed length in T for a given input.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    static abstract int GetDecompressedLength(ReadOnlySpan<byte> input);

    /// <summary>
    /// Encode a span of 'T' into a byte array. Ensure the output span is large enough, or use <see cref="GetMaxCompressedLength"/>
    /// </summary>
    /// <remarks>
    /// Roundtrips with <see cref="Decode(ReadOnlySpan{byte}, Span{T})"/>
    /// </remarks>
    /// <exception cref="ArgumentException">If the input length is not divisible with <see cref="BlockSize"/> </exception>
    /// <param name="input">Raw values.</param>
    /// <param name="output">Compressed result to write to.</param>
    /// <returns>The number of bytes written.</returns>
    static abstract int Encode(ReadOnlySpan<T> input, Span<byte> output);

    /// <summary>
    /// Decode a byte array into a span of 'T'. Ensure the output span is large enough, or use <see cref="GetDecompressedLength"/>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <returns>Bytes written</returns>
    static abstract int Decode(ReadOnlySpan<byte> input, Span<T> output);
}