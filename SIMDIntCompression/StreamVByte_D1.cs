using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace SIMDIntCompression;

/// <summary>
/// StreamVByte codec with Delta encoding and SIMD decoding support.
/// </summary>
public unsafe class StreamVByte_D1 : IIntCompressionCodec<uint>
{
    public static int BlockSize => 1;

    /// <inheritdoc/>
    public static int GetMaxCompressedLength(ReadOnlySpan<uint> input)
    {
        var length = input.Length;
        // worst case is 4 bytes per int, plus some other bytes for the keys
        return length * 4 + 4 + (length + 3) / 4;
    }

    /// <inheritdoc/>
    public static int GetDecompressedLength(ReadOnlySpan<byte> input) =>
        // first 4 bytes is the count
        (int)MemoryMarshal.Read<uint>(input);

    /// <summary>
    /// Decode a byte array into an array of integers.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public static (int Written, int Read) DecodePart(ReadOnlySpan<byte> input, Span<uint> output)
    {
        fixed (uint* outputPtr = output)
        fixed (byte* inputPtr = input)
            return (StreamVByteDecodeD1(outputPtr, inputPtr), input.Length);
    }

    /// <inheritdoc/>
    public static int Decode(ReadOnlySpan<byte> input, Span<uint> output)
    {
        var (w, _) = DecodePart(input, output);

        return w;
    }

    /// <inheritdoc/>
    public static int Encode(ReadOnlySpan<uint> input, Span<byte> output)
    {
        fixed (uint* inputPtr = input)
        fixed (byte* outputPtr = output)
            return StreamVByteEncodeD1(outputPtr, inputPtr, (uint)input.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int StreamVByteEncodeD1(byte* output, uint* input, uint count)
    {
        *(uint*)output = count; // first 4 bytes is number of ints
        byte* keyPtr = output + 4; // keys come immediately after 32-bit count
        uint keyLen = (count + 3) / 4; // 2-bits rounded to full byte
        byte* dataPtr = keyPtr + keyLen; // variable byte data after all keys

        return (int)(StreamVByteEncodeScalarD1Init(input, keyPtr, dataPtr, count, 0u) - output);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte* StreamVByteEncodeScalarD1Init(uint* input, byte* keyPtr, byte* dataPtr, uint count, uint prev)
    {
        if (count == 0)
            return dataPtr;

        byte shift = 0; // cycles 0,2,4,6,0,2...
        byte key = 0;

        for (uint c = 0; c < count; c++)
        {
            if (shift == 8)
            {
                shift = 0;
                *keyPtr++ = key;
                key = 0;
            }
            uint val = input[c] - prev;
            prev = input[c];
            byte code = EncodeData(val, &dataPtr);
            key = (byte)(key | (code << shift));
            shift = (byte)(shift + 2);
        }
        *keyPtr = key; // write last key (no increment needed)
        return dataPtr; // ptr to first unused byte in data
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte EncodeData(uint val, byte** dataPtrPtr)
    {
        var dataPtr = *dataPtrPtr;
        byte code;

        if (val < (1 << 8)) // 1 byte
        {
            *dataPtr = (byte)val;
            *dataPtrPtr += 1;
            code = 0;
        }
        else if (val < (1 << 16)) // 2 bytes
        {
            *(ushort*)dataPtr = (ushort)val;
            *dataPtrPtr += 2;
            code = 1;
        }
        else if (val < (1 << 24)) // 3 bytes
        {
            *(ushort*)dataPtr = (ushort)val;
            *(dataPtr + 2) = (byte)(val >> 16);
            *dataPtrPtr += 3;
            code = 2;
        }
        else // 4 bytes
        {
            *(uint*)dataPtr = val;
            *dataPtrPtr += 4;
            code = 3;
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int StreamVByteDecodeD1(uint* output, byte* input)
    {
        uint count = *(uint*)input; // first 4 bytes is number of ints
        if (count == 0)
            return 0;

        byte* keyPtr = input + 4; // full list of keys is next
        uint keyLen = (count + 3) / 4; // 2-bits rounded to full byte
        byte* dataPtr = keyPtr + keyLen; // variable byte data after all keys

        // return StreamVByteDecodeScalarD1Init(output, keyPtr, dataPtr, count, 0);
        return StreamVByteDecodeVectorD1Init(output, keyPtr, dataPtr, count, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int StreamVByteDecodeVectorD1Init(uint* output, byte* keyPtr, byte* dataPtr, ulong count, uint prev)
    {
        var startOutPtr = output;

        ulong keyBytes = count / 4; // number of key bytes

        if (keyBytes >= 8)
        {
            Vector128<uint> Prev = Vector128.Create(prev);
            Vector128<ushort> data;

            long offset = -(long)keyBytes / 8 + 1;

            ulong* keyPtr64 = (ulong*)keyPtr - offset;
            ulong nextKeys = keyPtr64[offset];
            for (; offset != 0; offset++)
            {
                ulong keys = nextKeys;
                nextKeys = keyPtr64[offset + 1];
                // faster 16-bit delta since we only have 8-bit values
                // if (!keys)
                if (keys == 0) // 32 1-byte ints in a row
                {
                    data = Vector128.WidenLower(Vector128.Load(dataPtr));
                    Prev = Write16BitD1(output, data, Prev);
                    data = Vector128.WidenLower(Vector128.Load(dataPtr + 8));
                    Prev = Write16BitD1(output + 8, data, Prev);
                    data = Vector128.WidenLower(Vector128.Load(dataPtr + 16));
                    Prev = Write16BitD1(output + 16, data, Prev);
                    data = Vector128.WidenLower(Vector128.Load(dataPtr + 24));
                    Prev = Write16BitD1(output + 24, data, Prev);
                    output += 32;
                    dataPtr += 32;
                    continue;
                }

                data = DecodeVec((uint)(keys & 0x00FF), &dataPtr);
                Prev = WriteVectorD1(output, data, Prev);
                data = DecodeVec((uint)((keys & 0xFF00) >> 8), &dataPtr);
                Prev = WriteVectorD1(output + 4, data, Prev);

                keys >>= 16;
                data = DecodeVec((uint)(keys & 0x00FF), &dataPtr);
                Prev = WriteVectorD1(output + 8, data, Prev);
                data = DecodeVec((uint)((keys & 0xFF00) >> 8), &dataPtr);
                Prev = WriteVectorD1(output + 12, data, Prev);

                keys >>= 16;
                data = DecodeVec((uint)(keys & 0x00FF), &dataPtr);
                Prev = WriteVectorD1(output + 16, data, Prev);
                data = DecodeVec((uint)((keys & 0xFF00) >> 8), &dataPtr);
                Prev = WriteVectorD1(output + 20, data, Prev);

                keys >>= 16;
                data = DecodeVec((uint)(keys & 0x00FF), &dataPtr);
                Prev = WriteVectorD1(output + 24, data, Prev);
                data = DecodeVec((uint)((keys & 0xFF00) >> 8), &dataPtr);
                Prev = WriteVectorD1(output + 28, data, Prev);
                output += 32;
            }
            {

                ulong keys = nextKeys;
                // faster 16-bit delta since we only have 8-bit values
                // if (!keys)
                if (keys == 0)
                {
                    data = Vector128.WidenLower(Vector128.Load(dataPtr));
                    Prev = Write16BitD1(output, data, Prev);
                    data = Vector128.WidenLower(Vector128.Load(dataPtr + 8));
                    Prev = Write16BitD1(output + 8, data, Prev);
                    data = Vector128.WidenLower(Vector128.Load(dataPtr + 16));
                    Prev = Write16BitD1(output + 16, data, Prev);
                    data = Vector128.WidenLower(Vector128.Load(dataPtr + 24));
                    Prev = Write16BitD1(output + 24, data, Prev);
                    output += 32;
                    dataPtr += 32;
                }
                else
                {
                    data = DecodeVec((uint)(keys & 0x00FF), &dataPtr);
                    Prev = WriteVectorD1(output, data, Prev);
                    data = DecodeVec((uint)((keys & 0xFF00) >> 8), &dataPtr);
                    Prev = WriteVectorD1(output + 4, data, Prev);

                    keys >>= 16;
                    data = DecodeVec((uint)(keys & 0x00FF), &dataPtr);
                    Prev = WriteVectorD1(output + 8, data, Prev);
                    data = DecodeVec((uint)((keys & 0xFF00) >> 8), &dataPtr);
                    Prev = WriteVectorD1(output + 12, data, Prev);

                    keys >>= 16;
                    data = DecodeVec((uint)(keys & 0x00FF), &dataPtr);
                    Prev = WriteVectorD1(output + 16, data, Prev);
                    data = DecodeVec((uint)((keys & 0xFF00) >> 8), &dataPtr);
                    Prev = WriteVectorD1(output + 20, data, Prev);

                    keys >>= 16;
                    data = DecodeVec((uint)(keys & 0x00FF), &dataPtr);
                    Prev = WriteVectorD1(output + 24, data, Prev);
                    data = DecodeVec((uint)((keys & 0xFF00) >> 8), &dataPtr);
                    Prev = WriteVectorD1(output + 28, data, Prev);

                    output += 32;
                }
            }
            prev = output[-1];
        }
        ulong consumedKeys = keyBytes - (keyBytes & 7);
        return (int)(output - startOutPtr) + StreamVByteDecodeScalarD1Init(output, keyPtr + consumedKeys, dataPtr, (uint)(count & 31), prev);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<uint> Write16BitD1(uint* output, Vector128<ushort> vec, Vector128<uint> prev)
    {
        var broadcastLastXMM = Vector128.Create(3, 3, 3, 3);

        // vec = [A B C D E F G H] (16 bit values)
        Vector128<ushort> add = VectorExtensions.ShiftLeftLogical128BitLane(vec, 2);     // [- A B C D E F G]
        prev = Vector128.Shuffle(prev.AsInt32(), broadcastLastXMM).AsUInt32();          // [P P P P] (32-bit)
        vec += add;                                                                     // [A AB BC CD DE FG GH]
        add = VectorExtensions.ShiftLeftLogical128BitLane(vec, 4);                      // [- - A AB BC CD DE EF]
        vec += add;                                                                     // [A AB ABC ABCD BCDE CDEF DEFG EFGH]
        Vector128<uint> v1 = Vector128.WidenLower(vec).AsUInt32();                      // [A AB ABC ABCD] (32-bit)
        v1 += prev;                                                                     // [PA PAB PABC PABCD] (32-bit)
        Vector128<uint> v2 = Vector128.Shuffle(vec.AsSByte(), Vector128.Create(8, 9, -1, -1, 10, 11, -1, -1, 12, 13, -1, -1, 14, 15, -1, -1)).AsUInt32();
        // [BCDE CDEF DEFG EFGH] (32-bit)
        v2 += v1;                                                                       // [PABCDE PABCDEF PABCDEFG PABCDEFGH] (32-bit)
        Vector128.Store(v1, output);
        Vector128.Store(v2, output + 4);
        return v2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<uint> WriteVectorD1(uint* output, Vector128<ushort> data, Vector128<uint> prev)
    {
        Vector128<uint> vec = data.AsUInt32();

        Vector128<uint> add = VectorExtensions.ShiftLeftLogical128BitLane(vec, 4);  // Cycle 1: [- A B C] (already done)
        prev = Vector128.Shuffle(prev, Vector128.Create(3u, 3, 3, 3));              // Cycle 2: [P P P P]
        vec += add;                                                                 // Cycle 2: [A AB BC CD]
        add = VectorExtensions.ShiftLeftLogical128BitLane(vec, 8);                  // Cycle 3: [- - A AB]
        vec += prev;                                                                // Cycle 3: [PA PAB PBC PCD]
        vec += add;                                                                 // Cycle 4: [PA PAB PABC PABCD]
        Vector128.Store(vec, output);
        return vec;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ushort> DecodeVec(uint key, byte** dataPtrPtr)
    {
        byte len = StreamVByteShuffleTables.LengthTable[key];
        Vector128<byte> data = Vector128.Load(*dataPtrPtr);
        Vector128<byte> shuffle = StreamVByteShuffleTables.ShuffleTable[key].AsByte();

        data = Vector128.Shuffle(data, shuffle);
        *dataPtrPtr += len;

        return data.AsUInt16();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int StreamVByteDecodeScalarD1Init(uint* outPtr, byte* keyPtr, byte* dataPtr, uint count, uint prev)
    {
        if (count == 0)
            return 0; // no data

        var startOutPtr = outPtr;

        byte shift = 0;
        uint key = *keyPtr++;

        for (uint c = 0; c < count; c++)
        {
            if (shift == 8)
            {
                shift = 0;
                key = *keyPtr++;
            }
            uint val = DecodeData(&dataPtr, (byte)((key >> shift) & 0x3));
            val += prev;
            *outPtr++ = val;
            prev = val;
            shift += 2;
        }
        return (int)(outPtr - startOutPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint DecodeData(byte** dataPtrPtr, byte code)
    {
        byte* dataPtr = *dataPtrPtr;
        uint val;

        if (code == 0) // 1 byte
        {
            val = *dataPtr;
            dataPtr += 1;
        }
        else if (code == 1) // 2 bytes
        {
            val = *(ushort*)dataPtr;
            dataPtr += 2;
        }
        else if (code == 2) // 3 bytes
        {
            val = *(ushort*)dataPtr;
            val |= (uint)(*(dataPtr + 2) << 16);
            dataPtr += 3;
        }
        else // 4 bytes
        {
            val = *(uint*)dataPtr;
            dataPtr += 4;
        }

        *dataPtrPtr = dataPtr;
        return val;
    }
}


