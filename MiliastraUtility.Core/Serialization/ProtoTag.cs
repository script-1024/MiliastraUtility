namespace MiliastraUtility.Core.Serialization;

/// <summary>
/// 表示数据的线路类型。
/// </summary>
public enum WireType : byte
{
    VARINT = 0,
    FIXED64 = 1,
    LENGTH = 2,
    GROUP_START = 3,
    GROUP_END = 4,
    FIXED32 = 5
}

/// <summary>
/// 表示 Protobuf 标签。
/// </summary>
public readonly struct ProtoTag : ISerializable
{
    public uint Id { get; init; }
    public WireType Type { get; init; }
    public uint Value => (Id << 3) | (byte)Type;

    public ProtoTag(uint id, WireType type) { Id = id; Type = type; }

    public ProtoTag(uint value)
    {
        Id = value >> 3;
        Type = (value & 0b111) switch
        {
            0 => WireType.VARINT,
            1 => WireType.FIXED64,
            2 => WireType.LENGTH,
            5 => WireType.FIXED32,
            _ => throw new NotSupportedException()
        };
    }

    public static implicit operator ProtoTag(uint value) => new(value);
    public static implicit operator ProtoTag(Varint varint) => new(varint.GetValue());
    public static implicit operator Varint(ProtoTag tag) => Varint.FromUInt32(tag.Value);

    /// <summary>
    /// 消耗一个无效或未知的标签
    /// </summary>
    public void Consume(BufferReader reader)
    {
        switch (Type)
        {
            case WireType.VARINT:
                _ = Varint.FromBuffer(reader);
                break;
            case WireType.FIXED64:
                reader.Seek(8, SeekOrigin.Current);
                break;
            case WireType.LENGTH:
                int length = Varint.FromBuffer(reader).GetValue();
                reader.Seek(length, SeekOrigin.Current);
                break;
            case WireType.FIXED32:
                reader.Seek(4, SeekOrigin.Current);
                break;
            default: throw new NotSupportedException();
        }
    }

    public int GetBufferSize() => Varint.GetBufferSize(Value);

    public void Serialize(BufferWriter writer) => Varint.FromUInt32(Value).Serialize(writer);
}
