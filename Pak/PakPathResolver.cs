namespace PakTool.Pak;

internal static class PakPathResolver
{
    public static string OutputPath(string outputDirectory, string pakPath) => ResolvePakPath(outputDirectory, pakPath);

    public static string OutputFile(string outputDirectory, PakFile entry)
    {
        var direct = ResolvePakPath(outputDirectory, entry.Path);
        return Directory.Exists(direct) ? Path.Combine(direct, PakConstants.SelfFileName) : direct;
    }

    public static string SourceFile(string inputDirectory, PakFile entry)
    {
        var direct = ResolvePakPath(inputDirectory, entry.Path);
        return File.Exists(direct) ? direct : Path.Combine(direct, PakConstants.SelfFileName);
    }

    private static string ResolvePakPath(string root, string pakPath)
    {
        var rootFullPath = Path.GetFullPath(root);
        var parts = pakPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part is "." or "..")
                throw new InvalidDataException($"Unsafe PAK path: {pakPath}");

            if (part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException($"PAK path contains invalid characters: {pakPath}");
        }

        var resolved = parts.Length == 0 ? rootFullPath : Path.GetFullPath(Path.Combine(new[] { rootFullPath }.Concat(parts).ToArray()));
        if (!IsInsideRoot(rootFullPath, resolved))
            throw new InvalidDataException($"PAK path escapes output root: {pakPath}");

        return resolved;
    }

    private static bool IsInsideRoot(string root, string path)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var normalizedRoot = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
        return path.Equals(Path.TrimEndingDirectorySeparator(root), comparison) || path.StartsWith(normalizedRoot, comparison);
    }
}