namespace PakTool.Command;

internal sealed class PakArgument(string name, bool optional = false, string? operand = null, string? description = null, params string[] aliases)
{
    public string Name { get; } = name;
    public bool Optional { get; } = optional;
    public string? Operand { get; } = operand;
    public string Description { get; } = description ?? string.Empty;
    public IReadOnlyList<string> Aliases { get; } = aliases;

    public bool IsOption => Name.StartsWith('-');
    public bool RequiresValue => Operand is not null;

    public IEnumerable<string> Names
    {
        get
        {
            yield return Name;
            foreach (var alias in Aliases) yield return alias;
        }
    }

    public string Usage
    {
        get
        {
            var name = IsOption && RequiresValue ? $"{Name} {Operand}" : Name;
            return Optional ? $"[{name}]" : name;
        }
    }
}
