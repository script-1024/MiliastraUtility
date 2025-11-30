using System.Text;
using System.Text.Json.Serialization;
using MiliastraUtility.Core.Serialization;

namespace MiliastraUtility.Core.Types;

public sealed class Asset : ISerializable, IDeserializable<Asset>
{
    // 1: 资产的元信息
    [JsonPropertyOrder(1)]
    public AssetInfo Info { get; set; }
    private static readonly ProtoTag TagInfo = new(1, WireType.LENGTH);
    private Integer szInfo = 0;

    // 2: 关联资产的元信息列表
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AssetInfo>? RelatedInfo { get; set; }
    private static readonly ProtoTag TagRelatedInfo = new(2, WireType.LENGTH);
    private Integer[] szRelated = [];
    private bool hasRelated = false;

    // 3: 资产名称
    [JsonPropertyOrder(3)]
    public string Name { get; set; } = string.Empty;
    private static readonly ProtoTag TagName = new(3, WireType.LENGTH);
    private Integer szName = 0;

    // 5: 资产类型
    [JsonPropertyOrder(5)]
    public AssetType Type { get; set; }
    private static readonly ProtoTag TagType = new(5, WireType.VARINT);

    // 13: 节点图
    // 19: 界面控件组

    /// <summary>
    /// 获取序列化此对象所需的缓冲区大小。
    /// </summary>
    public int GetBufferSize()
    {
        int size = 0;
        szInfo = Info.GetBufferSize();
        if (szInfo != 0) size += 1 + Varint.GetBufferSize((uint)szInfo) + szInfo;

        hasRelated = false;
        if (RelatedInfo?.Count > 0)
        {
            szRelated = new Integer[RelatedInfo.Count];
            for (int i = 0; i < RelatedInfo.Count; i++)
            {
                szRelated[i] = RelatedInfo[i].GetBufferSize();
                if (szRelated[i] == 0) continue; // 跳过空对象
                size += 1 + Varint.GetBufferSize((uint)szRelated[i]) + szRelated[i];
                hasRelated = true;
            }
        }

        szName = Encoding.UTF8.GetByteCount(Name);
        if (szName != 0) size += 1 + Varint.GetBufferSize((uint)szName) + szName;

        if (Type != AssetType.Unknown) size += 2;
        return size;
    }

    /// <summary>
    /// 将此对象序列化到指定的缓冲区写入器中。
    /// </summary>
    /// <remarks>
    /// 呼叫此方法前必须先调用 <see cref="GetBufferSize"/> 以确保缓冲区有足够的空间。
    /// </remarks>
    public void Serialize(ref BufferWriter writer)
    {
        if (szInfo != 0)
        {
            TagInfo.Serialize(ref writer);
            Varint.FromUInt32(szInfo).Serialize(ref writer);
            Info.Serialize(ref writer);
        }

        if (hasRelated)
        {
            for (int i = 0; i < RelatedInfo!.Count; i++)
            {
                if (szRelated[i] == 0) continue; // 跳过空对象
                TagRelatedInfo.Serialize(ref writer);
                Varint.FromUInt32(szRelated[i]).Serialize(ref writer);
                RelatedInfo[i].Serialize(ref writer);
            }
        }

        if (szName != 0)
        {
            TagName.Serialize(ref writer);
            Varint.FromUInt32(szName).Serialize(ref writer);
            writer.WriteString(Name, szName); // 已经计算过长度了，不用再计算一次
        }

        if (Type != AssetType.Unknown)
        {
            TagType.Serialize(ref writer);
            writer.WriteByte((byte)Type);
        }
    }

    public static Asset Deserialize(ref BufferReader reader)
        => Deserialize(ref reader, new());

    public static Asset Deserialize(ref BufferReader reader, Asset self)
    {
        int length = Varint.Deserialize(ref reader).GetValue();
        int end = reader.Position + length;

        while (reader.Position < end)
        {
            ProtoTag tag = Varint.Deserialize(ref reader);
            switch (tag.Id)
            {
                case 1:
                    if (tag.Type != WireType.LENGTH) break;
                    self.Info = AssetInfo.Deserialize(ref reader, self.Info);
                    continue;
                case 2:
                    if (tag.Type != WireType.LENGTH) break;
                    var info = AssetInfo.Deserialize(ref reader);
                    self.RelatedInfo ??= [];
                    self.RelatedInfo.Add(info);
                    continue;
                case 3:
                    if (tag.Type != WireType.LENGTH) break;
                    self.Name = reader.ReadString();
                    continue;
                case 5:
                    if (tag.Type != WireType.VARINT) break;
                    self.Type = Varint.AsEnum(ref reader, AssetType.Unknown);
                    continue;
                default: break;
            }
            tag.Consume(ref reader);
        }

        return self;
    }
}
