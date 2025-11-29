using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MiliastraUtility.Core.Serialization;

[InlineArray(10)] // 将 64 位整数编码为 Varint 时最多需要 10 个字节
internal struct VarintBuffer { private byte first; }

public ref struct Varint : ISerializable, IDeserializable<Varint>
{
    private int Size;
    private bool IsZigZagged;
    private VarintBuffer Buffer;

    public bool IsZero { get; private set; }

    public Span<byte> GetSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<VarintBuffer, byte>(ref Buffer), Size);

    private static uint ZigZagEncode32(int value) => (uint)((value << 1) ^ (value >> 31));

    private static ulong ZigZagEncode64(long value) => (ulong)((value << 1) ^ (value >> 63));

    private static ulong ZigZagDecode(ulong value) => (value >> 1) ^ unchecked(~(value & 1) + 1);

    public static Varint FromUInt32(uint value) => FromUInt64(value);

    public static Varint FromUInt64(ulong value)
    {
        var v = new Varint();

        if (value == 0)
        {
            v.Size = 1;
            v.IsZero = true;
            v.Buffer[0] = 0;
            return v;
        }

        while (value != 0)
        {
            byte data = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0) data |= 0x80;
            v.Buffer[v.Size++] = data;
        }

        return v;
    }

    public static Varint FromSInt32(int value)
    {
        var v = FromUInt32(ZigZagEncode32(value));
        v.IsZigZagged = true;
        return v;
    }

    public static Varint FromSInt64(long value)
    {
        var v = FromUInt64(ZigZagEncode64(value));
        v.IsZigZagged = true;
        return v;
    }

    public readonly Integer GetValue()
    {
        ulong result = 0;
        for (int i = 0; i < Size; i++)
        {
            byte data = (byte)(Buffer[i] & 0x7F);
            result |= (ulong)(data) << (i * 7);
            if (Buffer[i] >> 7 == 0) break;
        }
        return IsZigZagged ? ZigZagDecode(result) : result;
    }

    public readonly int GetBufferSize() => Size;

    public static int GetBufferSize(uint value)
    {
        if (value == 0) return 1; // 当 value 为零时也需要一字节的存储空间
        int bits = 32 - BitOperations.LeadingZeroCount(value);
        return (bits + 6) / 7;
    }

    public static int GetBufferSize(ulong value)
    {
        if (value == 0) return 1; // 当 value 为零时也需要一字节的存储空间
        int bits = 64 - BitOperations.LeadingZeroCount(value);
        return (bits + 6) / 7;
    }

    public void Serialize(BufferWriter writer) => writer.WriteSpan(GetSpan());

    public void Deserialize(BufferReader reader)
    {
        Size = 0;
        while (Size < 10)
        {
            byte data = reader.ReadByte();
            Buffer[Size++] = data;
            if ((data & 0x80) == 0) return;
        }
        throw new InvalidDataException("无效的 Varint 编码");
    }

    public static Varint FromBuffer(BufferReader reader)
    {
        var v = new Varint();
        v.Deserialize(reader);
        return v;
    }
}
