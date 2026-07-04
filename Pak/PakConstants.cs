namespace PakTool.Pak;

internal static class PakConstants
{
    public const string Magic = "PAK";
    public const string DataMarker = "DATA";
    public const int DefaultVersion = 0;
    public const int DirectoryFlag = 0x01;
    public const int Float64OffsetFlag = 0x02;
    public const int KnownFlags = DirectoryFlag | Float64OffsetFlag;
    public const string SelfFileName = "__self";
}