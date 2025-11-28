using System;
using System.Collections.Generic;
using System.Text;

namespace MiliastraUtility.Core.Serialization;

public static class ProtobufHelper
{
    /// <summary>
    /// 消耗一个无效或未知的标签
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="tag"></param>
    public static void ConsumeTag(BufferReader reader, ProtobufTag tag)
    {
        switch (tag.WireType)
        {
            case WireType.VARINT:
                _ = Varint.FromBuffer(reader);
                break;
            case WireType.FIXED64:
                reader.Seek(8, SeekOrigin.Current);
                break;
            case WireType.LENGTH:
                int length = (int)Varint.FromBuffer(reader).GetValue();
                reader.Seek(length, SeekOrigin.Current);
                break;
            case WireType.FIXED32:
                reader.Seek(4, SeekOrigin.Current);
                break;
            default: throw new NotSupportedException();
        }
    }
}
