module S4_BP128_D4Tests

open System
open Xunit
open SIMDIntCompression
open System.Linq

[<Fact>]
let ``Empty input`` () =
    let input: uint array = [||]

    let maxOutputSize = S4_BP128_D4.GetMaxCompressedLength input

    let output = Array.zeroCreate<byte> maxOutputSize

    let outputLength = S4_BP128_D4.Encode(input, output)

    // truncate the output to the actual length
    let output = output[..outputLength]

    // 4 byte count headers
    Assert.Equal(4, outputLength)

    // make sure the count is zero
    Assert.Equal(0, S4_BP128_D4.GetDecompressedLength output)

    let decoded: uint array =
        Array.zeroCreate (S4_BP128_D4.GetDecompressedLength output)

    let decodedLength = S4_BP128_D4.Decode(output[..outputLength], decoded)

    Assert.Equal(0, decodedLength)

[<Theory>]
[<InlineData(3)>]
// [<InlineData(4)>]
// [<InlineData(5)>]
// [<InlineData(6)>]
// [<InlineData(7)>]
// [<InlineData(8)>]
// [<InlineData(9)>]
// [<InlineData(110)>]
// [<InlineData(32133)>]
// [<InlineData(21913)>]
// [<InlineData(10000)>]
let ``Roundtrip encoding/decoding`` factor =
    let size = factor  * S4_BP128_D4.BlockSize
    let next =
        let r = Random size

        fun () ->
            // density of 25%
            r.Next(0, size * 4) |> uint

    let input: uint array = generateRandomPosting size next

    // make sure input length is divisible by 128
    if size <> input.Length then
        failwithf "ERROR in TESTING DATA size: %d, input.Length: %d" size input.Length

    // round trip it
    let maxOutputSize = S4_BP128_D4.GetMaxCompressedLength input

    let output = Array.zeroCreate<byte> maxOutputSize

    let outputLength = S4_BP128_D4.Encode(input, output)

    // truncate the output to the actual length

    let output = output[..outputLength]

    Assert.Equal(size, S4_BP128_D4.GetDecompressedLength output)

    let decoded: uint array =
        Array.zeroCreate (S4_BP128_D4.GetDecompressedLength output)

    let decodedLength = S4_BP128_D4.Decode(output, decoded)

    Assert.Equal(size, decodedLength)

    Enumerable.SequenceEqual(input, decoded) |> Assert.True