using System.Text;
using PakTool.IO;

namespace PakTool.Pak;

internal static class PakValidation
{
    public static PakValidationReport ValidateArchive(string pakFile)
    {
        using var file = File.Open(pakFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        var archive = PakTreeCodec.ReadArchive(file);
        return ValidateParsedArchive(file, archive);
    }

    private static PakValidationReport ValidateParsedArchive(FileStream file, PakArchive archive)
    {
        var report = new PakValidationReport();
        var physicalSize = file.Length;

        if (archive.HeaderSize < 16) report.Error($"headerSize is smaller than minimal header: {archive.HeaderSize}");
        if (archive.HeaderSize > physicalSize) report.Error($"headerSize points past EOF: headerSize={archive.HeaderSize}, fileSize={physicalSize}");

        var expectedDataSize = physicalSize - archive.HeaderSize;
        if (expectedDataSize >= 0 && archive.DataSizeField != (expectedDataSize & uint.MaxValue))
            report.Warn($"top-level dataSize={archive.DataSizeField} differs from physical data span modulo UInt32={expectedDataSize & uint.MaxValue}");

        var seenPaths = new Dictionary<string, int>(StringComparer.Ordinal);
        var fileRanges = new List<PakFileRange>();

        archive.Root.Walk(
            dir =>
            {
                CheckCommonNode(dir, report);
                if (dir.Path.Length != 0) RegisterPath(dir.Path, seenPaths, report);
                if ((dir.Flags & PakConstants.DirectoryFlag) == 0) report.Error($"directory misses dir flag: {dir.Path}");
                var unknown = dir.Flags & ~PakConstants.KnownFlags;
                if (unknown != 0) report.Warn($"directory has unknown flags: path={dir.Path}, flags={unknown}");
            },
            entry =>
            {
                CheckCommonNode(entry, report);
                RegisterPath(entry.Path, seenPaths, report);
                if ((entry.Flags & PakConstants.DirectoryFlag) != 0) report.Error($"file has dir flag: {entry.Path}");
                var unknown = entry.Flags & ~PakConstants.KnownFlags;
                if (unknown != 0) report.Warn($"file has unknown flags: path={entry.Path}, flags={unknown}");

                if (entry.DataOffset < 0) report.Error($"negative data offset: {entry.Path} offset={entry.DataOffset}");
                if (entry.DataSize < 0) report.Error($"negative data size: {entry.Path} size={entry.DataSize}");

                var usesF64 = (entry.Flags & PakConstants.Float64OffsetFlag) != 0;
                if (!usesF64 && entry.DataOffset > int.MaxValue) report.Error($"offset requires Float64 flag but is encoded as Int32: {entry.Path} offset={entry.DataOffset}");
                if (usesF64 && entry.DataOffset <= int.MaxValue) report.Warn($"offset fits Int32 but uses Float64 flag: {entry.Path} offset={entry.DataOffset}");

                var absoluteStart = archive.HeaderSize + entry.DataOffset;
                var absoluteEnd = absoluteStart + entry.DataSize;
                if (absoluteStart < archive.HeaderSize) report.Error($"file starts before data block: {entry.Path} start={absoluteStart} headerSize={archive.HeaderSize}");
                if (absoluteEnd > physicalSize) report.Error($"file range goes past EOF: {entry.Path} end={absoluteEnd} fileSize={physicalSize}");
                if (absoluteEnd < absoluteStart) report.Error($"file range overflow: {entry.Path} start={absoluteStart} end={absoluteEnd}");
                fileRanges.Add(new PakFileRange(entry.Path, absoluteStart, absoluteEnd));
            });

        foreach (var pair in fileRanges.OrderBy(range => range.Start).Zip(fileRanges.OrderBy(range => range.Start).Skip(1)))
            if (pair.First.End > pair.Second.Start)
                report.Error($"file ranges overlap: {pair.First.Path} [{pair.First.Start}, {pair.First.End}) and {pair.Second.Path} [{pair.Second.Start}, {pair.Second.End})");

        return report;
    }

    public static PakValidationReport VerifyArchiveAgainstDirectory(string pakFile, string sourceDirectory)
    {
        if (!Directory.Exists(sourceDirectory)) throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDirectory}");

        using var file = File.Open(pakFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        var archive = PakTreeCodec.ReadArchive(file);
        var report = ValidateParsedArchive(file, archive);
        var files = archive.Files;

        for (var index = 0; index < files.Count; index++)
        {
            var entry = files[index];
            var source = PakPathResolver.SourceFile(sourceDirectory, entry);
            if (!File.Exists(source))
            {
                report.Error($"missing source file for entry: {entry.Path} expected={source}");
                continue;
            }

            var sourceSize = new FileInfo(source).Length;
            if (sourceSize != entry.DataSize)
            {
                report.Error($"size mismatch for {entry.Path}: pak={entry.DataSize}, source={sourceSize}");
                continue;
            }

            if (!FileCopy.SameBytes(file, archive.HeaderSize + entry.DataOffset, source, entry.DataSize)) report.Error($"byte mismatch for {entry.Path}");
            if ((index + 1) % 1000 == 0) Console.WriteLine($"verified {index + 1}/{files.Count}");
        }

        return report;
    }

    private static void CheckCommonNode(PakNode node, PakValidationReport report)
    {
        var nameBytes = Encoding.UTF8.GetByteCount(node.Name);
        if (nameBytes > 255) report.Error($"entry name is longer than 255 bytes: {node.Path}");
        if (node.Name.Contains('/')) report.Error($"entry name contains '/': {node.Path}");
    }

    private static void RegisterPath(string path, Dictionary<string, int> seenPaths, PakValidationReport report)
    {
        seenPaths.TryGetValue(path, out var count);
        count++;
        seenPaths[path] = count;
        if (count > 1) report.Warn($"duplicate path in tree: {path} occurrence={count}");
    }
}
