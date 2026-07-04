using System.Buffers.Binary;
using System.Text;

namespace PakTool.IO;

internal static class BinaryIo
{
    public static int ReadU1(this Stream stream)
    {
        var value = stream.ReadByte();
        if (value < 0) throw new EndOfStreamException();
        return value;
    }

    public static long ReadU4(this Stream stream)
    {
        Span<byte> bytes = stackalloc byte[4];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    public static int ReadI4(this Stream stream)
    {
        Span<byte> bytes = stackalloc byte[4];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    public static double ReadF64(this Stream stream)
    {
        Span<byte> bytes = stackalloc byte[8];
        stream.ReadExactly(bytes);
        return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(bytes));
    }

    public static string ReadAscii(this Stream stream, int length)
    {
        return Encoding.ASCII.GetString(stream.ReadExactBytes(length));
    }

    public static string ReadUtf8(this Stream stream, int length)
    {
        return Encoding.UTF8.GetString(stream.ReadExactBytes(length));
    }

    public static byte[] ReadExactBytes(this Stream stream, int length)
    {
        var bytes = new byte[length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    public static void WriteU4(this Stream stream, long value)
    {
        if (value is < 0 or > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(value), $"Value does not fit UInt32: {value}");
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)value);
        stream.Write(bytes);
    }

    public static void WriteI4(this Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        stream.Write(bytes);
    }

    public static void WriteF64(this Stream stream, double value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, BitConverter.DoubleToInt64Bits(value));
        stream.Write(bytes);
    }

    public static void WriteAscii(this Stream stream, string value)
    {
        stream.Write(Encoding.ASCII.GetBytes(value));
    }

    public static void WriteZeroes(this Stream stream, long count)
    {
        var buffer = new byte[8192];
        var remaining = count;
        while (remaining > 0)
        {
            var chunk = (int)Math.Min(buffer.Length, remaining);
            stream.Write(buffer, 0, chunk);
            remaining -= chunk;
        }
    }
}