using System.Text;
using PakTool.IO;

namespace PakTool.Pak;

internal static class PakTreeCodec
{
    public static PakArchive ReadArchive(FileStream file)
    {
        file.Seek(0, SeekOrigin.Begin);
        var magic = file.ReadAscii(3);
        if (magic != PakConstants.Magic) throw new InvalidDataException($"Not a PAK file: magic='{magic}'");

        var version = file.ReadU1();
        var headerSize = file.ReadU4();
        var dataSize = file.ReadU4();
        if (headerSize < 16) throw new InvalidDataException($"Invalid headerSize={headerSize}");

        var root = ReadNode(file, "");
        if (root is not PakDirectory rootDirectory) throw new InvalidDataException("PAK root entry must be a directory");

        var markerOffset = headerSize - 4;
        if (file.Position > markerOffset) throw new InvalidDataException($"Serialized tree ended after DATA marker: treeEnd={file.Position}, markerOffset={markerOffset}");

        file.Seek(markerOffset, SeekOrigin.Begin);
        var marker = file.ReadAscii(4);
        if (marker != PakConstants.DataMarker) throw new InvalidDataException($"Expected DATA marker at {markerOffset}, got '{marker}'");

        return new PakArchive(version, headerSize, dataSize, Math.Max(0, file.Length - headerSize), rootDirectory);
    }

    public static void WriteArchiveHeader(FileStream file, PakArchive archive)
    {
        file.Seek(0, SeekOrigin.Begin);
        file.WriteAscii(PakConstants.Magic);
        file.WriteByte((byte)archive.Version);
        file.WriteU4(archive.HeaderSize);
        file.WriteU4(archive.DataSizeField);
        WriteNode(file, archive.Root);

        var markerOffset = archive.HeaderSize - 4;
        if (file.Position > markerOffset) throw new InvalidOperationException($"Serialized tree is larger than header: treeEnd={file.Position}, markerOffset={markerOffset}");

        file.WriteZeroes(markerOffset - file.Position);
        file.WriteAscii(PakConstants.DataMarker);
        if (file.Position != archive.HeaderSize) throw new InvalidOperationException($"Header ended at {file.Position}, expected {archive.HeaderSize}");
    }

    public static long SerializedSize(PakNode node)
    {
        var nameSize = Encoding.UTF8.GetByteCount(node.Name);
        var baseSize = 1L + nameSize + 1L;
        return node switch
        {
            PakDirectory directory => baseSize + 4L + directory.Children.Sum(SerializedSize),
            PakFile file => baseSize + EncodedOffsetSize(file) + 4L + 4L,
            _ => throw new InvalidOperationException($"Unsupported node type: {node.GetType()}")
        };
    }

    private static PakNode ReadNode(FileStream file, string parentPath)
    {
        var recordOffset = file.Position;
        var nameLength = file.ReadU1();
        var name = file.ReadUtf8(nameLength);
        var flags = file.ReadU1();
        var path = name.ToPakPath(parentPath);

        if ((flags & PakConstants.DirectoryFlag) != 0)
        {
            var childCount = file.ReadU4();
            if (childCount > int.MaxValue) throw new InvalidDataException($"Directory child count is too large at {recordOffset}, path='{path}': {childCount}");

            var children = new List<PakNode>((int)childCount);
            for (var i = 0; i < childCount; i++) children.Add(ReadNode(file, path));
            return new PakDirectory(name, path, flags, children);
        }

        var dataOffset = (flags & PakConstants.Float64OffsetFlag) != 0 ? ReadF64Offset(file, recordOffset, path) : file.ReadU4();
        return new PakFile(name, path, flags, dataOffset, file.ReadU4(), file.ReadI4());
    }

    private static long ReadF64Offset(FileStream file, long recordOffset, string path)
    {
        var value = file.ReadF64();
        if (!double.IsFinite(value) || value < 0.0 || value % 1.0 != 0.0) throw new InvalidDataException($"Invalid Float64 data offset at {recordOffset}, path='{path}': {value}");
        if (value > long.MaxValue) throw new InvalidDataException($"Float64 data offset is too large at {recordOffset}, path='{path}': {value}");
        return (long)value;
    }

    private static void WriteNode(FileStream file, PakNode node)
    {
        var nameBytes = Encoding.UTF8.GetBytes(node.Name);
        if (nameBytes.Length > 255) throw new InvalidOperationException($"Entry name is longer than 255 bytes: {node.Path}");

        file.WriteByte((byte)nameBytes.Length);
        file.Write(nameBytes);
        file.WriteByte((byte)node.Flags);

        switch (node)
        {
            case PakDirectory directory:
                file.WriteU4(directory.Children.Count);
                foreach (var child in directory.Children) WriteNode(file, child);
                break;

            case PakFile pakFile:
                if ((pakFile.Flags & PakConstants.Float64OffsetFlag) != 0) file.WriteF64(pakFile.DataOffset);
                else file.WriteU4(pakFile.DataOffset);
                file.WriteU4(pakFile.DataSize);
                file.WriteI4(pakFile.Checksum);
                break;
        }
    }

    private static long EncodedOffsetSize(PakFile file) => (file.Flags & PakConstants.Float64OffsetFlag) != 0 ? 8L : 4L;
}