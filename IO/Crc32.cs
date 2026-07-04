namespace PakTool.IO;

internal sealed class Crc32
{
    private static readonly uint[] Table = BuildTable();
    private uint _value = 0xFFFF_FFFFu;

    public int Value => unchecked((int)(_value ^ 0xFFFF_FFFFu));

    public void Update(ReadOnlySpan<byte> bytes)
    {
        foreach (var b in bytes) _value = Table[(int)((_value ^ b) & 0xFF)] ^ (_value >> 8);
    }

    public static int Compute(string path)
    {
        var crc = new Crc32();
        var buffer = new byte[1024 * 1024];
        using var input = File.OpenRead(path);
        while (true)
        {
            var read = input.Read(buffer);
            if (read == 0) break;
            crc.Update(buffer.AsSpan(0, read));
        }

        return crc.Value;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            var value = i;
            for (var bit = 0; bit < 8; bit++) value = (value & 1) == 0 ? value >> 1 : 0xEDB8_8320u ^ (value >> 1);
            table[i] = value;
        }

        return table;
    }
}