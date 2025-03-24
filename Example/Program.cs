using SIMDIntCompression;

// BinaryPacking128 = S4-BP128-D4 + StreamVByte-D1

List<uint> input = [];

Random rand = new(123123);

// simple synthetic data
for (var i = 0; i < 1_000_000; i++)
{
    // for each 0-999,999, 0.03 chance being present 
    if (rand.Next() % 33 == 0)
        input.Add((uint)i);
}

var inputArray = input.ToArray();

// allocate enough space. This is a worst-case scenario.
var output = new byte[BinaryPacking128.GetMaxCompressedLength(inputArray)];

// Encode
var encodedLength = BinaryPacking128.Encode(inputArray, output);

// Slice the output to the actual length
var encoded = output[..encodedLength];

// allocate exactly the right amount of space
var decodedOutput = new uint[BinaryPacking128.GetDecompressedLength(encoded)];

// decode
var decodedWritten = BinaryPacking128.Decode(encoded, decodedOutput);

// Slice the output to the actual length (should be the same in this case)
var decoded = decodedOutput[..decodedWritten];

// Compare the input and the output
var success = inputArray.SequenceEqual(decoded);
// "true"

Console.WriteLine($"Input: {inputArray.Length} Output: {decodedWritten} Success: {success}");