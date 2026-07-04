using System.Buffers;

namespace PakTool.IO;

internal static class Crc32
{
    private const uint Polynomial = 0xEDB88320;
    private const int BufferSize = 1024 * 1024;

    private static readonly uint[] Table = BuildTable();

    public static int Compute(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            FileOptions.SequentialScan
        );

        return Compute(stream);
    }

    public static int Compute(Stream stream)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            var crc = 0xFFFFFFFFu;

            while (true)
            {
                var read = stream.Read(buffer, 0, BufferSize);
                if (read == 0)
                    break;

                crc = Update(crc, buffer.AsSpan(0, read));
            }

            return Finish(crc);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static uint InitialState => 0xFFFFFFFFu;

    public static uint Update(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
            crc = Table[(crc ^ value) & 0xFF] ^ (crc >> 8);

        return crc;
    }

    public static int Finish(uint crc) => unchecked((int)~crc);

    private static uint[] BuildTable()
    {
        var table = new uint[256];

        for (var i = 0u; i < table.Length; i++)
        {
            var crc = i;

            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0
                    ? Polynomial ^ (crc >> 1)
                    : crc >> 1;
            }

            table[i] = crc;
        }

        return table;
    }
}