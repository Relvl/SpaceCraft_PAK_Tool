using PakTool.IO;

namespace PakTool.Pak;

internal sealed class PakReader(string path, IReadOnlySet<string>? ignoredExtensions = null, long? maxExtractFileSizeBytes = null) : IDisposable
{
    private readonly FileStream _file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    private readonly IReadOnlySet<string> _ignoredExtensions = ignoredExtensions ?? new HashSet<string>();

    private PakArchive Archive => field ??= PakTreeCodec.ReadArchive(_file);

    public void Dispose()
    {
        _file.Dispose();
    }

    public void List(bool onlyLargeFiles = false)
    {
        var files = Archive.Files;
        const long minLargeFileSize = 1024L * 1024L;
        var extensions = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var dirCount = 0;
        var printedFiles = 0;

        Archive.Root.Walk(
            dir =>
            {
                if (dir.Path.Length == 0) return;
                dirCount++;
                if (!onlyLargeFiles) Console.WriteLine($"[dir]  {dir.Path}");
            },
            entry =>
            {
                extensions[entry.Extension()] = extensions.GetValueOrDefault(entry.Extension()) + 1;
                if (onlyLargeFiles && entry.DataSize <= minLargeFileSize) return;

                printedFiles++;
                Console.WriteLine($"[file] {entry.Path} flags={entry.Flags} offset={Archive.HeaderSize + entry.DataOffset} size={entry.DataSize.FormatSize()} checksum={entry.Checksum.ToUInt32Hex()}");
            });

        Console.WriteLine();
        Console.WriteLine($"Version={Archive.Version}");
        Console.WriteLine($"Header size={Archive.HeaderSize}");
        Console.WriteLine($"Data size={Archive.DataSizeField}");
        Console.WriteLine($"File size={_file.Length}");
        Console.WriteLine($"Dirs={dirCount}");
        Console.WriteLine($"Files={files.Count}");
        if (onlyLargeFiles) Console.WriteLine($"Printed files larger than {minLargeFileSize.FormatSize()}={printedFiles}");

        Console.WriteLine();
        Console.WriteLine("Extensions:");
        foreach (var (extension, count) in extensions) Console.WriteLine($"{extension}: {count}");
    }

    public void Unpack(string outputDirectory)
    {
        if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, true);
        Directory.CreateDirectory(outputDirectory);

        var directories = new List<PakDirectory>();
        var files = new List<PakFile>();
        Archive.Root.Walk(
            dir =>
            {
                if (dir.Path.Length != 0) directories.Add(dir);
            },
            files.Add);

        foreach (var directory in directories) Directory.CreateDirectory(PakPathResolver.OutputPath(outputDirectory, directory.Path));

        var extracted = 0;
        var skipped = 0;
        for (var index = 0; index < files.Count; index++)
        {
            var entry = files[index];
            var target = PakPathResolver.OutputFile(outputDirectory, entry);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            var skipReason = SkipExtractReason(entry);
            if (skipReason is null)
            {
                Console.WriteLine($"[{index + 1}/{files.Count}] unpack {entry.Path} size={entry.DataSize.FormatSize()}");
                _file.CopyRangeTo(target, Archive.HeaderSize + entry.DataOffset, entry.DataSize);
                extracted++;
            }
            else
            {
                Console.WriteLine($"[{index + 1}/{files.Count}] skip {entry.Path} size={entry.DataSize.FormatSize()} reason={skipReason}");
                if (!File.Exists(target)) File.Create(target).Dispose();
                skipped++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Created dirs={directories.Count}");
        Console.WriteLine($"Extracted files={extracted}");
        Console.WriteLine($"Created empty files={skipped}");
        Console.WriteLine($"Output={Path.GetFullPath(outputDirectory)}");
    }

    public void CreateEmptyFileTree(string outputDirectory)
    {
        if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, true);
        Directory.CreateDirectory(outputDirectory);

        var dirsCreated = 0;
        var filesCreated = 0;
        Archive.Root.Walk(
            dir =>
            {
                if (dir.Path.Length == 0) return;
                Directory.CreateDirectory(PakPathResolver.OutputPath(outputDirectory, dir.Path));
                dirsCreated++;
            },
            entry =>
            {
                var target = PakPathResolver.OutputFile(outputDirectory, entry);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (!File.Exists(target)) File.Create(target).Dispose();
                filesCreated++;
            });

        Console.WriteLine($"Created dirs={dirsCreated} files={filesCreated} in {Path.GetFullPath(outputDirectory)}");
    }

    private string? SkipExtractReason(PakFile entry)
    {
        var ignoredExtension = _ignoredExtensions.FirstOrDefault(ext => entry.Path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        if (ignoredExtension is not null) return $"extension {ignoredExtension}";

        if (maxExtractFileSizeBytes is { } maxSize && entry.DataSize > maxSize) return $"size {entry.DataSize.FormatSize()} > {maxSize.FormatSize()}";
        return null;
    }
}