using MiliastraUtility.Core.Serialization;

namespace MiliastraUtility.Core.Types;

/// <summary>
/// 表示资产的特殊类型。
/// </summary>
/// <remarks>大多数情况下应使用 <see cref="AssetSpecialType.Default"/>。</remarks>
public enum AssetSpecialType
{
    /// <summary>
    /// 未知类型
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 默认类型（无特殊类型）
    /// </summary>
    Default = 1,

    /// <summary>
    /// 节点图
    /// </summary>
    NodeGraph = 5,

    /// <summary>
    /// 复合节点
    /// </summary>
    /// <remarks>
    /// 本质上，服务器信号和结构体其实也只是定义了专门用于操作它们的特殊节点而已
    /// </remarks>
    Composite = 23,

    /// <summary>
    /// 镜头配置
    /// </summary>
    Camera = 25,
}

/// <summary>
/// 表示资产的类别。
/// </summary>
public enum AssetCategory
{
    Special       = 0,
    Prefab        = 1,
    Entity        = 2,
    Configuration = 3,
    Terrain       = 5,
    UI            = 8,
    PresetPoint   = 9,
    Structure     = 15,
}

/// <summary>
/// 表示此资产的元信息。
/// </summary>
public struct AssetInfo : ISerializable, IDeserializable<AssetInfo>
{
    /// <summary>
    /// 获取或设置资产的特殊类型。
    /// </summary>
    /// <remarks>
    /// 大多数情况下此字段都应被设为 <see cref="AssetSpecialType.Default"/>
    /// </remarks>
    public AssetSpecialType SpecialType { get; set; }
    private static readonly ProtoTag TagSpecial = new(2, WireType.VARINT);

    /// <summary>
    /// 获取或设置资产的类别。
    /// </summary>
    public AssetCategory Category { get; set; }
    private static readonly ProtoTag TagCategory = new(3, WireType.VARINT);

    /// <summary>
    /// 获取或设置资产的全局唯一标识符。
    /// </summary>
    public Guid Guid { get; set; }
    private static readonly ProtoTag TagGuid = new(4, WireType.VARINT);


    public readonly int GetBufferSize()
    {
        int size = 0;
        if (SpecialType != AssetSpecialType.Unknown) size += 2;
        if (Category != AssetCategory.Special) size += 2;
        return Guid.IsZero ? size : size + 1 + Guid.GetBufferSize();
    }

    public readonly void Serialize(ref BufferWriter writer)
    {
        if (SpecialType != AssetSpecialType.Unknown)
        {
            TagSpecial.Serialize(ref writer);
            writer.WriteByte((byte)SpecialType);
        }

        if (Category != AssetCategory.Special)
        {
            TagCategory.Serialize(ref writer);
            writer.WriteByte((byte)Category);
        }

        if (!Guid.IsZero)
        {
            TagGuid.Serialize(ref writer);
            Guid.Serialize(ref writer);
        }
    }

    public static AssetInfo Deserialize(ref BufferReader reader)
        => Deserialize(ref reader, default);

    public static AssetInfo Deserialize(ref BufferReader reader, AssetInfo self)
    {
        int end = reader.Position + Varint.Deserialize(ref reader).GetValue();

        while (reader.Position < end)
        {
            ProtoTag tag = Varint.Deserialize(ref reader);
            switch (tag.Id)
            {
                case 2:
                    if (tag.Type != WireType.VARINT) break;
                    self.SpecialType = Varint.AsEnum(ref reader, AssetSpecialType.Unknown);
                    continue;
                case 3:
                    if (tag.Type != WireType.VARINT) break;
                    self.Category = Varint.AsEnum(ref reader, AssetCategory.Special);
                    continue;
                case 4:
                    if (tag.Type != WireType.VARINT) break;
                    self.Guid = Guid.Deserialize(ref reader);
                    continue;
                default: break;
            }
            tag.Consume(ref reader);
        }

        return self;
    }
}
