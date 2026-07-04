using System.Globalization;

namespace PakTool.Pak;

internal static class PakFormatting
{
    public static string FormatSize(this long value) => string.Format(CultureInfo.InvariantCulture, "{0,8:0.00} MiB", value / 1024.0 / 1024.0);

    public static string ToUInt32Hex(this int value) => $"0x{unchecked((uint)value):x8}";

    public static string Extension(this PakFile file)
    {
        var fileName = file.Path[(file.Path.LastIndexOf('/') + 1)..];
        var dot = fileName.LastIndexOf('.');
        return dot < 0 || dot == fileName.Length - 1 ? "<no extension>" : fileName[(dot + 1)..].ToLowerInvariant();
    }
}