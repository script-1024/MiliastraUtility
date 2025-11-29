using System.Text;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

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
    public readonly ReadOnlySpan<byte> Span { get; } = buffer;

    /// <summary>
    /// 获取缓冲区的长度。
    /// </summary>
    public readonly int Length => Span.Length;

    /// <summary>
    /// 获取当前读取位置的索引。
    /// </summary>
    public int Position { get; private set; } = 0;

    /// <summary>
    /// 确保缓冲区还有足够多的数据可供读取。
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
    /// 从缓冲区读取一个字节。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public byte ReadByte()
    {
        EnsureAvailable(sizeof(byte));
        return Span[Position++];
    }

    /// <summary>
    /// 从缓冲区读取一串字节序列。
    /// </summary>
    /// <param name="length">长度</param>
    /// <exception cref="EndOfStreamException"></exception>
    public ReadOnlySpan<byte> ReadSpan(int length)
    {
        EnsureAvailable(length);
        var value = Span.Slice(Position, length);
        Position += length;
        return value;
    }

    /// <summary>
    /// 以小端序从缓冲区读取一个32位有符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public int ReadInt32LE()
    {
        EnsureAvailable(sizeof(int));
        int value = BinaryPrimitives.ReadInt32LittleEndian(Span[Position..]);
        Position += sizeof(int);
        return value;
    }

    /// <summary>
    /// 以大端序从缓冲区读取一个32位有符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public int ReadInt32BE()
    {
        EnsureAvailable(sizeof(int));
        int value = BinaryPrimitives.ReadInt32BigEndian(Span[Position..]);
        Position += sizeof(int);
        return value;
    }

    /// <summary>
    /// 以小端序从缓冲区读取一个32位无符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public uint ReadUInt32LE()
    {
        EnsureAvailable(sizeof(uint));
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(Span[Position..]);
        Position += sizeof(uint);
        return value;
    }

    /// <summary>
    /// 以大端序从缓冲区读取一个32位无符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public uint ReadUInt32BE()
    {
        EnsureAvailable(sizeof(uint));
        uint value = BinaryPrimitives.ReadUInt32BigEndian(Span[Position..]);
        Position += sizeof(uint);
        return value;
    }

    /// <summary>
    /// 以小端序从缓冲区读取一个64位有符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public long ReadInt64LE()
    {
        EnsureAvailable(sizeof(long));
        long value = BinaryPrimitives.ReadInt64LittleEndian(Span[Position..]);
        Position += sizeof(long);
        return value;
    }

    /// <summary>
    /// 以大端序从缓冲区读取一个64位有符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public long ReadInt64BE()
    {
        EnsureAvailable(sizeof(long));
        long value = BinaryPrimitives.ReadInt64BigEndian(Span[Position..]);
        Position += sizeof(long);
        return value;
    }

    /// <summary>
    /// 以小端序从缓冲区读取一个64位无符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public ulong ReadUInt64LE()
    {
        EnsureAvailable(sizeof(ulong));
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(Span[Position..]);
        Position += sizeof(ulong);
        return value;
    }

    /// <summary>
    /// 以大端序从缓冲区读取一个64位无符号整数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public ulong ReadUInt64BE()
    {
        EnsureAvailable(sizeof(ulong));
        ulong value = BinaryPrimitives.ReadUInt64BigEndian(Span[Position..]);
        Position += sizeof(ulong);
        return value;
    }

    /// <summary>
    /// 从缓冲区读取一个单精度浮点数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public float ReadFloat()
    {
        EnsureAvailable(sizeof(float));
        float value = BinaryPrimitives.ReadSingleLittleEndian(Span[Position..]);
        Position += sizeof(float);
        return value;
    }

    /// <summary>
    /// 从缓冲区读取一个双精度浮点数。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public double ReadDouble()
    {
        EnsureAvailable(sizeof(double));
        double value = BinaryPrimitives.ReadDoubleLittleEndian(Span[Position..]);
        Position += sizeof(double);
        return value;
    }

    /// <summary>
    /// 从缓冲区读取一个带有长度前缀的 UTF-8 字符串。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public string ReadString()
    {
        int length = Varint.FromBuffer(this).GetValue();
        if (length == 0) return string.Empty;
        EnsureAvailable(length);
        string value = Encoding.UTF8.GetString(Span.Slice(Position, length));
        Position += length;
        return value;
    }

    /// <summary>
    /// 从缓冲区读取一个指定长度的 UTF-8 字符串。
    /// </summary>
    /// <exception cref="EndOfStreamException"></exception>
    public string ReadString(int length)
    {
        if (length == 0) return string.Empty;
        EnsureAvailable(length);
        string value = Encoding.UTF8.GetString(Span.Slice(Position, length));
        Position += length;
        return value;
    }
}
