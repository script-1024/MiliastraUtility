using System.Collections;
using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;

namespace MiliastraUtility.Core.Serialization;

public static class ProtoSerializer
{
    private static FrozenDictionary<int, PropertyInfo> GetProtoFields(Type type)
    {
        var result = new Dictionary<int, PropertyInfo>();
        var properties = from p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         where p.GetCustomAttribute<ProtoFieldAttribute>() != null
                         select p;

        foreach (var p in properties)
        {
            var attribute = p.GetCustomAttribute<ProtoFieldAttribute>()!;
            if (attribute.WrapperId != 0) result.Add(attribute.WrapperId, p);
            else result.Add(attribute.Id, p);
        }

        return result.ToFrozenDictionary();
    }

    private static Type? GetElementType(Type type)
        =>  type.IsArray ? type.GetElementType()! : 
            type.IsGenericType ? type.GetGenericArguments()[0] : null;

    private static bool IsEnumerableType(Type type)
        => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

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

    public static T? Deserialize<T>(ref BufferReader reader) where T : notnull, new()
        => (T?) Deserialize(typeof(T), ref reader);

    private static object? Deserialize(Type type, ref BufferReader reader)
    {
        var fields = GetProtoFields(type);
        object obj = Activator.CreateInstance(type)!;
        while (reader.Available())
        {
            ProtoTag tag = Varint.Deserialize(ref reader);
            if (!fields.TryGetValue(tag.Id, out PropertyInfo? prop) || !prop.CanWrite)
            {
                tag.Consume(ref reader);
                continue;
            }

            object? value = null;
            var attr = prop.GetCustomAttribute<ProtoFieldAttribute>()!;
            switch (attr.Kind)
            {
                case ValueKind.Varint:
                {
                    if (tag.Type != WireType.VARINT) throw new InvalidDataException();
                    value = Varint.Deserialize(ref reader).GetValue(prop.PropertyType);
                    prop.SetValue(obj, value);
                    break;
                }
                case ValueKind.Fixed32:
                {
                    if (tag.Type != WireType.FIXED32) throw new InvalidDataException();
                    if (prop.PropertyType == typeof(int)) value = reader.ReadInt32LE();
                    if (prop.PropertyType == typeof(uint)) value = reader.ReadUInt32LE();
                    if (prop.PropertyType == typeof(float)) value = reader.ReadFloat();
                    prop.SetValue(obj, value);
                    break;
                }
                case ValueKind.Fixed64:
                {
                    if (tag.Type != WireType.FIXED64) throw new InvalidDataException();
                    if (prop.PropertyType == typeof(long)) value = reader.ReadInt64LE();
                    if (prop.PropertyType == typeof(ulong)) value = reader.ReadUInt64LE();
                    if (prop.PropertyType == typeof(double)) value = reader.ReadDouble();
                    prop.SetValue(obj, value);
                    break;
                }
                case ValueKind.String:
                {
                    if (tag.Type != WireType.LENGTH) throw new InvalidDataException();
                    value = reader.ReadString();
                    prop.SetValue(obj, value);
                    break;
                }
                case ValueKind.Object:
                {
                    if (tag.Type != WireType.LENGTH) throw new InvalidDataException();
                    value = Deserialize(prop.PropertyType, ref reader);
                    prop.SetValue(obj, value);
                    break;
                }
                case ValueKind.List:
                {
                    if (tag.Type != WireType.LENGTH) throw new InvalidDataException();
                    if (!IsEnumerableType(prop.PropertyType)) throw new NotSupportedException();
                    Type elementType = GetElementType(prop.PropertyType)!;
                    AddToList(prop, obj, Deserialize(elementType, ref reader));
                    break;
                }
                case ValueKind.Null:
                {
                    if (tag.Type != WireType.LENGTH) throw new InvalidDataException();
                    tag.Consume(ref reader);
                    break;
                }
            }
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
