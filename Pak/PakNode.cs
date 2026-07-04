namespace PakTool.Pak;

internal abstract class PakNode(string name, string path, int flags)
{
    public string Name { get; } = name;
    public string Path { get; } = path;
    public int Flags { get; set; } = flags;
}
