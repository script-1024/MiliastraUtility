namespace MiliastraUtility.Core.Serialization;

/// <summary>
/// 表示数据的线路类型。
/// </summary>
public enum WireType
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
public struct ProtobufTag
{
    public uint Id { get; set; }
    public WireType WireType { get; set; }

    public static implicit operator ProtobufTag(Varint varint)
    {
        ulong value = varint.GetValue();
        return new ProtobufTag
        {
            Id = (uint)(value >> 3),
            WireType = (WireType)(value & 0b111)
        };
    }

    public static implicit operator Varint(ProtobufTag tag)
    {
        ulong value = ((ulong)tag.Id << 3) | (uint)tag.WireType;
        return Varint.FromUInt64(value);
    }
}
