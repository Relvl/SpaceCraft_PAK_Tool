namespace PakTool.Command;

internal interface IPakCommand
{
    string Name { get; }

    string Description { get; }

    IReadOnlyList<PakArgument> Arguments { get; }

    int Execute(CommandArguments args);
}
