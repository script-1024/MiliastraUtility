using System.Text.Json;
using System.Text.Json.Serialization;
using MiliastraUtility.Core.Serialization;

namespace MiliastraUtility.Core.Types;

[JsonConverter(typeof(JsonGuidConverter))]
public struct Guid(uint value) : ISerializable, IDeserializable<Guid>
{
    private static uint Assigned = 0x40000000; // 原神似乎是从 1073741824 开始分配 GUID 的
    public static readonly Guid Zero = new(0);

    public readonly bool IsZero => Value == 0;
    public uint Value { get; set; } = value;

    public static implicit operator Guid(uint value) => new(value);
    public static implicit operator Varint(Guid guid) => Varint.FromUInt32(guid.Value);

    /// <summary>
    /// 分配一个新的 GUID。
    /// </summary>
    public static Guid Assign() => new(Assigned++);

    public readonly int GetBufferSize() => Varint.GetBufferSize(Value);

    public readonly void Serialize(ref BufferWriter writer)
        => Varint.FromUInt32(Value).Serialize(ref writer);

    public static Guid Deserialize(ref BufferReader reader)
        => Deserialize(ref reader, default);

    public static Guid Deserialize(ref BufferReader reader, Guid self)
        => Varint.Deserialize<uint>(ref reader);
}

internal sealed class JsonGuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetUInt32();

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Value);
}
