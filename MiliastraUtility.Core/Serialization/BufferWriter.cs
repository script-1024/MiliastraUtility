using System.Text;
using System.Buffers.Binary;
using MiliastraUtility.Core.Types;

namespace MiliastraUtility.Core.Serialization;

/// <summary>
/// 提供用于将数据写入指定缓冲区的高性能写入器。
/// </summary>
/// <param name="buffer">缓冲区</param>
public ref struct BufferWriter(Span<byte> buffer)
{
    /// <summary>
    /// 获取用于写入数据的缓冲区。
    /// </summary>
    public Span<byte> Span { get; } = buffer;

    /// <summary>
    /// 获取缓冲区的长度。
    /// </summary>
    public readonly int Length => Span.Length;

    /// <summary>
    /// 获取当前写入位置的索引。
    /// </summary>
    public int Position { get; private set; } = 0;

    /// <summary>
    /// 向缓冲区写入一个字节。
    /// </summary>
    /// <param name="value"></param>
    public void WriteByte(byte value)
    {
        Span[Position] = value;
        Position += sizeof(byte);
    }

    /// <summary>
    /// 向缓冲区写入一串字节序列。
    /// </summary>
    /// <param name="source"></param>
    public void WriteSpan(Span<byte> source)
    {
        source.CopyTo(Span[Position..]);
        Position += source.Length;
    }

    /// <summary>
    /// 以小端序向缓冲区写入一个32位有符号整数。
    /// </summary>
    /// <param name="value"></param>
    public void WriteInt32LE(int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(Span[Position..], value);
        Position += sizeof(int);
    }

    /// <summary>
    /// 以大端序向缓冲区写入一个32位有符号整数。
    /// </summary>
    /// <param name="value"></param>
    public void WriteInt32BE(int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(Span[Position..], value);
        Position += sizeof(int);
    }

    /// <summary>
    /// 以小端序向缓冲区写入一个32位无符号整数。
    /// </summary>
    /// <param name="value"></param>
    public void WriteUInt32LE(uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(Span[Position..], value);
        Position += sizeof(uint);
    }

    /// <summary>
    /// 以大端序向缓冲区写入一个32位无符号整数。
    /// </summary>
    /// <param name="value"></param>
    public void WriteUInt32BE(uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(Span[Position..], value);
        Position += sizeof(uint);
    }

    /// <summary>
    /// 以小端序向缓冲区写入一个64位有符号整数。
    /// </summary>
    /// <param name="value"></param>
    public void WriteInt64LE(long value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(Span[Position..], value);
        Position += sizeof(long);
    }

    /// <summary>
    /// 以大端序向缓冲区写入一个64位有符号整数。
    /// </summary>
    /// <param name="value"></param>
    public void WriteInt64BE(long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(Span[Position..], value);
        Position += sizeof(long);
    }

    /// <summary>
    /// 以小端序向缓冲区写入一个64位无符号整数。
    /// </summary>
    /// <param name="value"></param>
    public void WriteUInt64LE(ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(Span[Position..], value);
        Position += sizeof(ulong);
    }

    /// <summary>
    /// 以大端序向缓冲区写入一个64位无符号整数。
    /// </summary>
    /// <param name="value"></param>
    public void WriteUInt64BE(ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(Span[Position..], value);
        Position += sizeof(ulong);
    }

    /// <summary>
    /// 向缓冲区写入一个单精度浮点数。
    /// </summary>
    /// <param name="value"></param>
    public void WriteFloat(float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(Span[Position..], value);
        Position += sizeof(float);
    }

    /// <summary>
    /// 向缓冲区写入一个双精度浮点数。
    /// </summary>
    /// <param name="value"></param>
    public void WriteDouble(double value)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(Span[Position..], value);
        Position += sizeof(double);
    }

    /// <summary>
    /// 向缓冲区写入一个 UTF-8 编码的字符串和其长度。
    /// </summary>
    /// <remarks>此字符串不以零字符结尾，而是显式地往缓冲区写入长度前缀</remarks>
    /// <param name="value"></param>
    public void WriteString(string value)
    {
        Varint.FromUInt32((uint)Encoding.UTF8.GetByteCount(value)).Serialize(this);
        int length = Encoding.UTF8.GetBytes(value, Span[Position..]);
        Position += length;
    }
}
