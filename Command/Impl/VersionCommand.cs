using System.Reflection;

namespace PakTool.Command.Impl;

internal sealed class VersionCommand : IPakCommand
{
    public string Name => "version";

    public string Description => "Prints tool version.";

    public IReadOnlyList<PakArgument> Arguments { get; } = [];

    public int Execute(CommandArguments args)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString()
                      ?? "unknown";
        Console.WriteLine(version);
        return 0;
    }
}
