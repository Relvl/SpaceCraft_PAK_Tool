using System.Buffers;
using PakTool.IO;

namespace PakTool.Pak;

internal static class PakWriter
{
    private const int CopyBufferSize = 1024 * 1024;

    public static void Pack(string inputDirectory, string outputPak)
    {
        if (!Directory.Exists(inputDirectory))
            throw new DirectoryNotFoundException($"Input directory does not exist: {inputDirectory}");

        var inputRoot = Path.GetFullPath(inputDirectory);
        var outputPath = Path.GetFullPath(outputPak);

        Console.WriteLine($"Scanning files: {inputRoot}");
        var root = PakDirectoryScanner.Scan(inputDirectory, outputPak);

        Console.WriteLine("Building archive layout...");
        var archive = PakLayoutBuilder.Build(PakConstants.DefaultVersion, root);
        var files = archive.Files;

        Console.WriteLine($"Writing archive: {outputPath}");
        Console.WriteLine($"Files: {files.Count}");
        Console.WriteLine($"Header size: {archive.HeaderSize}");
        Console.WriteLine($"Data size: {archive.PhysicalDataSize.FormatSize()}");
        Console.WriteLine();

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        using (var output = File.Open(outputPak, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            PakTreeCodec.WriteArchiveHeader(output, archive);
            output.SetLength(checked(archive.HeaderSize + archive.PhysicalDataSize));

            for (var index = 0; index < files.Count; index++)
            {
                var entry = files[index];
                var source = PakPathResolver.SourceFile(inputDirectory, entry);
                if (!File.Exists(source))
                    throw new FileNotFoundException($"Missing source for entry '{entry.Path}'", source);

                Console.WriteLine($"[{index + 1}/{files.Count}] pack {entry.Path} offset={entry.DataOffset} size={entry.DataSize.FormatSize()}");
                output.Seek(archive.HeaderSize + entry.DataOffset, SeekOrigin.Begin);

                entry.Checksum = CopyFileAndComputeCrc32(source, output, entry.DataSize);
            }

            PakTreeCodec.WriteArchiveHeader(output, archive);
        }

        Console.WriteLine();
        Console.WriteLine($"Packed files={files.Count}");
        Console.WriteLine($"Header size={archive.HeaderSize}");
        Console.WriteLine($"Data size={archive.DataSizeField}");
        Console.WriteLine($"Physical data size={archive.PhysicalDataSize.FormatSize()}");
        Console.WriteLine($"Output={outputPath}");
    }

    private static int CopyFileAndComputeCrc32(string sourcePath, FileStream output, long expectedSize)
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            BufferSize = CopyBufferSize,
            Options = FileOptions.SequentialScan
        };

        using var input = new FileStream(sourcePath, options);
        if (input.Length != expectedSize)
            throw new IOException($"Source file size changed while packing: {sourcePath}. Expected {expectedSize}, got {input.Length}.");

        var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        try
        {
            var crc = Crc32.InitialState;
            var remaining = expectedSize;

            while (remaining > 0)
            {
                var chunk = (int)Math.Min(buffer.Length, remaining);
                input.ReadExactly(buffer, 0, chunk);
                output.Write(buffer, 0, chunk);
                crc = Crc32.Update(crc, buffer.AsSpan(0, chunk));
                remaining -= chunk;
            }

            return Crc32.Finish(crc);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}