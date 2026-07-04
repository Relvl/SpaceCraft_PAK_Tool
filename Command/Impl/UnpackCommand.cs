using PakTool.Pak;

namespace PakTool.Command.Impl;

internal sealed class UnpackCommand : IPakCommand
{
    public string Name => "unpack";

    public string Description => "Extracts files from a PAK archive.";

    public IReadOnlyList<PakArgument> Arguments { get; } =
    [
        new("<pak-file>", description: "PAK archive to unpack."),
        new("--output", true, "<dir>", "Output directory. Default: unpacked.", "-o"),
        new("--ignore-ext", true, "<ext1,ext2>", "Create empty files for matching extensions instead of extracting data."),
        new("--max-file-size", true, "<size>", "Create empty files for entries larger than this size.")
    ];

    public int Execute(CommandArguments args)
    {
        var outputDirectory = args.OptionValueOrDefault("--output", "unpacked");
        var ignoredExtensions = args.OptionValue("--ignore-ext") is { } extensions
            ? CommandValueParsers.ParseExtensions(extensions)
            : new HashSet<string>();
        var maxFileSize = args.OptionValue("--max-file-size") is { } size
            ? CommandValueParsers.ParseSize(size)
            : (long?)null;

        using var reader = new PakReader(args.Positional(0), ignoredExtensions, maxFileSize);
        reader.Unpack(outputDirectory);
        return 0;
    }
}
