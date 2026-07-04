namespace PakTool.Command;

internal sealed class CommandArguments(IReadOnlyList<string> positionals, IReadOnlyDictionary<string, string?> options)
{
    public string Positional(int index)
    {
        return positionals[index];
    }

    public bool HasOption(string name)
    {
        return options.ContainsKey(name);
    }

    public string? OptionValue(string name)
    {
        return options.GetValueOrDefault(name);
    }

    public string OptionValueOrDefault(string name, string defaultValue)
    {
        return OptionValue(name) ?? defaultValue;
    }
}
