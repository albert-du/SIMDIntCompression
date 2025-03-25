// dotnet fsi .\scripts\generate.fsx > .\SIMDIntCompression\SimdBitPacking32.Simd.cs

let length = 32
let vectorSize = 128
let vt = $"V{vectorSize}i" // abbreviation for Vector128<uint>
let load = $"Vector{vectorSize}.Load"
let store = $"Vector{vectorSize}.Store"
let create = $"Vector{vectorSize}.Create"

let packBitInternal bit =
    let mutable inWordPointer = 0
    let mutable valueCounter = 0

    let mutable k = 0
    let m = ceil (float length * float bit) / float length |> int

    seq {
        while k < m do
            if valueCounter = length then
                k <- bit // "break"

            for x in inWordPointer .. bit .. 31 do
                if x <> 0 then $"        outReg |= inReg << {x};"
                else "        outReg = inReg;"

                if x + bit >= 32 then
                    while inWordPointer < length do
                        inWordPointer <- inWordPointer + bit
                    $"        {store}(outReg, (uint*)output);" 

                    if valueCounter + 1 < length then
                        "        ++output;"

                    inWordPointer <- inWordPointer - 32

                    if inWordPointer > 0 then
                        $"        outReg = inReg >> {bit - inWordPointer}; "

                if valueCounter + 1 < 32 then
                    $"        inReg = {load}((uint*)++inVec);\n"
                valueCounter <- valueCounter + 1

                if valueCounter = 32 then
                    k <- bit // "break"
            
            k <- k + 1
    } 
    |> String.concat "\n"

let packBit bit = $$"""
    private static void Pack{{bit}}(uint* input, {{vt}}* output)
    {
        {{vt}}* inVec = ({{vt}}*)input;
        {{vt}} outReg;
        {{vt}} inReg = {{load}}((uint*)inVec);

{{packBitInternal bit}}
    }
"""

let unpackBitInternal bit =
    let mutable inWordPointer = 0
    let mutable valueCounter = 0

    let mutable k = 0
    let m = ceil (float length * float bit) / 32.0 |> int

    [
        while k < m do
            for x in inWordPointer .. bit .. 31 do
                if valueCounter = length then
                    k <- m // "break"

                if x = 0 then
                    "outReg = inReg & mask;"
                elif x + bit < 32 then
                    $"outReg = inReg >> {x} & mask;"
                else
                    $"outReg = inReg >> {x};"

                if x + bit >= 32 then
                    while inWordPointer < 32 do
                        inWordPointer <- inWordPointer + bit

                    if valueCounter + 1 < length then
                        $"inReg = {load}((uint*)++input);\n"

                    inWordPointer <- inWordPointer - 32

                    if inWordPointer > 0 then
                        $"outReg |= (inReg << {bit} - {inWordPointer}) & mask;"

                $"{store}(outReg, (uint*)outVec++);\n"

                valueCounter <- valueCounter + 1

                if valueCounter = length then
                    k <- m // "break"

            k <- k + 1
    ]
    |> List.map (sprintf "        %s")
    |> String.concat "\n"

let unpackBit bit = $$"""
    private static void Unpack{{bit}}({{vt}}* input, uint* output)
    {
        {{vt}}* outVec = ({{vt}}*)output;
        {{vt}} inReg = {{load}}((uint*)input);
        {{vt}} outReg;
        {{vt}} mask = {{create}}((1u << {{bit}}) - 1);

{{unpackBitInternal bit}}
    }
"""

printfn $$"""
// This file was automatically generated by generate.fsx. Do not modify directly.
// 
// To regenerate this file, run the following command:
// dotnet fsi .\scripts\generate.fsx > .\SIMDIntCompression\SimdBitPacking32.Simd.cs
//
// References:
// https://github.com/fast-pack/SIMDCompressionAndIntersection

namespace SIMDIntCompression;

using System.Runtime.Intrinsics;

using {{vt}} = System.Runtime.Intrinsics.Vector128<uint>;

/// <summary>
/// SIMD bit packing/unpacking for 32-bit (unsigned) integers.
/// </summary>
public static unsafe partial class SimdBitPacking32
{
    private const int VectorSize = {{vectorSize}};

{{String.concat "\n" [for bit in 1..32 do packBit bit]}}

    private static void Unpack1({{vt}}* input, uint* output)
    {
        {{vt}}* outVec = ({{vt}}*)output;
        {{vt}} inReg1 = {{load}}((uint*)input);
        {{vt}} inReg2 = inReg1;
        {{vt}} outReg1, outReg2, outReg3, outReg4;
        {{vt}} mask = {{create}}(1u);

        int shift = 0;

        for (nuint i = 0; i < 8; i++)
        {
            outReg1 = (inReg1 >> (shift++) ) & mask;
            outReg2 = (inReg2 >> (shift++) ) & mask;
            outReg3 = (inReg1 >> (shift++) ) & mask;
            outReg4 = (inReg2 >> (shift++) ) & mask;
            {{store}}(outReg1, (uint*)outVec++);
            {{store}}(outReg2, (uint*)outVec++);
            {{store}}(outReg3, (uint*)outVec++);
            {{store}}(outReg4, (uint*)outVec++);
        } 

    }


{{String.concat "\n" [for bit in 2..31 do unpackBit bit]}}

    private static void Unpack32({{vt}}* input, uint* output)
    {
        {{vt}}* outVec = ({{vt}}*)output;
        for (int outer = 0; outer < 32; outer++)
            {{store}}({{load}}(input++), outVec++);
    }

    /// <summary>
    /// Pack the input array of integers into the output array of vectors.
    /// </summary>
    /// <param name="input">The input array of integers.</param>
    /// <param name="output">The output array of vectors.</param>
    /// <param name="bit">The bit width.</param>
    public static void Pack(uint* input, {{vt}}* output, int bit)
    {
        switch (bit)
        {
            {{String.concat "" [for bit in 1..32 do $"
            case {bit}:
                Pack{bit}(input, output);
                break;
            "]}}
            default:
                throw new ArgumentException("Unsupported bit width.");
        }
    }

    /// <summary>
    /// Unpack the input array of vectors into the output array of integers.
    /// </summary>
    /// <param name="input">The input array of vectors.</param>
    /// <param name="output">The output array of integers.</param>
    /// <param name="bit">The bit width.</param>
    /// <returns>The last vector.</returns>
    public static void Unpack({{vt}}* input, uint* output, int bit)
    {
        switch (bit)
        {
            {{String.concat "" [for bit in 1..32 do $"
            case {bit}:
                Unpack{bit}(input, output);
                break;
            "]}}
            default:
                throw new ArgumentException("Unsupported bit width.");
        }
    }
}
"""