using System.Text;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

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
    public readonly Span<byte> Span { get; } = buffer;

    /// <summary>
    /// 获取缓冲区的长度。
    /// </summary>
    public readonly int Length => Span.Length;

    /// <summary>
    /// 获取当前写入位置的索引。
    /// </summary>
    public int Position { get; private set; } = 0;

    /// <summary>
    /// 确保缓冲区还有足够多的空间可供写入。
    /// </summary>
    /// <param name="size">要求的字节数</param>
    /// <exception cref="EndOfStreamException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void EnsureAvailable(int size)
    {
        if (Position + size > Length) throw new EndOfStreamException();
    }

    /// <summary>
    /// 依照指定基准点调整读取位置。
    /// </summary>
    /// <param name="offset">偏移</param>
    /// <param name="origin">基准点</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Seek(int offset, SeekOrigin origin)
    {
        int newPos = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => Position
        };

        if (newPos < 0 || newPos > Length) throw new ArgumentOutOfRangeException(nameof(offset), "新的位置超出缓冲区范围");
        Position = newPos;
    }

    /// <summary>
    /// 向缓冲区写入一个字节。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteByte(byte value)
    {
        EnsureAvailable(sizeof(byte));
        Span[Position++] = value;
    }

    /// <summary>
    /// 向缓冲区写入一串字节序列。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteSpan(Span<byte> source)
    {
        EnsureAvailable(source.Length);
        source.CopyTo(Span[Position..]);
        Position += source.Length;
    }

    /// <summary>
    /// 以小端序向缓冲区写入一个32位有符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteInt32LE(int value)
    {
        EnsureAvailable(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(Span[Position..], value);
        Position += sizeof(int);
    }

    /// <summary>
    /// 以大端序向缓冲区写入一个32位有符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteInt32BE(int value)
    {
        EnsureAvailable(sizeof(int));
        BinaryPrimitives.WriteInt32BigEndian(Span[Position..], value);
        Position += sizeof(int);
    }

    /// <summary>
    /// 以小端序向缓冲区写入一个32位无符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteUInt32LE(uint value)
    {
        EnsureAvailable(sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(Span[Position..], value);
        Position += sizeof(uint);
    }

    /// <summary>
    /// 以大端序向缓冲区写入一个32位无符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteUInt32BE(uint value)
    {
        EnsureAvailable(sizeof(uint));
        BinaryPrimitives.WriteUInt32BigEndian(Span[Position..], value);
        Position += sizeof(uint);
    }

    /// <summary>
    /// 以小端序向缓冲区写入一个64位有符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteInt64LE(long value)
    {
        EnsureAvailable(sizeof(long));
        BinaryPrimitives.WriteInt64LittleEndian(Span[Position..], value);
        Position += sizeof(long);
    }

    /// <summary>
    /// 以大端序向缓冲区写入一个64位有符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteInt64BE(long value)
    {
        EnsureAvailable(sizeof(long));
        BinaryPrimitives.WriteInt64BigEndian(Span[Position..], value);
        Position += sizeof(long);
    }

    /// <summary>
    /// 以小端序向缓冲区写入一个64位无符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteUInt64LE(ulong value)
    {
        EnsureAvailable(sizeof(ulong));
        BinaryPrimitives.WriteUInt64LittleEndian(Span[Position..], value);
        Position += sizeof(ulong);
    }

    /// <summary>
    /// 以大端序向缓冲区写入一个64位无符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteUInt64BE(ulong value)
    {
        EnsureAvailable(sizeof(ulong));
        BinaryPrimitives.WriteUInt64BigEndian(Span[Position..], value);
        Position += sizeof(ulong);
    }

    /// <summary>
    /// 向缓冲区写入一个单精度浮点数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteFloat(float value)
    {
        EnsureAvailable(sizeof(float));
        BinaryPrimitives.WriteSingleLittleEndian(Span[Position..], value);
        Position += sizeof(float);
    }

    /// <summary>
    /// 向缓冲区写入一个双精度浮点数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteDouble(double value)
    {
        EnsureAvailable(sizeof(double));
        BinaryPrimitives.WriteDoubleLittleEndian(Span[Position..], value);
        Position += sizeof(double);
    }

    /// <summary>
    /// 向缓冲区写入一个带有长度前缀的 UTF-8 字符串。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteString(string value)
    {
        Integer length = Encoding.UTF8.GetByteCount(value);
        Varint.FromUInt32(length).Serialize(this);
        EnsureAvailable(length);
        Encoding.UTF8.GetBytes(value, Span[Position..]);
        Position += length;
    }

    /// <summary>
    /// 向缓冲区写入一个 UTF-8 字符串。
    /// </summary>
    /// <remarks>需要提供字符串经过 UTF-8 编码后的长度，不包含终止符。</remarks>
    /// <exception cref="EndOfStreamException"></exception>
    public void WriteString(string value, int length)
    {
        EnsureAvailable(length);
        if (length != Encoding.UTF8.GetBytes(value, Span[Position..]))
            throw new ArgumentException("提供的长度与实际编码长度不符", nameof(length));
        Position += length;
    }
}
