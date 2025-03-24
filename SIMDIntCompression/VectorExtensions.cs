using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SIMDIntCompression;

/// <summary>
/// Extensions for <see cref="Vector128{T}"/> intrinsics.
/// </summary>
internal static class VectorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<T> ShiftLeftLogical128BitLane<T>(Vector128<T> input, [ConstantExpected] byte numBytes) where T : struct
    {
        // x86 can use this.
        if (Sse2.IsSupported)
            return V128Cast<byte, T>(Sse2.ShiftLeftLogical128BitLane(input.AsByte(), numBytes));

        // Otherwise we need to use a shuffle.

#pragma warning disable CS0618 // Type or member is obsolete
        return DoNotCallDirectlyShiftLeftLogical128BitLaneAllPlatforms(input, numBytes);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Do not call this method directly. Use <see cref="ShiftLeftLogical128BitLane{T}"/> instead.
    /// 
    /// The only reason this is not private is for testing purposes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <param name="numBytes"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [Obsolete("Do not call this method directly. Use ShiftLeftLogical128BitLane instead.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<T> DoNotCallDirectlyShiftLeftLogical128BitLaneAllPlatforms<T>(Vector128<T> input, [ConstantExpected] byte numBytes) where T : struct
    {
        var shuffle =
            (numBytes % 16) switch
            {
                0 => Vector128.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15),
                1 => Vector128.Create(-1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14),
                2 => Vector128.Create(-1, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13),
                3 => Vector128.Create(-1, -1, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12),
                4 => Vector128.Create(-1, -1, -1, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11),
                5 => Vector128.Create(-1, -1, -1, -1, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10),
                6 => Vector128.Create(-1, -1, -1, -1, -1, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9),
                7 => Vector128.Create(-1, -1, -1, -1, -1, -1, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8),
                8 => Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, 0, 1, 2, 3, 4, 5, 6, 7),
                9 => Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 1, 2, 3, 4, 5, 6),
                10 => Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 1, 2, 3, 4, 5),
                11 => Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 1, 2, 3, 4),
                12 => Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 1, 2, 3),
                13 => Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 1, 2),
                14 => Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 1),
                15 => Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -3, 0),
                _ => throw new Exception("Unreachable")
            };

        return V128Cast<sbyte, T>(Vector128.Shuffle(input.AsSByte(), shuffle));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<T> ShiftRightLogical128BitLane<T>(Vector128<T> input, [ConstantExpected] byte numBytes) where T : struct
    {
        // x86 can use this.
        if (Sse2.IsSupported)
            return V128Cast<byte, T>(Sse2.ShiftRightLogical128BitLane(input.AsByte(), numBytes));

        // Otherwise we need to use a shuffle.

#pragma warning disable CS0618 // Type or member is obsolete
        return DoNotCallDirectlyShiftRightLogical128BitLaneAllPlatforms(input, numBytes);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Do not call this method directly. Use <see cref="ShiftRightLogical128BitLane{T}"/> instead.
    /// 
    /// The only reason this is not private is for testing purposes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <param name="numBytes"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [Obsolete("Do not call this method directly. Use ShiftRightLogical128BitLane instead.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<T> DoNotCallDirectlyShiftRightLogical128BitLaneAllPlatforms<T>(Vector128<T> input, [ConstantExpected] byte numBytes) where T : struct
    {
        var shuffle =
            (numBytes % 16) switch
            {
                0 => Vector128.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15),
                1 => Vector128.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, -1),
                2 => Vector128.Create(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, -1, -1),
                3 => Vector128.Create(3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, -1, -1, -1),
                4 => Vector128.Create(4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, -1, -1, -1, -1),
                5 => Vector128.Create(5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, -1, -1, -1, -1, -1),
                6 => Vector128.Create(6, 7, 8, 9, 10, 11, 12, 13, 14, 15, -1, -1, -1, -1, -1, -1),
                7 => Vector128.Create(7, 8, 9, 10, 11, 12, 13, 14, 15, -1, -1, -1, -1, -1, -1, -1),
                8 => Vector128.Create(8, 9, 10, 11, 12, 13, 14, 15, -1, -1, -1, -1, -1, -1, -1, -1),
                9 => Vector128.Create(9, 10, 11, 12, 13, 14, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1),
                10 => Vector128.Create(10, 11, 12, 13, 14, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1),
                11 => Vector128.Create(11, 12, 13, 14, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1),
                12 => Vector128.Create(12, 13, 14, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1),
                13 => Vector128.Create(13, 14, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1),
                14 => Vector128.Create(14, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1),
                15 => Vector128.Create(15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1),
                _ => throw new Exception("Unreachable")
            };

        return V128Cast<sbyte, T>(Vector128.Shuffle(input.AsSByte(), shuffle));
    }

}

