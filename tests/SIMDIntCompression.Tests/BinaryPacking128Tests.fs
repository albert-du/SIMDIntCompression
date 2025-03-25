module BinaryPacking128Tests

open System
open Xunit
open SIMDIntCompression
open System.Linq

[<Fact>]
let ``Empty input`` () =
    let input: uint array = [||]

    let maxOutputSize = BinaryPacking128.GetMaxCompressedLength input

    let output = Array.zeroCreate<byte> maxOutputSize

    let outputLength = BinaryPacking128.Encode(input, output)

    // truncate the output to the actual length
    let output = output[..outputLength]

    // 4 byte * 3 count headers
    Assert.Equal(12, outputLength)

    // make sure the count is zero
    Assert.Equal(0, BinaryPacking128.GetDecompressedLength output)

    let decoded: uint array =
        Array.zeroCreate (BinaryPacking128.GetDecompressedLength output)

    let decodedLength = BinaryPacking128.Decode(output[..outputLength], decoded)

    Assert.Equal(0, decodedLength)


[<Fact>]
let ``Single input`` () =
    let input: uint array = [| 32132u |]

    let maxOutputSize = BinaryPacking128.GetMaxCompressedLength input

    let output = Array.zeroCreate<byte> maxOutputSize

    let outputLength = BinaryPacking128.Encode(input, output)

    // truncate the output to the actual length
    let output = output[..outputLength]

    // 4 * 3 byte count header + key
    Assert.Equal(15, outputLength)

    // make sure the count is one
    Assert.Equal(1, BinaryPacking128.GetDecompressedLength output)

    let decoded: uint array =
        Array.zeroCreate (BinaryPacking128.GetDecompressedLength output)

    let decodedLength = BinaryPacking128.Decode(output[..outputLength], decoded)

    Assert.Equal(1, decodedLength)

    Enumerable.SequenceEqual(input, decoded) |> Assert.True


[<Theory>]
[<InlineData(2)>]
[<InlineData(3)>]
[<InlineData(4)>]
[<InlineData(5)>]
[<InlineData(6)>]
[<InlineData(7)>]
[<InlineData(8)>]
[<InlineData(9)>]
[<InlineData(10)>]
[<InlineData(100)>]
[<InlineData(1000)>]
[<InlineData(1001230)>]
[<InlineData(3213213)>]
[<InlineData(2191233)>]
[<InlineData(1000000)>]
let ``Roundtrip encoding/decoding`` size =
    let next =
        let r = Random size

        fun () ->
            // density of 25%
            r.Next(0, size * 4) |> uint

    let input: uint array = generateRandomPosting size next

    // round trip it
    let maxOutputSize = BinaryPacking128.GetMaxCompressedLength input

    let output = Array.zeroCreate<byte> maxOutputSize

    let outputLength = BinaryPacking128.Encode(input, output)

    // truncate the output to the actual length

    let output = output[..outputLength]

    let decoded: uint array =
        Array.zeroCreate (BinaryPacking128.GetDecompressedLength output)

    let decodedLength = BinaryPacking128.Decode(output[..outputLength], decoded)

    Assert.Equal(size, decodedLength)

    Enumerable.SequenceEqual(input, decoded) |> Assert.True


[<Fact>]
let ``Trivial`` () =
    let n = 1024
    let data = [| for i in 0..n-1 do uint i |]

    let maxOutputSize = BinaryPacking128.GetMaxCompressedLength data
    let output = Array.zeroCreate<byte> maxOutputSize

    let outputLength = BinaryPacking128.Encode(data, output)
    let output = output[..outputLength]

    let decoded = Array.zeroCreate<uint> (BinaryPacking128.GetDecompressedLength output)
    let decodedLength = BinaryPacking128.Decode(output, decoded)

    Assert.Equal(n, decodedLength)
    Enumerable.SequenceEqual(data, decoded) |> Assert.True
