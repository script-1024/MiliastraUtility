namespace MiliastraUtility.Core.Serialization;

public interface ISerializable
{
    public int GetBufferSize();
    public void Serialize(ref BufferWriter writer);
}
