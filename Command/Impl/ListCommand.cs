using PakTool.Pak;

namespace PakTool.Command.Impl;

internal sealed class ListCommand : IPakCommand
{
    public string Name => "list";

    public string Description => "Prints archive contents and extension statistics.";

    public IReadOnlyList<PakArgument> Arguments { get; } =
    [
        new("<pak-file>", description: "PAK archive to inspect."),
        new("--large", true, description: "Print only files larger than 1 MiB.")
    ];

    public int Execute(CommandArguments args)
    {
        using var reader = new PakReader(args.Positional(0));
        reader.List(args.HasOption("--large"));
        return 0;
    }
}
