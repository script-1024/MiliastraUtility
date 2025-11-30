using MiliastraUtility.Core.Serialization;
using MiliastraUtility.Core.Types;

namespace MiliastraUtility.Core;

public sealed class GiaFile : GiFile
{
    public override GiFileType Type => GiFileType.Gia;

    /// <summary>
    /// 获取资产列表。
    /// </summary>
    /// <remarks>id = 1</remarks>
    public List<Asset> Assets { get; private set; } = [];

    /// <summary>
    /// 获取关联资产列表。
    /// </summary>
    /// <remarks>id = 2</remarks>
    public List<Asset> RelatedAssets { get; private set; } = [];

    /// <summary>
    /// 获取或设置导出信息字符串。
    /// </summary>
    /// <remarks>id = 3</remarks>
    public string ExportInfo { get; set; } = string.Empty;

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
}
