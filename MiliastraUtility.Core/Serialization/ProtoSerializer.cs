using System.Collections;
using System.Collections.Frozen;
using System.Reflection;
using System.Text;

namespace MiliastraUtility.Core.Serialization;

public static class ProtoSerializer
{
    /// <summary>
    /// 取得属性信息
    /// </summary>
    private static IEnumerable<PropertyInfo> GetProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(p => p.GetCustomAttribute<ProtoFieldAttribute>() != null);

    /// <summary>
    /// 取得字段编号和属性信息的映射字典
    /// </summary>
    private static FrozenDictionary<int, PropertyInfo> GetFieldMap(Type type)
    {
        var result = new Dictionary<int, PropertyInfo>();
        var properties = GetProperties(type);

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
        if (prop.GetValue(obj) is not IList list)
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
    /// 将枚举值转为 ulong，以便稍后可以用 Varint 将其序列化
    /// </summary>
    private static ulong EnumToUInt64(Type enumType, object value)
    {
        Type type = Enum.GetUnderlyingType(enumType);
        return type switch
        {
            var _ when type == typeof(sbyte)  => (ulong)(sbyte)value,
            var _ when type == typeof(byte)   => (byte)value,
            var _ when type == typeof(short)  => (ulong)(short)value,
            var _ when type == typeof(ushort) => (ushort)value,
            var _ when type == typeof(int)    => (ulong)(int)value,
            var _ when type == typeof(uint)   => (uint)value,
            var _ when type == typeof(long)   => (ulong)(long)value,
            var _ when type == typeof(ulong)  => (ulong)value,
            _ => throw new NotSupportedException()
        };
    }

    /// <summary>
    /// 取得最适合目标类型的 <see cref="ValueKind"/>
    /// </summary>
    private static ValueKind GetValueKind(Type type)
        => type switch
        {
            var t when t.IsEnum            => ValueKind.Varint,
            var t when t == typeof(bool)   => ValueKind.Varint,
            var t when t == typeof(sbyte)  => ValueKind.Varint,
            var t when t == typeof(byte)   => ValueKind.Varint,
            var t when t == typeof(short)  => ValueKind.Varint,
            var t when t == typeof(ushort) => ValueKind.Varint,
            var t when t == typeof(int)    => ValueKind.Varint,
            var t when t == typeof(uint)   => ValueKind.Varint,
            var t when t == typeof(long)   => ValueKind.Varint,
            var t when t == typeof(ulong)  => ValueKind.Varint,
            var t when t == typeof(float)  => ValueKind.Fixed32,
            var t when t == typeof(double) => ValueKind.Fixed64,
            var t when t == typeof(string) => ValueKind.String,
            var t when t.IsAssignableTo(typeof(IList)) => ValueKind.List,
            _ => ValueKind.Object
        };

    /// <summary>
    /// 计算目标值所需的缓冲区大小
    /// </summary>
    private static int GetBufferSizeOfValue(ValueKind kind, Type type, object value)
    {
        switch (kind)
        {
            case ValueKind.Varint:
                switch (value)
                {
                    case bool bl:
                        return (bl == false) ? 0 : 1;

                    case sbyte sb:
                        return (sb == 0) ? 0 : Varint.GetBufferSize((uint)sb);

                    case byte b:
                        return (b == 0) ? 0 : Varint.GetBufferSize(b);

                    case short s:
                        return (s == 0) ? 0 : Varint.GetBufferSize((uint)s);

                    case ushort us:
                        return (us == 0) ? 0 : Varint.GetBufferSize(us);

                    case int i:
                        return (i == 0) ? 0 : Varint.GetBufferSize((uint)i);

                    case uint ui:
                        return (ui == 0) ? 0 : Varint.GetBufferSize(ui);

                    case long l:
                        return (l == 0) ? 0 : Varint.GetBufferSize((ulong)l);

                    case ulong ul:
                        return (ul == 0) ? 0 : Varint.GetBufferSize(ul);

                    default:
                    {
                        if (!type.IsEnum)
                            throw new InvalidOperationException("无效的类型声明：ValueKind.Varint 只能用于枚举或整数类型的属性上");
                        
                        ulong u = EnumToUInt64(type, value);
                        return (u == 0) ? 0 : Varint.GetBufferSize(u);
                    }
                }
            case ValueKind.Fixed32:
                return value switch
                {
                    int i => (i == 0) ? 0 : 4,
                    uint u => (u == 0) ? 0 : 4,
                    float f => (f == 0) ? 0 : 4,
                    _ => 0,
                };
            case ValueKind.Fixed64:
                return value switch
                {
                    long l => (l == 0) ? 0 : 8,
                    ulong u => (u == 0) ? 0 : 8,
                    double d => (d == 0) ? 0 : 8,
                    _ => 0,
                };
            case ValueKind.String:
            {
                if (value is not string str)
                    throw new InvalidOperationException("无效的类型声明：ValueKind.String 只能用于字符串类型的属性上");
                int size = Encoding.UTF8.GetByteCount(str);
                return (size == 0) ? 0 : Varint.GetBufferSize((uint)size) + size;
            }
            case ValueKind.Object:
            {
                int size = GetBufferSize(value);
                return (size == 0) ? 0 : Varint.GetBufferSize((uint)size) + size;
            }
            case ValueKind.List:
            {
                if (value is not IList list || list.Count == 0) return 0;
                var elemType = GetElementType(type)!;
                var elemKind = GetValueKind(elemType);
                int size = 0;
                foreach (object item in list)
                {
                    int itemSize = GetBufferSizeOfValue(elemKind, elemType, item);
                    if (itemSize > 0) size += itemSize;
                }
                return size;
            }
        }
        return 0;
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

    /// <summary>
    /// 计算序列化所需的缓冲区大小
    /// </summary>
    /// <param name="instance">目标对象</param>
    /// <exception cref="InvalidOperationException"/>
    /// <exception cref="NotSupportedException"/>
    public static int GetBufferSize(object instance)
    {
        int size = 0;
        var properties = GetProperties(instance.GetType());

        bool hasProperty = false;
        foreach (var p in properties)
        {
            hasProperty = true;
            object? value = p.GetValue(instance);
            var attr = p.GetCustomAttribute<ProtoFieldAttribute>()!;
            int tagSize = Varint.GetBufferSize((uint)(attr.Id << 3));

            // 一个字节的长度信息，值为 0x00，此 null 值是被刻意保留的，不能省略
            if (attr.Kind == ValueKind.Null)
            {
                size += tagSize + 1;
                continue;
            }

            if (value is null) continue;
            Type type = value.GetType();

            if (attr.Kind == ValueKind.List)
            {
                int listSize = GetBufferSizeOfValue(ValueKind.List, type, value);
                if (listSize > 0) size += ((value as IList)!.Count * tagSize) + listSize;
                continue;
            }
            
            if (attr.Kind == ValueKind.Wrapped)
            {
                var wrapperInfo = p.GetCustomAttribute<WrappedFieldAttribute>() ??
                    throw new InvalidOperationException("缺少修饰特性 WrappedFieldAttribute");

                int itemSize = GetBufferSizeOfValue(wrapperInfo.Kind, type, value);
                if (itemSize == 0) continue;

                int itemTagSize = Varint.GetBufferSize((uint)(wrapperInfo.Id << 3));
                int wrapperSize = itemTagSize + itemSize;

                for (int i = wrapperInfo.Levels.Length - 1; i >= 0; i--)
                {
                    int wrapperTagSize = Varint.GetBufferSize((uint)(wrapperInfo.Levels[i] << 3));
                    wrapperSize = wrapperTagSize + Varint.GetBufferSize((uint)wrapperSize) + wrapperSize;
                }

                size += wrapperSize;
                continue;
            }

            int valueSize = GetBufferSizeOfValue(attr.Kind, type, value);
            if (valueSize > 0) size += tagSize + valueSize;
        }

        if (!hasProperty) // 传入了基础类型
        {
            var type = instance.GetType();
            var kind = GetValueKind(type);
            return kind != ValueKind.Object
                ? GetBufferSizeOfValue(kind, type, instance)
                : throw new NotSupportedException("不受支持的类型");
        }

        return size;
    }

    public static void Serialize<T>(ref BufferWriter writer, T value) where T : notnull
    {
        throw new NotImplementedException();
    }
}
