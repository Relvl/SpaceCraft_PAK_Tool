using PakTool.Pak;

namespace PakTool.Command.Impl;

internal sealed class EmptyTreeCommand : IPakCommand
{
    public string Name => "empty-tree";

    public string Description => "Creates an empty file tree matching a PAK archive.";

    public IReadOnlyList<PakArgument> Arguments { get; } =
    [
        new("<pak-file>", description: "PAK archive to read."),
        new("<output-dir>", description: "Directory to create or replace.")
    ];

    public int Execute(CommandArguments args)
    {
        using var reader = new PakReader(args.Positional(0));
        reader.CreateEmptyFileTree(args.Positional(1));
        return 0;
    }
}
