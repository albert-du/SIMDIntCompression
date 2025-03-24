namespace SIMDIntCompression;

using System.Numerics;
using System.Runtime.Intrinsics;

/// <summary>
/// SIMD bit packing/unpacking for 32-bit (unsigned) integers.
/// 
/// No differential coding.
/// </summary>
public static unsafe partial class SimdBitPacking32
{
    public static uint MaxBits(uint* begin)
    {
        var pin = (Vector128<uint>*)begin;
        var accumulator = Vector128.Load((uint*)pin);

        for (uint k = 1; k < VectorSize; k++)
            accumulator |= Vector128.Load((uint*)(pin + k));

        var tmp1 = accumulator >> 8 | accumulator; // (A,B,C,D) xor (0,0,A,B) = (A,B,C xor A,D xor B)
        var tmp2 = tmp1 >> 4 | tmp1; // (A,B,C xor A,D xor B) xor  (0,0,0,C xor A)

        return (uint)(sizeof(uint) * 8 - BitOperations.LeadingZeroCount(tmp2.ToScalar()));
    }
}