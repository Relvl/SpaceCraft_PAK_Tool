using PakTool.Pak;

namespace PakTool.Command.Impl;

internal sealed class ValidateCommand : IPakCommand
{
    public string Name => "validate";

    public string Description => "Checks archive structure without comparing extracted files.";

    public IReadOnlyList<PakArgument> Arguments { get; } =
    [
        new("<pak-file>", description: "PAK archive to validate.")
    ];

    public int Execute(CommandArguments args)
    {
        var report = PakValidation.ValidateArchive(args.Positional(0));
        report.Print();
        report.RequireValid();
        return 0;
    }
}
