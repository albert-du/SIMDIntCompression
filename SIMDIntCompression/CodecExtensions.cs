namespace SIMDIntCompression;


/// <summary>
/// Enable encoding extension methods for all codecs.
/// </summary>
public static class CodecExtensions
{
    /// <summary>
    /// Decode a span of bytes into a span of T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="Codec"></typeparam>
    /// <param name="codec"></param>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public static int Encode<T, Codec>(this Codec codec, ReadOnlySpan<T> input, Span<byte> output)
        where T : unmanaged
        where Codec : IIntCompressionCodec<T>
        => Codec.Encode(input, output);

    /// <summary>
    /// Decode a span of bytes into a span of T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="Codec"></typeparam>
    /// <param name="codec"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public static int GetMaxCompressedLength<T, Codec>(this Codec codec, ReadOnlySpan<T> input)
        where T : unmanaged
        where Codec : IIntCompressionCodec<T>
        => Codec.GetMaxCompressedLength(input);

    /// <summary>
    /// Decode a span of bytes into a span of T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="Codec"></typeparam>
    /// <param name="codec"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public static int GetMaxDecompressedLength<T, Codec>(this Codec codec, ReadOnlySpan<byte> input)
        where T : unmanaged
        where Codec : IIntCompressionCodec<T>
        => Codec.GetDecompressedLength(input);
}