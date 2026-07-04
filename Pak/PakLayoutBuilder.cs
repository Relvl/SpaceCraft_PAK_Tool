namespace PakTool.Pak;

internal static class PakLayoutBuilder
{
    public static PakArchive Build(int version, PakDirectory root)
    {
        var result = AssignOffsets(root, 0);
        var rootWithOffsets = (PakDirectory)result.Node;
        var physicalDataSize = rootWithOffsets.FilesEndOffset();
        var headerSize = 12L + PakTreeCodec.SerializedSize(rootWithOffsets) + 4L;
        return new PakArchive(version, headerSize, physicalDataSize & uint.MaxValue, physicalDataSize, rootWithOffsets);
    }

    private static OffsetResult AssignOffsets(PakNode node, long nextOffset)
    {
        switch (node)
        {
            case PakDirectory directory:
                var offset = nextOffset;
                var children = new List<PakNode>(directory.Children.Count);
                foreach (var child in directory.Children)
                {
                    var result = AssignOffsets(child, offset);
                    offset = result.NextOffset;
                    children.Add(result.Node);
                }

                return new OffsetResult(new PakDirectory(directory.Name, directory.Path, directory.Flags, children), offset);

            case PakFile file:
                var flags = WithOffsetEncoding(file.Flags, nextOffset);
                return new OffsetResult(new PakFile(file.Name, file.Path, flags, nextOffset, file.DataSize, file.Checksum), checked(nextOffset + file.DataSize));

            default:
                throw new InvalidOperationException($"Unsupported node type: {node.GetType()}");
        }
    }

    private static int WithOffsetEncoding(int flags, long offset) => offset > int.MaxValue ? flags | PakConstants.Float64OffsetFlag : flags & ~PakConstants.Float64OffsetFlag;

    private static long FilesEndOffset(this PakDirectory directory) => directory.Files().Select(file => file.DataOffset + file.DataSize).DefaultIfEmpty(0).Max();

    private static IEnumerable<PakFile> Files(this PakDirectory directory)
    {
        var files = new List<PakFile>();
        directory.CollectFiles(files);
        return files;
    }

    private sealed record OffsetResult(PakNode Node, long NextOffset);
}