namespace MiliastraUtility.Core.Serialization;

public interface IDeserializable<T> where T : IDeserializable<T>, new(), allows ref struct
{
    public static abstract T Deserialize(ref BufferReader reader);
    public static abstract T Deserialize(ref BufferReader reader, T self);
}
