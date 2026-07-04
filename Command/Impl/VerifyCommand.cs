using PakTool.Pak;

namespace PakTool.Command.Impl;

internal sealed class VerifyCommand : IPakCommand
{
    public string Name => "verify";

    public string Description => "Compares archive contents with a source directory.";

    public IReadOnlyList<PakArgument> Arguments { get; } =
    [
        new("<pak-file>", description: "PAK archive to verify."),
        new("<input-dir>", description: "Directory containing source files.")
    ];

    public int Execute(CommandArguments args)
    {
        var report = PakValidation.VerifyArchiveAgainstDirectory(args.Positional(0), args.Positional(1));
        report.Print("PAK/source verification");
        report.RequireValid();
        return 0;
    }
}
