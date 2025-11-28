using MiliastraUtility.Core.Serialization;

namespace MiliastraUtility.Core.Types;

public class Asset : ISerializable, IDeserializable<Asset>
{
    public int GetBufferSize()
    {
        throw new NotImplementedException();
    }

    public void Serialize(BufferWriter writer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(BufferReader reader)
    {
        throw new NotImplementedException();
    }
    
    public static Asset FromBuffer(BufferReader reader)
    {
        throw new NotImplementedException();
    }
}
