using PakTool.Pak;

namespace PakTool.Command.Impl;

internal sealed class PackCommand : IPakCommand
{
    public string Name => "pack";

    public string Description => "Builds a PAK archive from a directory.";

    public IReadOnlyList<PakArgument> Arguments { get; } =
    [
        new("<input-dir>", description: "Directory containing unpacked archive files."),
        new("<output-pak>", description: "PAK archive to create.")
    ];

    public int Execute(CommandArguments args)
    {
        PakWriter.Pack(args.Positional(0), args.Positional(1));
        return 0;
    }
}
