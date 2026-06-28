// Ported from https://github.com/jixunmoe-go/qrc (MIT).
// See THIRD_PARTY_NOTICES.md in the repository root.

namespace PixelBar_App.Services.Lyrics;

internal static class QmQrcDes
{
    private static readonly byte[] KeyRndShifts =
    [
        0x01, 0x01, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x01, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x01,
    ];

    private static readonly byte[] LargeStateShifts = [0x1a, 0x14, 0x0e, 0x08, 0x3a, 0x34, 0x2e, 0x28];

    private static readonly byte[][] SBoxes =
    [
        [14, 0, 4, 15, 13, 7, 1, 4, 2, 14, 15, 2, 11, 13, 8, 1, 3, 10, 10, 6, 6, 12, 12, 11, 5, 9, 9, 5, 0, 3, 7, 8, 4, 15, 1, 12, 14, 8, 8, 2, 13, 4, 6, 9, 2, 1, 11, 7, 15, 5, 12, 11, 9, 3, 7, 14, 3, 10, 10, 0, 5, 6, 0, 13],
        [15, 3, 1, 13, 8, 4, 14, 7, 6, 15, 11, 2, 3, 8, 4, 15, 9, 12, 7, 0, 2, 1, 13, 10, 12, 6, 0, 9, 5, 11, 10, 5, 0, 13, 14, 8, 7, 10, 11, 1, 10, 3, 4, 15, 13, 4, 1, 2, 5, 11, 8, 6, 12, 7, 6, 12, 9, 0, 3, 5, 2, 14, 15, 9],
        [10, 13, 0, 7, 9, 0, 14, 9, 6, 3, 3, 4, 15, 6, 5, 10, 1, 2, 13, 8, 12, 5, 7, 14, 11, 12, 4, 11, 2, 15, 8, 1, 13, 1, 6, 10, 4, 13, 9, 0, 8, 6, 15, 9, 3, 8, 0, 7, 11, 4, 1, 15, 2, 14, 12, 3, 5, 11, 10, 5, 14, 2, 7, 12],
        [7, 13, 13, 8, 14, 11, 3, 5, 0, 6, 6, 15, 9, 0, 10, 3, 1, 4, 2, 7, 8, 2, 5, 12, 11, 1, 12, 10, 4, 14, 15, 9, 10, 3, 6, 15, 9, 0, 0, 6, 12, 10, 11, 10, 7, 13, 13, 8, 15, 9, 1, 4, 3, 5, 14, 11, 5, 12, 2, 7, 8, 2, 4, 14],
        [2, 14, 12, 11, 4, 2, 1, 12, 7, 4, 10, 7, 11, 13, 6, 1, 8, 5, 5, 0, 3, 15, 15, 10, 13, 3, 0, 9, 14, 8, 9, 6, 4, 11, 2, 8, 1, 12, 11, 7, 10, 1, 13, 14, 7, 2, 8, 13, 15, 6, 9, 15, 12, 0, 5, 9, 6, 10, 3, 4, 0, 5, 14, 3],
        [12, 10, 1, 15, 10, 4, 15, 2, 9, 7, 2, 12, 6, 9, 8, 5, 0, 6, 13, 1, 3, 13, 4, 14, 14, 0, 7, 11, 5, 3, 11, 8, 9, 4, 14, 3, 15, 2, 5, 12, 2, 9, 8, 5, 12, 15, 3, 10, 7, 11, 0, 14, 4, 1, 10, 7, 1, 6, 13, 0, 11, 8, 6, 13],
        [4, 13, 11, 0, 2, 11, 14, 7, 15, 4, 0, 9, 8, 1, 13, 10, 3, 14, 12, 3, 9, 5, 7, 12, 5, 2, 10, 15, 6, 8, 1, 6, 1, 6, 4, 11, 11, 13, 13, 8, 12, 1, 3, 4, 7, 10, 14, 7, 10, 9, 15, 5, 6, 0, 8, 15, 0, 14, 5, 2, 9, 3, 2, 12],
        [13, 1, 2, 15, 8, 13, 4, 8, 6, 10, 15, 3, 11, 7, 1, 4, 10, 12, 9, 5, 3, 6, 14, 11, 5, 0, 0, 14, 12, 9, 7, 2, 7, 2, 11, 1, 4, 14, 1, 7, 9, 4, 12, 10, 14, 8, 2, 13, 0, 15, 6, 12, 10, 9, 13, 0, 15, 3, 3, 5, 5, 6, 8, 11],
    ];

    private static readonly byte[] PBox =
    [
        0x0f, 0x06, 0x13, 0x14, 0x1c, 0x0b, 0x1b, 0x10, 0x00, 0x0e, 0x16, 0x19, 0x04, 0x11, 0x1e, 0x09,
        0x01, 0x07, 0x17, 0x0d, 0x1f, 0x1a, 0x02, 0x08, 0x12, 0x0c, 0x1d, 0x05, 0x15, 0x0a, 0x03, 0x18,
    ];

    private static readonly byte[] Ip =
    [
        0x39, 0x31, 0x29, 0x21, 0x19, 0x11, 0x09, 0x01, 0x3b, 0x33, 0x2b, 0x23, 0x1b, 0x13, 0x0b, 0x03,
        0x3d, 0x35, 0x2d, 0x25, 0x1d, 0x15, 0x0d, 0x05, 0x3f, 0x37, 0x2f, 0x27, 0x1f, 0x17, 0x0f, 0x07,
        0x38, 0x30, 0x28, 0x20, 0x18, 0x10, 0x08, 0x00, 0x3a, 0x32, 0x2a, 0x22, 0x1a, 0x12, 0x0a, 0x02,
        0x3c, 0x34, 0x2c, 0x24, 0x1c, 0x14, 0x0c, 0x04, 0x3e, 0x36, 0x2e, 0x26, 0x1e, 0x16, 0x0e, 0x06,
    ];

    private static readonly byte[] IpInv =
    [
        0x27, 0x07, 0x2f, 0x0f, 0x37, 0x17, 0x3f, 0x1f, 0x26, 0x06, 0x2e, 0x0e, 0x36, 0x16, 0x3e, 0x1e,
        0x25, 0x05, 0x2d, 0x0d, 0x35, 0x15, 0x3d, 0x1d, 0x24, 0x04, 0x2c, 0x0c, 0x34, 0x14, 0x3c, 0x1c,
        0x23, 0x03, 0x2b, 0x0b, 0x33, 0x13, 0x3b, 0x1b, 0x22, 0x02, 0x2a, 0x0a, 0x32, 0x12, 0x3a, 0x1a,
        0x21, 0x01, 0x29, 0x09, 0x31, 0x11, 0x39, 0x19, 0x20, 0x00, 0x28, 0x08, 0x30, 0x10, 0x38, 0x18,
    ];

    private static readonly byte[] KeyPermutationTable =
    [
        0x38, 0x30, 0x28, 0x20, 0x18, 0x10, 0x08, 0x00, 0x39, 0x31, 0x29, 0x21, 0x19, 0x11, 0x09, 0x01,
        0x3a, 0x32, 0x2a, 0x22, 0x1a, 0x12, 0x0a, 0x02, 0x3b, 0x33, 0x2b, 0x23, 0x3e, 0x36, 0x2e, 0x26,
        0x1e, 0x16, 0x0e, 0x06, 0x3d, 0x35, 0x2d, 0x25, 0x1d, 0x15, 0x0d, 0x05, 0x3c, 0x34, 0x2c, 0x24,
        0x1c, 0x14, 0x0c, 0x04, 0x1b, 0x13, 0x0b, 0x03,
    ];

    private static readonly byte[] KeyCompression =
    [
        0x0d, 0x10, 0x0a, 0x17, 0x00, 0x04, 0x02, 0x1b, 0x0e, 0x05, 0x14, 0x09, 0x16, 0x12, 0x0b, 0x03,
        0x19, 0x07, 0x0f, 0x06, 0x1a, 0x13, 0x0c, 0x01, 0x2d, 0x38, 0x23, 0x29, 0x33, 0x3b, 0x22, 0x2c,
        0x37, 0x31, 0x25, 0x34, 0x30, 0x35, 0x2b, 0x3c, 0x26, 0x39, 0x32, 0x2e, 0x36, 0x28, 0x21, 0x24,
    ];

    private static readonly byte[] KeyExpansion =
    [
        0x1f, 0x00, 0x01, 0x02, 0x03, 0x04, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x07, 0x08, 0x09, 0x0a,
        0x0b, 0x0c, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x13, 0x14,
        0x15, 0x16, 0x17, 0x18, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x00,
    ];

    private static readonly ulong[] DesShiftTableCache =
    [
        0x0000000080000000, 0x0000000040000000, 0x0000000020000000, 0x0000000010000000,
        0x0000000008000000, 0x0000000004000000, 0x0000000002000000, 0x0000000001000000,
        0x0000000000800000, 0x0000000000400000, 0x0000000000200000, 0x0000000000100000,
        0x0000000000080000, 0x0000000000040000, 0x0000000000020000, 0x0000000000010000,
        0x0000000000008000, 0x0000000000004000, 0x0000000000002000, 0x0000000000001000,
        0x0000000000000800, 0x0000000000000400, 0x0000000000000200, 0x0000000000000100,
        0x0000000000000080, 0x0000000000000040, 0x0000000000000020, 0x0000000000000010,
        0x0000000000000008, 0x0000000000000004, 0x0000000000000002, 0x0000000000000001,
        0x8000000000000000, 0x4000000000000000, 0x2000000000000000, 0x1000000000000000,
        0x0800000000000000, 0x0400000000000000, 0x0200000000000000, 0x0100000000000000,
        0x0080000000000000, 0x0040000000000000, 0x0020000000000000, 0x0010000000000000,
        0x0008000000000000, 0x0004000000000000, 0x0002000000000000, 0x0001000000000000,
        0x0000800000000000, 0x0000400000000000, 0x0000200000000000, 0x0000100000000000,
        0x0000080000000000, 0x0000040000000000, 0x0000020000000000, 0x0000010000000000,
        0x0000008000000000, 0x0000004000000000, 0x0000002000000000, 0x0000001000000000,
        0x0000000800000000, 0x0000000400000000, 0x0000000200000000, 0x0000000100000000,
    ];

    public static void TransformBytes(byte[] data, ReadOnlySpan<byte> key, bool encrypt)
    {
        if (data.Length % 8 != 0)
            throw new ArgumentException("Data length must be a multiple of 8.", nameof(data));

        var subkeys = BuildSubkeys(key, encrypt);
        for (var i = 0; i < data.Length; i += 8)
        {
            var value = BitConverter.ToUInt64(data, i);
            value = TransformBlock(value, subkeys);
            BitConverter.TryWriteBytes(data.AsSpan(i, 8), value);
        }
    }

    private static ulong[] BuildSubkeys(ReadOnlySpan<byte> keyBytes, bool encrypt)
    {
        var subkeys = new ulong[16];
        var key = BitConverter.ToUInt64(keyBytes);
        var param = MapU64(key, KeyPermutationTable);
        var paramC = GetLo32(param);
        var paramD = GetHi32(param);

        for (var i = 0; i < KeyRndShifts.Length; i++)
        {
            var subkeyIdx = encrypt ? i : 15 - i;
            UpdateParam(ref paramC, KeyRndShifts[i]);
            UpdateParam(ref paramD, KeyRndShifts[i]);
            subkeys[subkeyIdx] = MapU64(MakeU64(paramD, paramC), KeyCompression);
        }

        return subkeys;
    }

    private static ulong TransformBlock(ulong data, ulong[] subkeys)
    {
        var state = DesIp(data);
        foreach (var subkey in subkeys)
            state = DesCryptProc(state, subkey);

        state = SwapU64Side(state);
        return DesIpInv(state);
    }

    private static void UpdateParam(ref uint param, byte shiftLeft)
    {
        var shiftRight = 28 - shiftLeft;
        param = (param << shiftLeft) | ((param >> shiftRight) & 0xFFFFFFF0u);
    }

    private static ulong DesIp(ulong data) => MapU64(data, Ip);

    private static ulong DesIpInv(ulong data) => MapU64(data, IpInv);

    private static ulong DesCryptProc(ulong state, ulong key)
    {
        var stateHi32 = GetHi32(state);
        var stateLo32 = GetLo32(state);

        state = MapU64(MakeU64(stateHi32, stateHi32), KeyExpansion);
        state ^= key;

        var nextLo32 = SboxTransform(state);
        nextLo32 = MapU32Bits(nextLo32, PBox);
        nextLo32 ^= stateLo32;

        return MakeU64(nextLo32, stateHi32);
    }

    private static uint SboxTransform(ulong state)
    {
        uint result = 0;
        for (var i = 0; i < LargeStateShifts.Length; i++)
        {
            var sboxIdx = (state >> LargeStateShifts[i]) & 0b111111;
            result = (result << 4) | SBoxes[i][sboxIdx];
        }

        return result;
    }

    private static ulong MakeU64(uint hi32, uint lo32) => ((ulong)hi32 << 32) | lo32;

    private static ulong SwapU64Side(ulong value) => (value >> 32) | (value << 32);

    private static uint GetLo32(ulong value) => (uint)value;

    private static uint GetHi32(ulong value) => (uint)(value >> 32);

    private static ulong GetU64ByShiftIdx(byte value) => DesShiftTableCache[value & 0x3f];

    private static void MapBit(ref ulong result, ulong src, byte check, byte set)
    {
        if ((GetU64ByShiftIdx(check) & src) != 0)
            result |= GetU64ByShiftIdx(set);
    }

    private static uint MapU32Bits(uint srcValue, byte[] table)
    {
        ulong result = 0;
        for (var i = 0; i < table.Length; i++)
            MapBit(ref result, srcValue, table[i], (byte)i);

        return (uint)result;
    }

    private static ulong MapU64(ulong srcValue, byte[] table)
    {
        var midIdx = table.Length / 2;
        var tableLo32 = table.AsSpan(0, midIdx);
        var tableHi32 = table.AsSpan(midIdx);

        ulong lo32 = 0;
        ulong hi32 = 0;

        for (var i = 0; i < tableLo32.Length; i++)
            MapBit(ref lo32, srcValue, tableLo32[i], (byte)i);

        for (var i = 0; i < tableHi32.Length; i++)
            MapBit(ref hi32, srcValue, tableHi32[i], (byte)i);

        return MakeU64((uint)hi32, (uint)lo32);
    }
}
