namespace SIMDIntCompression;

using System.Runtime.Intrinsics;

/// <summary>
/// SIMD bit packing/unpacking for 32-bit (unsigned) integers.
/// </summary>
public static unsafe partial class SimdBitPacking32D4
{
    internal static uint MaxBits(uint* input, ref Vector128<uint> initOffset)
    {
        var pin = (Vector128<uint>*)input;
        var newVec = Vector128.Load((uint*)pin);
        var accumulator = newVec - initOffset; // Delta4
        var oldVec = newVec;

        for (uint k = 1; 4 * k < VectorSize; k++)
        {
            newVec = Vector128.Load((uint*)(pin + k));
            accumulator |= newVec - oldVec; // Delta4
            oldVec = newVec;
        }

        initOffset = oldVec;

        return MaxBitsAs32Int(accumulator);

        // var tmp1 = accumulator >> 8 | accumulator; // (A,B,C,D) xor (0,0,A,B) = (A,B,C xor A,D xor B)
        // var tmp2 = tmp1 >> 4 | tmp1; // (A,B,C xor A,D xor B) xor  (0,0,0,C xor A)

        // return (uint)(sizeof(uint) * 8 - BitOperations.LeadingZeroCount(tmp2.ToScalar()));
    }
}