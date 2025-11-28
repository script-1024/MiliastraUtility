using MiliastraUtility.Core.Serialization;

namespace MiliastraUtility.Core;

public enum GiFileType { Unknown = 0, Gip = 1, Gil = 2, Gia = 3, Gir = 4 }

public abstract class GiFile
{
    /// <summary>
    /// 获取文件格式的版本号。
    /// </summary>
    public uint Version { get; protected set; } = 1;

    /// <summary>
    /// 获取此 GI 文件的类型。
    /// </summary>
    public abstract GiFileType Type { get; }

    /// <summary>
    /// 获取用于校验的头部魔数。
    /// </summary>
    public static uint HeadMagicNumber => 0x0326;

    /// <summary>
    /// 获取用于校验的尾部魔数。
    /// </summary>
    public static uint TailMagicNumber => 0x0679;

    /// <summary>
    /// 从指定路径读取 GI 文件元信息的公共方法。
    /// </summary>
    /// <remarks>继承类型应先调用此方法获取一个合法的读取器。</remarks>
    /// <typeparam name="T">实例的类型，应为一个继承自 <see cref="GiFile"/> 的类型</typeparam>
    /// <param name="path">文件路径</param>
    /// <param name="instance">实例对象</param>
    /// <param name="length">内容长度</param>
    /// <exception cref="InvalidDataException"></exception>
    protected static BufferReader ReadFromFile<T>(string path, T instance, out int length) where T : GiFile
    {
        ReadOnlySpan<byte> data = File.ReadAllBytes(path);
        if (data.Length < 24) throw new InvalidDataException("文件过小，无法读取数据。");

        var reader = new BufferReader(data);

        // 文件大小
        uint fileSize = reader.ReadUInt32BE();
        if (fileSize + 4 != data.Length) throw new InvalidDataException("文件大小与头部信息不符。");

        // 版本编号
        instance.Version = reader.ReadUInt32BE();

        // 头部魔数
        uint headMagic = reader.ReadUInt32BE();
        if (headMagic != HeadMagicNumber) throw new InvalidDataException("文件头部魔数不匹配，可能不是有效的 GI 文件。");

        // 文件类型
        GiFileType type = reader.ReadUInt32BE() switch
        {
            1 => GiFileType.Gip,
            2 => GiFileType.Gil,
            3 => GiFileType.Gia,
            4 => GiFileType.Gir,
            _ => GiFileType.Unknown
        };
        if (type != instance.Type) throw new InvalidDataException("文件类型不正确。");

        // 内容长度
        length = reader.ReadInt32BE();
        if (length + 24 != data.Length) throw new InvalidDataException("内容长度与文件大小不符。");

        // 尾部魔数
        int current = reader.Position;
        reader.Seek(-4, SeekOrigin.End);
        uint tailMagic = reader.ReadUInt32BE();
        if (tailMagic != TailMagicNumber) throw new InvalidDataException("文件尾部魔数不匹配，可能不是有效的 GI 文件。");

        // 通过校验，返回到内容起始位置，交给继承类型处理
        reader.Seek(current, SeekOrigin.Begin);
        return reader;
    }

    /// <summary>
    /// 在指定路径创建并写入 GI 文件元信息的公共方法。
    /// </summary>
    /// <remarks>继承类型在调用此方法前应负责确保导出路径合法，并提供一个空间足够的写入器，保留文件头部和尾部共 24 字节。</remarks>
    /// <typeparam name="T">实例的类型，应为一个继承自 <see cref="GiFile"/> 的类型</typeparam>
    /// <param name="path">文件路径</param>
    /// <param name="instance">>实例对象</param>
    /// <param name="writer">写入器</param>
    /// <exception cref="InvalidDataException"></exception>
    protected static void WriteToFile<T>(string path, T instance, BufferWriter writer) where T : GiFile
    {
        if (writer.Length < 24) throw new InvalidDataException("文件过小，无法保存数据。");
        uint length = (uint)writer.Length;

        writer.Seek(0, SeekOrigin.Begin);
        writer.WriteUInt32BE(length - 4);          // 文件大小
        writer.WriteUInt32BE(instance.Version);    // 版本编号
        writer.WriteUInt32BE(HeadMagicNumber);     // 头部魔数
        writer.WriteUInt32BE((uint)instance.Type); // 文件类型
        writer.WriteUInt32BE(length - 24);         // 内容长度

        writer.Seek(-4, SeekOrigin.End);
        writer.WriteUInt32BE(TailMagicNumber);     // 尾部魔数
        File.WriteAllBytes(path, writer.Span);
    }
}
