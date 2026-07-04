namespace PakTool.Pak;

internal sealed class PakFile(string name, string path, int flags, long dataOffset, long dataSize, int checksum) : PakNode(name, path, flags)
{
    public long DataOffset { get; set; } = dataOffset;
    public long DataSize { get; } = dataSize;
    public int Checksum { get; set; } = checksum;
}
