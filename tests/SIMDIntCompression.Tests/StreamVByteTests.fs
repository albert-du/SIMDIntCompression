module StreamVByteTests

open System
open Xunit
open SIMDIntCompression
open System.Linq

[<Fact>]
let ``Empty input`` () =
    let input: uint array = [||]

    let maxOutputSize = StreamVByte_D1.GetMaxCompressedLength input

    let output = Array.zeroCreate<byte> maxOutputSize

    let outputLength = StreamVByte_D1.Encode(input, output)

    // truncate the output to the actual length
    let output = output[..outputLength]

    // 4 byte count header
    Assert.Equal(4, outputLength)

    // make sure the count is zero
    Assert.Equal(0, StreamVByte_D1.GetDecompressedLength output)

    let decoded: uint array =
        Array.zeroCreate (StreamVByte_D1.GetDecompressedLength output)

    let decodedLength = StreamVByte_D1.Decode(output[..outputLength], decoded)

    Assert.Equal(0, decodedLength)


[<Fact>]
let ``Single input`` () =
    let input: uint array = [| 32132u |]

    let maxOutputSize = StreamVByte_D1.GetMaxCompressedLength input

    let output = Array.zeroCreate<byte> maxOutputSize

    let outputLength = StreamVByte_D1.Encode(input, output)

    // truncate the output to the actual length
    let output = output[..outputLength]

    // 4 byte count header + key
    Assert.Equal(7, outputLength)

    // make sure the count is one
    Assert.Equal(1, StreamVByte_D1.GetDecompressedLength output)

    let decoded: uint array =
        Array.zeroCreate (StreamVByte_D1.GetDecompressedLength output)

    let decodedLength = StreamVByte_D1.Decode(output[..outputLength], decoded)

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
    let maxOutputSize = StreamVByte_D1.GetMaxCompressedLength input

    let output = Array.zeroCreate<byte> maxOutputSize

    let outputLength = StreamVByte_D1.Encode(input, output)

    // truncate the output to the actual length

    let output = output[..outputLength]

    let decoded: uint array =
        Array.zeroCreate (StreamVByte_D1.GetDecompressedLength output)

    let decodedLength = StreamVByte_D1.Decode(output[..outputLength], decoded)

    Assert.Equal(size, decodedLength)

    Enumerable.SequenceEqual(input, decoded) |> Assert.True