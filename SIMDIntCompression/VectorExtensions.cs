using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SIMDIntCompression;

internal static class VectorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<T> ShiftLeftLogical128BitLane<T>(Vector128<T> input, [ConstantExpected] byte bytes) where T : struct
    {
        if (Sse2.IsSupported)
            return V128Cast<byte, T>(Sse2.ShiftLeftLogical128BitLane(input.AsByte(), bytes));

        throw new PlatformNotSupportedException();

        var shuffle =
            bytes switch
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
                _ => throw new ArgumentOutOfRangeException(nameof(bytes))
            };

        return V128Cast<sbyte, T>(Vector128.Shuffle(input.AsSByte(), shuffle));
    }
}

