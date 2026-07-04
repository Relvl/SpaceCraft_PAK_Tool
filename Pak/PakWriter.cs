namespace PakTool.Pak;

internal static class PakWriter
{
    public static void Pack(string inputDirectory, string outputPak)
    {
        if (!Directory.Exists(inputDirectory)) throw new DirectoryNotFoundException($"Input directory does not exist: {inputDirectory}");

        var root = PakDirectoryScanner.Scan(inputDirectory, outputPak);
        var archive = PakLayoutBuilder.Build(PakConstants.DefaultVersion, root);
        var files = archive.Files;

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPak));
        if (!string.IsNullOrEmpty(outputDirectory)) Directory.CreateDirectory(outputDirectory);

        using (var output = File.Open(outputPak, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            PakTreeCodec.WriteArchiveHeader(output, archive);
            output.SetLength(checked(archive.HeaderSize + archive.PhysicalDataSize));

            for (var index = 0; index < files.Count; index++)
            {
                var entry = files[index];
                var source = PakPathResolver.SourceFile(inputDirectory, entry);
                if (!File.Exists(source)) throw new FileNotFoundException($"Missing source for entry '{entry.Path}'", source);

                Console.WriteLine($"[{index + 1}/{files.Count}] pack {entry.Path} offset={entry.DataOffset} size={entry.DataSize.FormatSize()}");
                output.Seek(archive.HeaderSize + entry.DataOffset, SeekOrigin.Begin);
                using var input = File.OpenRead(source);
                input.CopyTo(output, 1024 * 1024);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Packed files={files.Count}");
        Console.WriteLine($"Header size={archive.HeaderSize}");
        Console.WriteLine($"Data size={archive.DataSizeField}");
        Console.WriteLine($"Output={Path.GetFullPath(outputPak)}");
    }
}