using MiliastraUtility.Core.Serialization;

namespace MiliastraUtility.Core.Types;

/// <summary>
/// 表示资产的特殊类型。
/// </summary>
/// <remarks>大多数情况下应使用 <see cref="AssetSpecialType.Default"/>。</remarks>
public enum AssetSpecialType : byte
{
    Unknown   = 0,
    Default   = 1,
    NodeGraph = 23,
    Camera    = 25,
}

/// <summary>
/// 表示资产的类别。
/// </summary>
public enum AssetCategory : byte
{
    Special     = 0,
    Component   = 1,
    Entity      = 2,
    Config      = 3,
    Terrain     = 5,
    UIControl   = 8,
    PresetPoint = 9,
    Struct      = 15,
}

/// <summary>
/// 表示此资产的元信息。
/// </summary>
public struct AssetInfo : ISerializable, IDeserializable<AssetInfo>
{
    /// <summary>
    /// 获取或设置资产的特殊类型。
    /// </summary>
    /// <remarks>大多数情况下此字段都应被设为 <see cref="AssetSpecialType.Default"/></remarks>
    public AssetSpecialType SpecialType { get; set; }
    private const byte TagSpecial = (2 << 3) | (byte)WireType.VARINT;

    /// <summary>
    /// 获取或设置资产的类别。
    /// </summary>
    public AssetCategory Category { get; set; }
    private const byte TagCategory = (3 << 3) | (byte)WireType.VARINT;

    /// <summary>
    /// 获取或设置资产的全局唯一标识符。
    /// </summary>
    public Guid Guid { get; set; }
    private const byte TagGuid = (4 << 3) | (byte)WireType.VARINT;


    public readonly int GetBufferSize()
    {
        int size = 0;
        if (SpecialType != AssetSpecialType.Unknown) size += 2;
        if (Category != AssetCategory.Special) size += 2;
        return Guid.IsZero ? size : size + 1 + Guid.GetBufferSize();
    }

    public readonly void Serialize(BufferWriter writer)
    {
        if (SpecialType != AssetSpecialType.Unknown)
        {
            writer.WriteByte(TagSpecial);
            writer.WriteByte((byte)SpecialType);
        }

        if (Category != AssetCategory.Special)
        {
            writer.WriteByte(TagCategory);
            writer.WriteByte((byte)Category);
        }

        if (!Guid.IsZero)
        {
            writer.WriteByte(TagGuid);
            Guid.Serialize(writer);
        }
    }

    public void Deserialize(BufferReader reader)
    {
        int end = reader.Position + Varint.FromBuffer(reader).GetValue();
        SpecialType = AssetSpecialType.Unknown;
        Category = AssetCategory.Special;
        Guid = Guid.Zero;

        while (reader.Position < end)
        {
            ProtoTag tag = Varint.FromBuffer(reader);
            switch (tag.Id)
            {
                case 2:
                    if (tag.Type != WireType.VARINT) break;
                    SpecialType = Varint.AsEnum(reader, AssetSpecialType.Unknown);
                    continue;
                case 3:
                    if (tag.Type != WireType.VARINT) break;
                    Category = Varint.AsEnum(reader, AssetCategory.Special);
                    continue;
                case 4:
                    if (tag.Type != WireType.VARINT) break;
                    Guid.Deserialize(reader);
                    continue;
                default: break;
            }
            tag.Consume(reader);
        }
    }

    public static AssetInfo FromBuffer(BufferReader reader)
    {
        var info = new AssetInfo();
        info.Deserialize(reader);
        return info;
    }
}
