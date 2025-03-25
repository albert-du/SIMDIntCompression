using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace SIMDIntCompression;

/// <summary>
/// Differentiation methods.
/// </summary>
public static class Differentiation
{

    internal static void DiffScalar(Span<int> input)
    {
        if (input.Length <= 1)
            return;
        for (var i = 1; i < input.Length; i++)
            input[i] = input[i] - input[i - 1];
    }

    internal static void DiffSimd256(Span<int> data)
    {
        ref int curr = ref MemoryMarshal.GetReference(data);
        ref int end = ref Unsafe.Add(ref curr, data.Length);
        ref int oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector256<int>.Count);

        // vector containing one element at the first position that was the last vector of the previous loop
        var prev = Vector256<int>.Zero;

        Vector256<int> rightShiftVector = Vector256.Create(-1, 0, 1, 2, 3, 4, 5, 6);
        Vector256<int> leftShiftVector = Vector256.Create(7, -1, -1, -1, -1, -1, -1, -1);

        while (!Unsafe.IsAddressGreaterThan(ref curr, ref oneVectorAwayFromEnd))
        {
            var current = Vector256.LoadUnsafe(ref curr);

            var rightShifted = Vector256.Shuffle(current, rightShiftVector);

            var diff = current - rightShifted - prev;

            diff.StoreUnsafe(ref curr);

            prev = Vector256.Shuffle(current, leftShiftVector);


            curr = ref Unsafe.Add(ref curr, Vector256<int>.Count);
        }

        var p = prev.ToScalar();

        while (Unsafe.IsAddressLessThan(ref curr, ref end))
        {
            var next = curr;
            curr -= p;
            p = next;

            curr = ref Unsafe.Add(ref curr, 1);
        }
    }

    internal static void DiffSimd128(Span<int> data)
    {
        ref int curr = ref MemoryMarshal.GetReference(data);
        ref int end = ref Unsafe.Add(ref curr, data.Length);
        ref int oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector128<int>.Count);

        // vector containing one element at the first position that was the last vector of the previous loop
        var prev = Vector128<int>.Zero;

        Vector128<int> rightShiftVector = Vector128.Create(-1, 0, 1, 2);
        Vector128<int> leftShiftVector =  Vector128.Create(3, -1, -1, -1);

        while (!Unsafe.IsAddressGreaterThan(ref curr, ref oneVectorAwayFromEnd))
        {
            var current = Vector128.LoadUnsafe(ref curr);

            var rightShifted = Vector128.Shuffle(current, rightShiftVector);

            var diff = current - rightShifted - prev;

            diff.StoreUnsafe(ref curr);

            prev = Vector128.Shuffle(current, leftShiftVector);


            curr = ref Unsafe.Add(ref curr, Vector128<int>.Count);
        }


        var p = prev.ToScalar();

        while (Unsafe.IsAddressLessThan(ref curr, ref end))
        {
            var next = curr;
            curr -= p;
            p = next;

            curr = ref Unsafe.Add(ref curr, 1);
        }
    }

    /// <summary>
    /// Calculates single int delta, in place
    /// </summary>
    /// <param name="input"></param>
    public static void Delta(Span<int> input)
    {
        if (!Vector128.IsHardwareAccelerated || input.Length < Vector128<int>.Count)
        {
            // Scalar code path
            DiffScalar(input);
        }
        else if (!Vector256.IsHardwareAccelerated || input.Length < Vector256<int>.Count)
        {
            // Vector128 code path
            DiffSimd128(input);
        }
        else
        {
            // Vector256 code path
            DiffSimd256(input);
        }
    }

    internal static void PrefixSumScalar(Span<int> input)
    {
        if (input.Length <= 1)
            return;

        for (var i = 1; i < input.Length; i++)
            input[i] = input[i] + input[i - 1];
    }

    internal static void PrefixSumSimd128(Span<int> input)
    {
        //const __m128i _tmp1 = _mm_add_epi32(_mm_slli_si128(curr, 8), curr);        \
        //const __m128i _tmp2 = _mm_add_epi32(_mm_slli_si128(_tmp1, 4), _tmp1);      \
        //ret = _mm_add_epi32(_tmp2, _mm_shuffle_epi32(prev, 0xff));

        ref int curr = ref MemoryMarshal.GetReference(input);
        ref int end = ref Unsafe.Add(ref curr, input.Length);
        ref int oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector128<int>.Count);

        // vector containing one element at the first position that was the last vector of the previous loop
        var prev = Vector128<int>.Zero;

        //Vector128<int> left8 = Vector128.Create(2, 3, -1, -1);
        Vector128<int> left8 = Vector128.Create(-1, -1, 0, 1);
        //Vector128<int> left4 = Vector128.Create(-1, 0, 1, 2);
        Vector128<int> left4 = Vector128.Create(-1, 0, 1, 2);
        Vector128<int> _0xff = Vector128.Create(3, 3, 3, 3);

        while (!Unsafe.IsAddressGreaterThan(ref curr, ref oneVectorAwayFromEnd))
        {
            var current = Vector128.LoadUnsafe(ref curr);

            var temp1 = current + Vector128.Shuffle(current, left8);
            var temp2 = temp1 + Vector128.Shuffle(temp1, left4);
            prev = temp2 + Vector128.Shuffle(prev, _0xff);

            prev.StoreUnsafe(ref curr);
            curr = ref Unsafe.Add(ref curr, Vector128<int>.Count);
        }

    }

    /// <summary>
    /// Calculates the prefix sums of the input, in place
    /// </summary>
    /// <param name="input"></param>
    public static void PrefixSum(Span<int> input)
    {
        if (!Vector128.IsHardwareAccelerated || input.Length < Vector128<int>.Count)
        {
            // Scalar code path
            PrefixSumScalar(input);
        }
        else if (!Vector256.IsHardwareAccelerated || input.Length < Vector256<int>.Count)
        {
            // Vector128 code path
            PrefixSumSimd128(input);
        }
        else
        {
            // Vector256 code path
            DiffSimd256(input);
        }
    }

    /// <summary>
    /// Calculates 4 int diffs, in place
    /// </summary>
    /// <param name="input"></param>
    public static void Diff4(Span<int> input)
    {
        // 128 = 4 * 32

        // if there are 4 or fewer then it makes no sense to diff them
        if (input.Length <= 4)
            return;

        if (Vector128.IsHardwareAccelerated)
            Diff4Simd(input);
        else
            Diff4Scalar(input);
    }

    private static void Diff4Simd(Span<int> data)
    {
        ref int curr = ref MemoryMarshal.GetReference(data);
        ref int end = ref Unsafe.Add(ref curr, data.Length);

        ref int oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector128<int>.Count);

        var prev = Vector128.LoadUnsafe(ref curr);

        curr = ref Unsafe.Add(ref curr, Vector128<int>.Count);

        while (!Unsafe.IsAddressGreaterThan(ref curr, ref oneVectorAwayFromEnd))
        {
            var next = Vector128.LoadUnsafe(ref curr);
            Vector128.Subtract(next, prev).StoreUnsafe(ref curr);

            prev = next;
            curr = ref Unsafe.Add(ref curr, Vector128<int>.Count);
        }

        while (Unsafe.IsAddressLessThan(ref curr, ref end))
        {
            curr -= Unsafe.Subtract(ref curr, 4);
            curr = ref Unsafe.Add(ref curr, 1);
        }
    }

    private static void Diff4Scalar(Span<int> input)
    {
        // if there are 4 or fewer then it makes no sense to diff them
        if (input.Length <= 4)
            return;

        for (var i = 4; i < input.Length; i++)
            input[i] = input[i] - input[i - 4];
    }


    private static void Undiff4Scalar(Span<int> input)
    {
        // if there are 4 or fewer then it makes no sense to diff them
        if (input.Length <= 4)
            return;

        for (var i = 4; i < input.Length; i++)
            input[i] = input[i] + input[i - 4];
    }

    private static void Undiff4Simd(Span<int> data)
    {
        ref int curr = ref MemoryMarshal.GetReference(data);
        ref int end = ref Unsafe.Add(ref curr, data.Length);

        ref int oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector128<int>.Count);

        var prev = Vector128.LoadUnsafe(ref curr);

        curr = ref Unsafe.Add(ref curr, Vector128<int>.Count);

        while (!Unsafe.IsAddressGreaterThan(ref curr, ref oneVectorAwayFromEnd))
        {
            var next = Vector128.LoadUnsafe(ref curr);
            prev = Vector128.Add(next, prev);
            prev.StoreUnsafe(ref curr);

            curr = ref Unsafe.Add(ref curr, Vector128<int>.Count);
        }

        while (Unsafe.IsAddressLessThan(ref curr, ref end))
        {
            curr += Unsafe.Subtract(ref curr, 4);
            curr = ref Unsafe.Add(ref curr, 1);
        }
    }

    /// <summary>
    /// Calculate a prefix sum in place.
    /// </summary>
    /// <param name="input"></param>
    public static void Undiff4(Span<int> input)
    {
        if (input.Length <= 4)
            return;

        if (Vector128.IsHardwareAccelerated)
            Undiff4Simd(input);
        else
            Undiff4Scalar(input);
    }
}