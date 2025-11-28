namespace MiliastraUtility.Core.Serialization;

public interface IDeserializable<T> where T : IDeserializable<T>, new(), allows ref struct
{
    public void Deserialize(BufferReader reader);
    public static abstract T FromBuffer(BufferReader reader);
}
