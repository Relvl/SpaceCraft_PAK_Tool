namespace PakTool.Command;

internal sealed class CommandLineParser(IPakCommand command)
{
    private readonly IReadOnlyList<PakArgument> _positionals = command.Arguments.Where(argument => !argument.IsOption).ToList();
    private readonly Dictionary<string, PakArgument> _options = BuildOptionMap(command);

    public CommandArguments Parse(string[] args)
    {
        var positionals = new List<string>();
        var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (arg.StartsWith('-'))
            {
                index = ParseOption(args, index, options);
                continue;
            }

            positionals.Add(arg);
        }

        ValidatePositionals(positionals);
        return new CommandArguments(positionals, NormalizeOptionKeys(options));
    }

    private int ParseOption(string[] args, int index, Dictionary<string, string?> output)
    {
        var name = args[index];
        if (!_options.TryGetValue(name, out var definition))
            throw new UsageException($"Unknown option for '{command.Name}': {name}");

        if (output.ContainsKey(definition.Name))
            throw new UsageException($"Duplicate option for '{command.Name}': {name}");

        if (!definition.RequiresValue)
        {
            output[definition.Name] = null;
            return index;
        }

        if (index + 1 >= args.Length)
            throw new UsageException($"Option {name} requires a value.");

        output[definition.Name] = args[index + 1];
        return index + 1;
    }

    private void ValidatePositionals(IReadOnlyList<string> positionals)
    {
        var required = _positionals.Count(argument => !argument.Optional);
        if (positionals.Count < required)
            throw new UsageException($"Command '{command.Name}' expects {FormatPositionals()}, got {positionals.Count} positional argument(s).");

        if (positionals.Count > _positionals.Count)
            throw new UsageException($"Command '{command.Name}' expects {FormatPositionals()}, got extra argument: {positionals[_positionals.Count]}");
    }

    private string FormatPositionals()
    {
        return string.Join(" ", _positionals.Select(argument => argument.Usage));
    }

    private static Dictionary<string, PakArgument> BuildOptionMap(IPakCommand command)
    {
        var result = new Dictionary<string, PakArgument>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in command.Arguments.Where(argument => argument.IsOption))
        {
            foreach (var name in option.Names)
            {
                if (!result.TryAdd(name, option))
                    throw new InvalidOperationException($"Duplicate option '{name}' in command '{command.Name}'.");
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string?> NormalizeOptionKeys(Dictionary<string, string?> options)
    {
        return options;
    }
}
