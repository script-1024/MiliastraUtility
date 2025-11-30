using MiliastraUtility.Core.Serialization;
using MiliastraUtility.Core.Types;
using System.Text;
using System.Text.Json.Serialization;

namespace MiliastraUtility.Core;

public sealed class GiaFile : GiFile
{
    [JsonPropertyOrder(0)]
    public override GiFileType Type => GiFileType.Gia;

    /// <summary>
    /// 获取资产列表。
    /// </summary>
    /// <remarks>id = 1</remarks>
    [JsonPropertyOrder(1)]
    public List<Asset> Assets { get; set; } = [];
    private static readonly ProtoTag TagAssets = new(1, WireType.LENGTH);
    private Integer[] szAssets = [];
    private bool hasAssets = false;

    /// <summary>
    /// 获取关联资产列表。
    /// </summary>
    /// <remarks>id = 2</remarks>
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Asset> RelatedAssets { get; set; } = [];
    private static readonly ProtoTag TagRelatedAssets = new(2, WireType.LENGTH);
    private Integer[] szRelatedAssets = [];
    private bool hasRelatedAssets = false;

    /// <summary>
    /// 获取或设置导出信息字符串。
    /// </summary>
    /// <remarks>id = 3</remarks>
    [JsonPropertyOrder(3)]
    public string ExportInfo { get; set; } = string.Empty;
    private static readonly ProtoTag TagExportInfo = new(3, WireType.LENGTH);
    private Integer szExportInfo = 0;

    private int GetBufferSize()
    {
        int size = 24; // 文件头 + 尾部标记

        hasAssets = false;
        if (Assets.Count > 0)
        {
            szAssets = new Integer[Assets.Count];
            for (int i = 0; i < Assets.Count; i++)
            {
                szAssets[i] = Assets[i].GetBufferSize();
                if (szAssets[i] == 0) continue; // 跳过空对象
                size += 1 + Varint.GetBufferSize((uint)szAssets[i]) + szAssets[i];
                hasAssets = true;
            }
        }

        hasRelatedAssets = false;
        if (RelatedAssets.Count > 0)
        {
            szRelatedAssets = new Integer[RelatedAssets.Count];
            for (int i = 0; i < RelatedAssets.Count; i++)
            {
                szRelatedAssets[i] = RelatedAssets[i].GetBufferSize();
                if (szRelatedAssets[i] == 0) continue; // 跳过空对象
                size += 1 + Varint.GetBufferSize((uint)szRelatedAssets[i]) + szRelatedAssets[i];
                hasRelatedAssets = true;
            }
        }

        szExportInfo = Encoding.UTF8.GetByteCount(ExportInfo);
        if (szExportInfo != 0) size += 1 + Varint.GetBufferSize((uint)szExportInfo) + szExportInfo;

        return size;
    }

    /// <summary>
    /// 从指定路径加载 GIA 文件。
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <exception cref="InvalidDataException">无效的文件</exception>
    public static GiaFile ReadFromFile(string path)
    {
        var instance = new GiaFile();
        var reader = ReadFromFile(path, instance, out int length);

        int end = reader.Position + length;
        while (reader.Position < end) // 读取所有字段
        {
            ProtoTag tag = Varint.Deserialize(ref reader);
            switch (tag.Id) // 根据标签 ID 解析字段，忽略未知标签
            {
                case 1: {
                    if (tag.Type != WireType.LENGTH) break;
                    var asset = Asset.Deserialize(ref reader);
                    instance.Assets.Add(asset);
                    continue;
                }
                case 2: {
                    if (tag.Type != WireType.LENGTH) break;
                    var asset = Asset.Deserialize(ref reader);
                    instance.RelatedAssets.Add(asset);
                    continue;
                }
                case 3:
                    if (tag.Type != WireType.LENGTH) break;
                    instance.ExportInfo = reader.ReadString();
                    continue;
                default: break; // 忽略未知字段
            }
            // 消耗一个未知标签
            tag.Consume(ref reader);
        }

        return instance;
    }

    /// <summary>
    /// 保存 GIA 文件到指定路径。
    /// </summary>
    /// <param name="path">文件路径</param>
    public override void WriteToFile(string path)
    {
        int size = GetBufferSize();
        var writer = new BufferWriter(new byte[size]);
        WriteToFile(this, ref writer); // 写入元信息

        if (hasAssets)
        {
            for (int i = 0; i < Assets.Count; i++)
            {
                if (szAssets[i] == 0) continue; // 跳过空对象
                TagAssets.Serialize(ref writer);
                Varint.FromUInt32(szAssets[i]).Serialize(ref writer);
                Assets[i].Serialize(ref writer);
            }
        }

        if (hasRelatedAssets)
        {
            for (int i = 0; i < RelatedAssets.Count; i++)
            {
                if (szRelatedAssets[i] == 0) continue; // 跳过空对象
                TagRelatedAssets.Serialize(ref writer);
                Varint.FromUInt32(szRelatedAssets[i]).Serialize(ref writer);
                RelatedAssets[i].Serialize(ref writer);
            }
        }

        if (szExportInfo != 0)
        {
            TagExportInfo.Serialize(ref writer);
            Varint.FromUInt32(szExportInfo).Serialize(ref writer);
            writer.WriteString(ExportInfo, szExportInfo); // 已经计算过长度了，不用再计算一次
        }

        File.WriteAllBytes(path, writer.Span);
    }
}
