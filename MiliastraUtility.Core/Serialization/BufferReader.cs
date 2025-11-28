using System.Text;
using System.Buffers.Binary;
using MiliastraUtility.Core.Types;

namespace MiliastraUtility.Core.Serialization;

/// <summary>
/// 提供用于从指定缓冲区读取数据的高性能读取器。
/// </summary>
/// <param name="buffer">只读缓冲区</param>
public ref struct BufferReader(ReadOnlySpan<byte> buffer)
{
    /// <summary>
    /// 获取用于读取数据的只读缓冲区。
    /// </summary>
    public ReadOnlySpan<byte> Span { get; } = buffer;

    /// <summary>
    /// 获取缓冲区的长度。
    /// </summary>
    public readonly int Length => Span.Length;

    /// <summary>
    /// 获取当前读取位置的索引。
    /// </summary>
    public int Position { get; private set; } = 0;

    /// <summary>
    /// 从缓冲区读取一个字节。
    /// </summary>
    /// <returns></returns>
    public byte ReadByte()
    {
        byte value = Span[Position];
        Position += sizeof(byte);
        return value;
    }

    /// <summary>
    /// 从缓冲区读取一串字节序列。
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public ReadOnlySpan<byte> ReadSpan(int length)
    {
        var value = Span.Slice(Position, length);
        Position += length;
        return value;
    }

    /// <summary>
    /// 以小端序从缓冲区读取一个32位有符号整数。
    /// </summary>
    /// <returns></returns>
    public int ReadInt32LE()
    {
        int value = BinaryPrimitives.ReadInt32LittleEndian(Span[Position..]);
        Position += sizeof(int);
        return value;
    }

    /// <summary>
    /// 以大端序从缓冲区读取一个32位有符号整数。
    /// </summary>
    /// <returns></returns>
    public int ReadInt32BE()
    {
        int value = BinaryPrimitives.ReadInt32BigEndian(Span[Position..]);
        Position += sizeof(int);
        return value;
    }

    /// <summary>
    /// 以小端序从缓冲区读取一个32位无符号整数。
    /// </summary>
    /// <returns></returns>
    public uint ReadUInt32LE()
    {
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(Span[Position..]);
        Position += sizeof(uint);
        return value;
    }

    /// <summary>
    /// 以大端序从缓冲区读取一个32位无符号整数。
    /// </summary>
    /// <returns></returns>
    public uint ReadUInt32BE()
    {
        uint value = BinaryPrimitives.ReadUInt32BigEndian(Span[Position..]);
        Position += sizeof(uint);
        return value;
    }

    /// <summary>
    /// 以小端序从缓冲区读取一个64位有符号整数。
    /// </summary>
    /// <returns></returns>
    public long ReadInt64LE()
    {
        long value = BinaryPrimitives.ReadInt64LittleEndian(Span[Position..]);
        Position += sizeof(long);
        return value;
    }

    /// <summary>
    /// 以大端序从缓冲区读取一个64位有符号整数。
    /// </summary>
    /// <returns></returns>
    public long ReadInt64BE()
    {
        long value = BinaryPrimitives.ReadInt64BigEndian(Span[Position..]);
        Position += sizeof(long);
        return value;
    }

    /// <summary>
    /// 以小端序从缓冲区读取一个64位无符号整数。
    /// </summary>
    /// <returns></returns>
    public ulong ReadUInt64LE()
    {
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(Span[Position..]);
        Position += sizeof(ulong);
        return value;
    }

    /// <summary>
    /// 以大端序从缓冲区读取一个64位无符号整数。
    /// </summary>
    /// <returns></returns>
    public ulong ReadUInt64BE()
    {
        ulong value = BinaryPrimitives.ReadUInt64BigEndian(Span[Position..]);
        Position += sizeof(ulong);
        return value;
    }

    /// <summary>
    /// 从缓冲区读取一个单精度浮点数。
    /// </summary>
    /// <returns></returns>
    public float ReadFloat()
    {
        float value = BinaryPrimitives.ReadSingleLittleEndian(Span[Position..]);
        Position += sizeof(float);
        return value;
    }

    /// <summary>
    /// 从缓冲区读取一个双精度浮点数。
    /// </summary>
    /// <returns></returns>
    public double ReadDouble()
    {
        double value = BinaryPrimitives.ReadDoubleLittleEndian(Span[Position..]);
        Position += sizeof(double);
        return value;
    }

    public string ReadString()
    {
        int length = (int)Varint.FromBuffer(this).GetValue();
        string value = Encoding.UTF8.GetString(Span.Slice(Position, length));
        Position += length;
        return value;
    }
}
