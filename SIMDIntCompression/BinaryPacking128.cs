namespace SIMDIntCompression;

/// <summary>
/// Binary Packing 128 with StreamVByte as the fallback encoding.
/// </summary>
public sealed class BinaryPacking128 : CompositeCodec<uint, S4_BP128_D4, StreamVByte_D1>;