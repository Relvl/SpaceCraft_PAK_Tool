namespace PakTool.Pak;

internal sealed class PakDirectory(string name, string path, int flags, IReadOnlyList<PakNode> children) : PakNode(name, path, flags)
{
    public IReadOnlyList<PakNode> Children { get; } = children;
}