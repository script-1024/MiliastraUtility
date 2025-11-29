using MiliastraUtility.Core.Serialization;

namespace MiliastraUtility.Core.Types;

public struct Guid(uint value) : ISerializable, IDeserializable<Guid>
{
    private static uint Assigned = 0x40000000; // 原神似乎是从 1073741824 开始分配 GUID 的
    public uint Value { get; set; } = value;

    public static implicit operator Guid(uint value) => new(value);
    public static implicit operator Guid(Integer value) => new(value);
    public static implicit operator Varint(Guid guid) => Varint.FromUInt32(guid.Value);

    /// <summary>
    /// 分配一个新的 GUID。
    /// </summary>
    public static Guid Assign() => new(Assigned++);

    public readonly int GetBufferSize() => Varint.GetBufferSize(Value);

    public readonly void Serialize(BufferWriter writer) => Varint.FromUInt32(Value).Serialize(writer);

    public void Deserialize(BufferReader reader) => Value = Varint.FromBuffer(reader).GetValue();

    public static Guid FromBuffer(BufferReader reader) => Varint.FromBuffer(reader).GetValue();
}
