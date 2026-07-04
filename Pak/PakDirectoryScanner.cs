namespace PakTool.Pak;

internal static class PakDirectoryScanner
{
    public static PakDirectory Scan(string inputDirectory, string outputPak)
    {
        var normalizedOutputPak = Path.GetFullPath(outputPak);
        return ScanDirectory(new DirectoryInfo(Path.GetFullPath(inputDirectory)), "", normalizedOutputPak);
    }

    private static PakDirectory ScanDirectory(DirectoryInfo directory, string parentPath, string outputPak)
    {
        var children = directory.EnumerateFileSystemInfos()
            .Where(entry => ShouldInclude(entry.FullName, outputPak))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Name, StringComparer.Ordinal)
            .SelectMany(entry => ScanChild(entry, parentPath, outputPak))
            .ToList();

        return new PakDirectory(
            parentPath.Length == 0 ? "" : parentPath[(parentPath.LastIndexOf('/') + 1)..],
            parentPath,
            PakConstants.DirectoryFlag,
            children);
    }

    private static IEnumerable<PakNode> ScanChild(FileSystemInfo entry, string parentPath, string outputPak)
    {
        if (entry is DirectoryInfo directory)
        {
            var entryName = directory.Name;
            var entryPath = entryName.ToPakPath(parentPath);
            var selfFile = new FileInfo(Path.Combine(directory.FullName, PakConstants.SelfFileName));

            if (selfFile.Exists) yield return FileNode(entryName, entryPath, selfFile);
            yield return ScanDirectory(directory, entryPath, outputPak);
            yield break;
        }

        if (entry is not FileInfo file || file.Name == PakConstants.SelfFileName) yield break;

        yield return FileNode(file.Name, file.Name.ToPakPath(parentPath), file);
    }

    private static PakFile FileNode(string name, string path, FileInfo source)
    {
        return new PakFile(name, path, 0, 0, source.Length, 0);
    }

    private static bool ShouldInclude(string path, string outputPak)
    {
        return !string.Equals(Path.GetFullPath(path), outputPak, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
