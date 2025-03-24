# SIMDIntCompression
 .NET implementation of SIMD-based integer compression algorithms.

## Introduction
This libary is based on https://github.com/fast-pack/SIMDCompressionAndIntersection and provides a .NET implementation of SIMD-based integer compression algorithms. The library is written in C# and targets .NET 8.0+.

## Algorithms
The following algorithms are implemented in this library:

- **S4-BP128-D4**: A SIMD-based implementation of 4 integer differential coding and binary packing.
- **StreamVByte-D1**: A SIMD-based implementation of the StreamVByte algorithm with differential coding (currently only decoding is SIMD accelerated).

` **BinaryPacking128**: Composite algorithm that combines S4-BP128-D4 and StreamVByte-D1. It uses S4-BP128-D4 for blocks of 128 integers and StreamVByte-D1 for any remainder.

## Usage
```csharp
List<uint> input = [];

// simple synthetic data
for (var i = 0; i < 1_000_000; i++)
{
    // for each 0-999,999, ~0.03 chance being present 
    if (Random.Shared.Next() % 33 == 0)
        input.Add((uint)i);
}

var inputArray = input.ToArray();

// allocate enough space. This is a worst-case scenario.
var output = new byte[BinaryPacking128.GetMaxCompressedLength(inputArray)];

var encodedLength = BinaryPacking128.Encode(inputArray, output);

// Slice the output to the actual length
var encoded = output[..encodedLength];

// allocate exactly the right amount of space
var decodedOutput = new uint[BinaryPacking128.GetDecompressedLength(encoded)];

var decodedWritten = BinaryPacking128.Decode(encoded, decodedOutput);

// Slice the output to the actual length (should be the same in this case)
var decoded = decodedOutput[..decodedWritten];

var success = inputArray.SequenceEqual(decoded);
// "true"

Console.WriteLine($"Input: {inputArray.Length} Output: {decodedWritten} Success: {success}");
```