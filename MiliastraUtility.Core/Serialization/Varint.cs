using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MiliastraUtility.Core.Serialization;

[InlineArray(10)] // 将 64 位整数编码为 Varint 时最多需要 10 个字节
internal struct VarintBuffer { private byte first; }

public struct Varint : ISerializable, IDeserializable<Varint>
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

    public static Varint FromInt32(int value)
        => value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "不接受负值")
                     : FromUInt32((uint)value);

    public static Varint FromInt64(long value)
        => value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "不接受负值")
                     : FromUInt64((ulong)value);

    public readonly T GetValue<T>() where T : unmanaged
        => (T)GetValue(typeof(T));

    public readonly object GetValue(Type type)
    {
        ulong result = GetValue();
        return type switch
        {
            var _ when type == typeof(bool)   => result != 0,
            var _ when type == typeof(sbyte)  => (sbyte)result,
            var _ when type == typeof(byte)   => (byte)result,
            var _ when type == typeof(short)  => (short)result,
            var _ when type == typeof(ushort) => (ushort)result,
            var _ when type == typeof(int)    => (int)result,
            var _ when type == typeof(uint)   => (uint)result,
            var _ when type == typeof(long)   => (long)result,
            var _ when type == typeof(ulong)  => result,
            _ => throw new NotSupportedException()
        };
    }

    public readonly ulong GetValue()
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

    public void Serialize(ref BufferWriter writer) => writer.WriteSpan(GetSpan());

    public static T Deserialize<T>(ref BufferReader reader) where T : unmanaged
        => Deserialize(ref reader, default).GetValue<T>();

    public static Varint Deserialize(ref BufferReader reader)
        => Deserialize(ref reader, default);

    public static Varint Deserialize(ref BufferReader reader, Varint self)
    {
        self.Size = 0;
        while (self.Size < 10)
        {
            byte data = reader.ReadByte();
            self.Buffer[self.Size++] = data;
            if ((data & 0x80) == 0) return self;
        }
        throw new InvalidDataException("无效的 Varint 编码");
    }

    /// <summary>
    /// 消耗一个 Varint 但不获取其值
    /// </summary>
    /// <param name="reader">读取器</param>
    /// <exception cref="InvalidDataException"></exception>
    public static void Consume(ref BufferReader reader)
    {
        for (int i = 0; i < 10; i++)
        {
            byte data = reader.ReadByte();
            if ((data & 0x80) == 0) return;
        }
        throw new InvalidDataException("无效的 Varint 编码");
    }

    /// <summary>
    /// 将 Varint 解释为枚举类型 TEnum 的值。
    /// </summary>
    /// <remarks>若该值不在 TEnum 的定义范围内，则返回指定的 fallback 值。</remarks>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="fallback">失败值</param>
    public readonly TEnum AsEnum<TEnum>(TEnum fallback) where TEnum : struct, Enum
    {
        var type = typeof(TEnum);
        var value = GetValue(Enum.GetUnderlyingType(type));
        return Enum.IsDefined(type, value) ? (TEnum)Enum.ToObject(type, value) : fallback;
    }

    /// <summary>
    /// 从读取器获取一个 Varint，并将其解释为枚举类型 TEnum 的值。
    /// </summary>
    /// <remarks>若该值不在 TEnum 的定义范围内，则返回指定的 fallback 值。</remarks>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="reader">读取器</param>
    /// <param name="fallback">失败值</param>
    public static TEnum AsEnum<TEnum>(ref BufferReader reader, TEnum fallback) where TEnum : struct, Enum
    {
        var v = Deserialize(ref reader);
        return v.AsEnum(fallback);
    }
}
