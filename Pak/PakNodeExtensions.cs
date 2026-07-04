namespace PakTool.Pak;

internal static class PakNodeExtensions
{
    public static void Walk(this PakDirectory directory, Action<PakDirectory> onDirectory, Action<PakFile> onFile)
    {
        onDirectory(directory);
        foreach (var child in directory.Children)
            switch (child)
            {
                case PakDirectory childDirectory:
                    childDirectory.Walk(onDirectory, onFile);
                    break;
                case PakFile file:
                    onFile(file);
                    break;
            }
    }

    public static void CollectFiles(this PakDirectory directory, List<PakFile> output)
    {
        foreach (var child in directory.Children)
            switch (child)
            {
                case PakDirectory childDirectory:
                    childDirectory.CollectFiles(output);
                    break;
                case PakFile file:
                    output.Add(file);
                    break;
            }
    }

    public static string ToPakPath(this string name, string parentPath) => parentPath.Length == 0 ? name : name.Length == 0 ? parentPath : $"{parentPath}/{name}";
}