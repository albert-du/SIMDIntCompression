global using static SIMDIntCompression.Utils;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace SIMDIntCompression;

/// <summary>
/// Utility methods.
/// </summary>
internal static class Utils
{
    /// <summary>
    /// Casts a <see cref="Vector128{T}"/> from one type to another.
    /// </summary>
    /// <typeparam name="From"></typeparam>
    /// <typeparam name="To"></typeparam>
    /// <param name="vector"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<To> V128Cast<From, To>(Vector128<From> vector) where From : struct where To : struct =>
        Unsafe.BitCast<Vector128<From>, Vector128<To>>(vector);

    /// <summary>
    /// Get the maximum number of bits required to represent the values in the accumulator.
    /// </summary>
    /// <param name="accumulator"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint MaxBitsAs32Int(Vector128<uint> accumulator)
    {
        Span<uint> tmp = stackalloc uint[4];
        accumulator.CopyTo(tmp);
        var tmp1 = tmp[0] | tmp[1] | tmp[2] | tmp[3];
        return (uint)(sizeof(uint) * 8 - BitOperations.LeadingZeroCount(tmp1));
    }

    /// <summary>
    /// Checks if a span of integers is entirely non-negative.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    internal static bool SpanNonNegative(ReadOnlySpan<int> input)
    {
        if (input.IsEmpty)
            return true;

        // just the mask for the sign bit
        int maskInt = 1 << 31;

        var mask = Vector.Create(maskInt);

        foreach (var span in MemoryMarshal.Cast<int, Vector<int>>(input))
        {
            if ((mask & span) != Vector<int>.Zero)
                return false;
        }

        var remaining = input.Length % Vector<int>.Count;

        if (remaining == 0)
            return true;

        Span<int> remainingSpan = stackalloc int[Vector<int>.Count];
        input[^remaining..].CopyTo(remainingSpan);
        
        Vector<int> remainingVector = new(remainingSpan);
        return (mask & remainingVector) == Vector<int>.Zero;
    }
}