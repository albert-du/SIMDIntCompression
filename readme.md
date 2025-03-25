# SIMDIntCompression
 .NET implementation of SIMD-based integer compression algorithms.

## Introduction
This libary is based on https://github.com/fast-pack/SIMDCompressionAndIntersection and provides a .NET implementation of SIMD-based integer compression algorithms for lists of increasing 32 bit unsigned integers. The library is written in C# and targets .NET 9.0+.


Consider the following list: 
```
23, 45, 47, 76, 95
```

We can compress this list by encoding the differences between consecutive integers. This is known as differential coding. The compressed list would look like this:

```
23, 22, 2, 29, 19
```

And we can finally pack these differences into fewer bytes.

## Algorithms
The following algorithms are implemented in this library:

- **S4-BP128-D4**: A SIMD-based implementation of 4 integer differential coding and binary packing. This algorithm only works on blocks of 128 integers. Use BinaryPacking128 for general use.

- **StreamVByte-D1**: A SIMD-based implementation of the StreamVByte algorithm with differential coding (currently only decoding is SIMD accelerated).

- **BinaryPacking128**: Composite algorithm that combines S4-BP128-D4 and StreamVByte-D1. It uses S4-BP128-D4 for blocks of 128 integers and StreamVByte-D1 for any remainder. __This is the recommended algorithm for general use.__ For those already using `fast-pack/SIMDCompressionAndIntersection` Note that the binary format of this implementations differs from that of the reference implementation in that this library prefixes the compressed data with the length of the compressed data of the first composite section. 

## Usage
This libary currently only supports 32 bit integers in an increasing order. It's recommended to use the `BinaryPacking128` algorithm for general use. The following example demonstrates how to use the `BinaryPacking128` algorithm to compress and decompress a list of integers. Static and instance methods are available for both encoding and decoding. BinaryPacking128 also contains int32 overloads for the same methods automatically casting to uint32.

```csharp
List<uint> input = [];

// simple synthetic data
for (var i = 0; i < 1_000_000; i++)
{
    // for each 0-999,999, ~0.03 independent chance being present 
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

## API

Each of the codecs have the following static methods:

```csharp
public static int GetMaxCompressedLength(ReadOnlySpan<uint> input);
```
Returns the maximum length of the compressed data for the given input.


```csharp
public static int GetDecompressedLength(ReadOnlySpan<byte> input);
```
Returns the length of the decompressed data for the given compressed data.

```csharp
public static int Encode(ReadOnlySpan<uint> input, Span<byte> output);
```

Encodes the input data and writes the compressed data to the output span. Returns the length of the compressed data.

```csharp
public static int Decode(ReadOnlySpan<byte> input, Span<uint> output);
```

Decodes the compressed data and writes the decompressed data to the output span. Returns the length of the decompressed data.

### Instance methods
```csharp
public int Encode(ReadOnlySpan<uint> input, Span<byte> output);
public int Decode(ReadOnlySpan<byte> input, Span<uint> output);
public int GetMaxCompressedLength(ReadOnlySpan<uint> input);
public int GetDecompressedLength(ReadOnlySpan<byte> input);
```

### BinaryPacking128

Additionally, BinaryPacking128 contains the following, which may throw `ArgumentException` if the input is not entirely non negative:

```csharp
public static int GetMaxCompressedLength(ReadOnlySpan<int> input);
```
Returns the maximum length of the compressed data for the given input.


```csharp
public static int Encode(ReadOnlySpan<int> input, Span<byte> output);
```

Encodes the input data and writes the compressed data to the output span. Returns the length of the compressed data.

```csharp
public static int Decode(ReadOnlySpan<byte> input, Span<int> output);
```

Decodes the compressed data and writes the decompressed data to the output span. Returns the length of the decompressed data.

## How it works

Using SIMD instructions, we can process multiple integers at once. This is particularly useful for integer compression algorithms, which often operate on blocks of integers. The library uses the `System.Numerics.Vectors` namespace to leverage SIMD instructions across multiple platforms without writing intrinsic code for each platform. The library is written in C# and targets .NET 9.0 and newer.