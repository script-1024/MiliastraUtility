namespace MiliastraUtility.Core;

public enum GiFileType { Unknown = 0, Gip = 1, Gil = 2, Gia = 3, Gir = 4 }

public abstract class GiFile
{
    public uint Version { get; protected set; }
    public abstract GiFileType Type { get; }
    public static uint HeadMagicNumber => 0x0326;
    public static uint TailMagicNumber => 0x0679;
}
