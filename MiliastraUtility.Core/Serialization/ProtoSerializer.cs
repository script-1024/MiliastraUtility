using System.Collections;
using System.Collections.Frozen;
using System.Reflection;

namespace MiliastraUtility.Core.Serialization;

public static class ProtoSerializer
{
    /// <summary>
    /// 取得字段编号和属性信息的映射字典
    /// </summary>
    private static FrozenDictionary<int, PropertyInfo> GetFieldMap(Type type)
    {
        var result = new Dictionary<int, PropertyInfo>();
        var properties = from p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         where p.GetCustomAttribute<ProtoFieldAttribute>() != null
                         select p;

        foreach (var p in properties)
        {
            var attribute = p.GetCustomAttribute<ProtoFieldAttribute>()!;
            result.Add(attribute.Id, p);
        }

        return result.ToFrozenDictionary();
    }

    /// <summary>
    /// 取得集合元素的类型
    /// </summary>
    private static Type? GetElementType(Type type)
        =>  type.IsArray ? type.GetElementType()! : 
            type.IsGenericType ? type.GetGenericArguments()[0] : null;

    /// <summary>
    /// 尝试将值插入列表中，若列表为 null 会先进行初始化
    /// </summary>
    private static void AddToList(PropertyInfo prop, object obj, object? element)
    {
        var list = prop.GetValue(obj) as IList;
        if (list is null)
        {
            Type elementType = GetElementType(prop.PropertyType)!;
            Type listType = typeof(List<>).MakeGenericType(elementType);
            list = (IList)Activator.CreateInstance(listType)!;
            prop.SetValue(obj, list);
        }
        list.Add(element);
    }

    /// <summary>
    /// 尝试解析数值，将根据 <see cref="ValueKind"/> 的值选择不同的解析分支
    /// </summary>
    private static object? ParseValue(Type type, ref BufferReader reader, ProtoTag tag, ValueKind kind)
    {
        switch (kind)
        {
            case ValueKind.Null:
                if (tag.Type != WireType.LENGTH) throw new InvalidDataException();
                tag.Consume(ref reader);
                break;

            case ValueKind.Varint:
                if (tag.Type != WireType.VARINT) throw new InvalidDataException();
                return Varint.Deserialize(ref reader).GetValue(type);

            case ValueKind.Fixed32:
                if (tag.Type != WireType.FIXED32) throw new InvalidDataException();
                if (type == typeof(int)) return reader.ReadInt32LE();
                if (type == typeof(uint)) return reader.ReadUInt32LE();
                if (type == typeof(float)) return reader.ReadFloat();
                throw new InvalidOperationException();

            case ValueKind.Fixed64:
                if (tag.Type != WireType.FIXED64) throw new InvalidDataException();
                if (type == typeof(long)) return reader.ReadInt64LE();
                if (type == typeof(ulong)) return reader.ReadUInt64LE();
                if (type == typeof(double)) return reader.ReadDouble();
                throw new InvalidOperationException();

            case ValueKind.String:
                if (tag.Type != WireType.LENGTH) throw new InvalidDataException();
                return reader.ReadString();

            case ValueKind.Object:
                if (tag.Type != WireType.LENGTH) throw new InvalidDataException();
                return Deserialize(type, ref reader, Varint.Deserialize<int>(ref reader));
            
            case ValueKind.List:
                if (tag.Type != WireType.LENGTH) throw new InvalidDataException();
                if (!typeof(IList).IsAssignableFrom(type)) throw new InvalidOperationException();
                return Deserialize(GetElementType(type)!, ref reader, Varint.Deserialize<int>(ref reader));
        }
        return null;
    }

    /// <summary>
    /// 拆除包装器，返回内部目标字段所属的标签和跳出包装器后的位置，并使读取器指向目标数据等待进一步解析
    /// </summary>
    private static (ProtoTag?, int) Unwrap(ref BufferReader reader, WrappedFieldAttribute attr)
    {
        int len = Varint.Deserialize<int>(ref reader);
        int ret = reader.Position + len; // 保存跳出该包装器后的位置
        foreach (int level in attr.Levels) // 找到位于最深处的目标字段
        {
            int end = reader.Position + len;
            while (reader.Position < end)
            {
                ProtoTag tag = Varint.Deserialize(ref reader);
                if (tag.Id == level)
                {
                    if (tag.Type != WireType.LENGTH) throw new InvalidDataException();
                    len = Varint.Deserialize<int>(ref reader);
                    break;
                }
                tag.Consume(ref reader);
            }
        }
        while (reader.Position < ret)
        {
            ProtoTag tag = Varint.Deserialize(ref reader);
            if (tag.Id == attr.Id) return (tag, ret);
            tag.Consume(ref reader);
        }
        return (null, ret);
    }

    /// <summary>
    /// 从读取器反序列化成指定类型的实例
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="reader">读取器</param>
    /// <exception cref="InvalidDataException"/>
    /// <exception cref="InvalidOperationException"/>
    public static T? Deserialize<T>(ref BufferReader reader) where T : notnull, new()
        => (T?) Deserialize(typeof(T), ref reader, reader.Length);

    /// <summary>
    /// 从读取器反序列化成指定类型的实例，接受一个消息长度参数
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="reader">读取器</param>
    /// <param name="length">消息长度</param>
    /// <exception cref="InvalidDataException"/>
    /// <exception cref="InvalidOperationException"/>
    public static T? Deserialize<T>(ref BufferReader reader, int length) where T : notnull, new()
        => (T?)Deserialize(typeof(T), ref reader, length);

    /// <summary>
    /// 从读取器反序列化成指定类型的实例，接受一个消息长度参数
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="reader">读取器</param>
    /// <param name="length">消息长度</param>
    /// <exception cref="InvalidDataException"/>
    /// <exception cref="InvalidOperationException"/>
    public static object? Deserialize(Type type, ref BufferReader reader, int length)
    {
        var map = GetFieldMap(type);
        int end = reader.Position + length;
        object obj = Activator.CreateInstance(type)!;
        while (reader.Position < end)
        {
            ProtoTag tag = Varint.Deserialize(ref reader);
            if (!map.TryGetValue(tag.Id, out PropertyInfo? prop) || !prop.CanWrite)
            {
                tag.Consume(ref reader); // 跳过未知或不可写入的属性
                continue;
            }

            object? value = null;
            var attr = prop.GetCustomAttribute<ProtoFieldAttribute>()!;
            if (attr.Kind == ValueKind.Wrapped)
            {
                if (tag.Type != WireType.LENGTH) throw new InvalidDataException();
                var wrapperInfo = prop.GetCustomAttribute<WrappedFieldAttribute>() ??
                    throw new InvalidOperationException("缺少修饰特性 WrappedFieldAttribute");

                var (target, ret) = Unwrap(ref reader, wrapperInfo);
                if (target != null)
                    value = ParseValue(prop.PropertyType, ref reader, (ProtoTag)target, wrapperInfo.Kind);
                reader.Seek(ret, SeekOrigin.Begin);
            }
            else value = ParseValue(prop.PropertyType, ref reader, tag, attr.Kind);

            if (value is null) continue;
            if (attr.Kind == ValueKind.List) AddToList(prop, obj, value);
            else prop.SetValue(obj, value);
        }
        return obj;
    }

    public static int GetBufferSize<T>()
    {
        throw new NotImplementedException();
    }

    public static void Serialize<T>(ref BufferWriter writer, T value) where T : notnull
    {
        throw new NotImplementedException();
    }
}
