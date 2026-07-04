namespace PakTool.Command;

internal static class CommandValueParsers
{
    public static IReadOnlySet<string> ParseExtensions(string value)
    {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(extension => extension.ToLowerInvariant())
            .Select(extension => extension.StartsWith('.') ? extension : "." + extension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static long ParseSize(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        var suffixStart = trimmed.TakeWhile(char.IsDigit).Count();
        if (suffixStart == 0 || !long.TryParse(trimmed[..suffixStart], out var number))
            throw new UsageException($"Invalid size '{value}'. Examples: 1048576, 512K, 20M, 2G");

        var multiplier = trimmed[suffixStart..] switch
        {
            "" or "b" => 1L,
            "k" or "kb" => 1024L,
            "m" or "mb" => 1024L * 1024L,
            "g" or "gb" => 1024L * 1024L * 1024L,
            "t" or "tb" => 1024L * 1024L * 1024L * 1024L,
            var suffix => throw new UsageException($"Unsupported size suffix '{suffix}'.")
        };

        return checked(number * multiplier);
    }
}
