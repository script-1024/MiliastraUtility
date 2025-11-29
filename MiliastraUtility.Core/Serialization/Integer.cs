namespace MiliastraUtility.Core.Serialization;

public readonly struct Integer
{
    private readonly ulong Value;

    public Integer(int value) => Value = (ulong)value;
    public Integer(long value) => Value = (ulong)value;
    public Integer(uint value) => Value = value;
    public Integer(ulong value) => Value = value;

    public static implicit operator Integer(int value) => new(value);
    public static implicit operator int(Integer value) => (int)value.Value;
    public static implicit operator Integer(long value) => new(value);
    public static implicit operator long(Integer value) => (long)value.Value;
    public static implicit operator Integer(uint value) => new(value);
    public static implicit operator uint(Integer value) => (uint)value.Value;
    public static implicit operator Integer(ulong value) => new(value);
    public static implicit operator ulong(Integer value) => value.Value;
}
