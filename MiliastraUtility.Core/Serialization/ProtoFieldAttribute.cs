namespace MiliastraUtility.Core.Serialization;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ProtoFieldAttribute : Attribute
{

    public required int Id
    {
        get => field;
        set
        {
            if (value < 1 || value > 65535)
                throw new ArgumentOutOfRangeException(nameof(value), "无效的字段编号");
            field = value;
        }
    }

    public required ValueKind Kind { get; set; }

    public int WrapperId { get; set; } = 0;

    public bool Repeated { get; set; } = false;
}

public enum ValueKind
{
    Varint = 0,
    Fixed32 = 1,
    Fixed64 = 2,
    String = 3,
    Object = 4,
    List = 5,
    Null = 6
}
