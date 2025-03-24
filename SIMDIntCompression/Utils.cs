global using static SIMDIntCompression.Utils;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace SIMDIntCompression;

internal static class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<To> V128Cast<From, To>(Vector128<From> vector) where From : struct where To : struct =>
        Unsafe.BitCast<Vector128<From>, Vector128<To>>(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint MaxBitsAs32Int(Vector128<uint> accumulator)
    {
        Span<uint> tmp = stackalloc uint[4];
        accumulator.CopyTo(tmp);
        var tmp1 = tmp[0] | tmp[1] | tmp[2] | tmp[3];
        return (uint)(sizeof(uint) * 8 - BitOperations.LeadingZeroCount(tmp1));
    }
}