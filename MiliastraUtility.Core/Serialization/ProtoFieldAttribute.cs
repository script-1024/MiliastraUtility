namespace MiliastraUtility.Core.Serialization;

/// <summary>
/// 指示被修饰属性成为消息字段
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ProtoFieldAttribute : Attribute
{
    /// <summary>
    /// 初始化一个新的 <see cref="ProtoFieldAttribute"/> 实例
    /// </summary>
    /// <param name="id">字段编号</param>
    /// <param name="kind">
    /// 字段类型。若此值被设为 <see cref="ValueKind.Wrapped"/>，还必须使用 <see cref="WrappedFieldAttribute"/> 修饰该属性。
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public ProtoFieldAttribute(int id, ValueKind kind)
    {
        if (id < 1 || id > 65535)
            throw new ArgumentOutOfRangeException(nameof(id), "无效的字段编号，接受范围：[1, 65535]");
        Id = id;
        Kind = kind;
    }

    /// <summary>
    /// 字段编号
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// 字段类型
    /// </summary>
    /// <remarks>
    /// 若此值被设为 <see cref="ValueKind.Wrapped"/>，还必须使用 <see cref="WrappedFieldAttribute"/> 修饰该属性。
    /// </remarks>
    public ValueKind Kind { get; }
}

/// <summary>
/// 指示被修饰属性成为被包装的消息字段
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class WrappedFieldAttribute : Attribute
{
    /// <summary>
    /// 初始化一个新的 <see cref="WrappedFieldAttribute"/> 实例
    /// </summary>
    /// <param name="id">字段编号</param>
    /// <param name="kind">字段类型，不接受 <see cref="ValueKind.Wrapped"/></param>
    /// <param name="levels">每层包装的编号</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public WrappedFieldAttribute(int id, ValueKind kind, params int[] levels)
    {
        if (id < 1 || id > 65535)
            throw new ArgumentOutOfRangeException(nameof(id), "无效的字段编号，接受范围：[1, 65535]");
        if (kind == ValueKind.Wrapped)
            throw new ArgumentException("不接受 ValueKind.Wrapped 类型");
        Id = id;
        Kind = kind;
        Levels = levels;
    }

    /// <summary>
    /// 字段编号
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// 字段类型
    /// </summary>
    public ValueKind Kind { get; }

    /// <summary>
    /// 每层包装的编号
    /// </summary>
    public int[] Levels { get; }
}

public enum ValueKind
{
    /// <summary>
    /// 表示一个内容总是为空的消息字段，该属性在反序列化时将被跳过
    /// </summary>
    Null = 0,

    /// <summary>
    /// 表示一个可变长度整数
    /// </summary>
    Varint = 1,

    /// <summary>
    /// 表示一个固定 32 位长的数值
    /// </summary>
    Fixed32 = 2,

    /// <summary>
    /// 表示一个固定 64 位长的数值
    /// </summary>
    Fixed64 = 3,

    /// <summary>
    /// 表示一个字符串
    /// </summary>
    String = 4,

    /// <summary>
    /// 表示一个对象
    /// </summary>
    Object = 5,

    /// <summary>
    /// 表示一个列表
    /// </summary>
    List = 6,

    /// <summary>
    /// 表示一个被包装的消息字段
    /// </summary>
    Wrapped = 7
}
