namespace PakTool.IO;

internal static class FileCopy
{
    public static void CopyRangeTo(this FileStream source, string targetPath, long sourceOffset, long byteCount)
    {
        source.Seek(sourceOffset, SeekOrigin.Begin);
        using var output = File.Create(targetPath);
        var buffer = new byte[1024 * 1024];
        var remaining = byteCount;
        while (remaining > 0)
        {
            var chunk = (int)Math.Min(buffer.Length, remaining);
            source.ReadExactly(buffer, 0, chunk);
            output.Write(buffer, 0, chunk);
            remaining -= chunk;
        }
    }

    public static bool SameBytes(FileStream pak, long pakOffset, string sourcePath, long size)
    {
        var pakBuffer = new byte[1024 * 1024];
        var sourceBuffer = new byte[1024 * 1024];
        var remaining = size;
        pak.Seek(pakOffset, SeekOrigin.Begin);

        using var source = File.OpenRead(sourcePath);
        while (remaining > 0)
        {
            var chunk = (int)Math.Min(pakBuffer.Length, remaining);
            pak.ReadExactly(pakBuffer, 0, chunk);
            source.ReadExactly(sourceBuffer, 0, chunk);

            if (!pakBuffer.AsSpan(0, chunk).SequenceEqual(sourceBuffer.AsSpan(0, chunk))) return false;
            remaining -= chunk;
        }

        return source.ReadByte() < 0;
    }
}