namespace PakTool.Pak;

internal sealed class PakArchive(int version, long headerSize, long dataSizeField, long physicalDataSize, PakDirectory root)
{
    public int Version { get; } = version;
    public long HeaderSize { get; } = headerSize;
    public long DataSizeField { get; } = dataSizeField;
    public long PhysicalDataSize { get; } = physicalDataSize;
    public PakDirectory Root { get; } = root;

    public IReadOnlyList<PakFile> Files
    {
        get
        {
            var files = new List<PakFile>();
            Root.CollectFiles(files);
            return files;
        }
    }
}
