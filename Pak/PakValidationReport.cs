namespace PakTool.Pak;

internal sealed class PakValidationReport
{
    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];

    private bool HasErrors => _errors.Count != 0;

    public void Error(string message) => _errors.Add(message);

    public void Warn(string message) => _warnings.Add(message);

    public void Print(string prefix = "PAK validation")
    {
        Console.WriteLine($"{prefix}: errors={_errors.Count}, warnings={_warnings.Count}");
        foreach (var warning in _warnings) Console.WriteLine($"WARN: {warning}");
        foreach (var error in _errors) Console.WriteLine($"ERROR: {error}");
    }

    public void RequireValid()
    {
        if (HasErrors) throw new InvalidDataException($"PAK validation failed with {_errors.Count} error(s)");
    }
}
