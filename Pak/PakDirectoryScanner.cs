using PakTool.IO;

namespace PakTool.Pak;

internal static class PakDirectoryScanner
{
    public static PakDirectory Scan(string inputDirectory, string outputPak)
    {
        var normalizedOutputPak = Path.GetFullPath(outputPak);
        return ScanDirectory(Path.GetFullPath(inputDirectory), "", normalizedOutputPak);
    }

    private static PakDirectory ScanDirectory(string directory, string parentPath, string outputPak)
    {
        var children = Directory.EnumerateFileSystemEntries(directory)
            .Where(path => ShouldInclude(path, outputPak))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .SelectMany(child => ScanChild(child, parentPath, outputPak))
            .ToList();

        return new PakDirectory(
            parentPath.Length == 0 ? "" : parentPath[(parentPath.LastIndexOf('/') + 1)..],
            parentPath,
            PakConstants.DirectoryFlag,
            children);
    }

    private static IEnumerable<PakNode> ScanChild(string child, string parentPath, string outputPak)
    {
        if (Directory.Exists(child))
        {
            var entryName = Path.GetFileName(child);
            var entryPath = entryName.ToPakPath(parentPath);
            var selfFile = Path.Combine(child, PakConstants.SelfFileName);

            if (File.Exists(selfFile)) yield return FileNode(entryName, entryPath, selfFile);
            yield return ScanDirectory(child, entryPath, outputPak);
            yield break;
        }

        if (!File.Exists(child) || Path.GetFileName(child) == PakConstants.SelfFileName) yield break;

        var name = Path.GetFileName(child);
        yield return FileNode(name, name.ToPakPath(parentPath), child);
    }

    private static PakFile FileNode(string name, string path, string source)
    {
        return new PakFile(name, path, 0, 0, new FileInfo(source).Length, Crc32.Compute(source));
    }

    private static bool ShouldInclude(string path, string outputPak)
    {
        return !string.Equals(Path.GetFullPath(path), outputPak, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}