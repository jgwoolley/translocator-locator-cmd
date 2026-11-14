#nullable enable

using System;
using System.IO;
using System.Text;

namespace Nf3t.VintageStory.Screenshots;

/// <summary>
/// Reads the ImageDescription text the game writes into a screenshot's EXIF data (its <c>eXIf</c> PNG chunk), by
/// reading the file directly. No image or EXIF library is needed. Damaged or unexpected files give null, never an
/// exception. The CRC of each chunk is not checked.
/// </summary>
public static class ExifReader
{
    private const uint ImageDescriptionTag = 0x010E;
    private const ushort AsciiType = 2;
    private const long MaxExifBytes = 1 << 20;
    private const int IfdEntrySize = 12;
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Returns the ImageDescription of the PNG at <paramref name="path"/>, or null if it has none.</summary>
    public static string? ReadImageDescription(string path)
    {
        using var stream = File.OpenRead(path);
        return ReadImageDescription(stream);
    }

    /// <summary>Returns the ImageDescription of a PNG stream, or null if it has none. The stream must be seekable.</summary>
    public static string? ReadImageDescription(Stream stream)
    {
        try
        {
            var signature = new byte[PngSignature.Length];
            if (!ReadExactly(stream, signature) || !signature.AsSpan().SequenceEqual(PngSignature)) return null;

            var header = new byte[8];
            while (ReadExactly(stream, header))
            {
                var length = ReadUInt32(header, 0, bigEndian: true);
                var type = Encoding.ASCII.GetString(header, 4, 4);
                if (type == "IEND") return null;

                if (type == "eXIf")
                {
                    if (length > MaxExifBytes) return null;
                    var data = new byte[length];
                    return ReadExactly(stream, data) ? ReadTiffDescription(data) : null;
                }

                // Skip the chunk's data and its CRC.
                var next = stream.Position + (long)length + 4;
                if (next > stream.Length) return null;
                stream.Position = next;
            }
        }
        catch (Exception)
        {
            // A damaged file is not an error for the caller: it is simply not a screenshot with location data.
        }

        return null;
    }

    private static string? ReadTiffDescription(byte[] data)
    {
        var start = data.Length >= 6 && data[0] == 'E' && data[1] == 'x' && data[2] == 'i' && data[3] == 'f' &&
                    data[4] == 0 && data[5] == 0
            ? 6
            : 0;

        bool littleEndian;
        if (data[start] == 'I' && data[start + 1] == 'I') littleEndian = true;
        else if (data[start] == 'M' && data[start + 1] == 'M') littleEndian = false;
        else return null;

        if (ReadUInt16(data, start + 2, littleEndian) != 42) return null;

        var ifd = start + (int)ReadUInt32(data, start + 4, bigEndian: !littleEndian);
        var entries = ReadUInt16(data, ifd, littleEndian);
        for (var i = 0; i < entries; i++)
        {
            var entry = ifd + 2 + i * IfdEntrySize;
            if (ReadUInt16(data, entry, littleEndian) != ImageDescriptionTag) continue;
            if (ReadUInt16(data, entry + 2, littleEndian) != AsciiType) return null;

            // Values of up to four bytes sit inside the entry; longer ones are at an offset from the TIFF start.
            var count = (int)ReadUInt32(data, entry + 4, bigEndian: !littleEndian);
            var valueAt = count <= 4 ? entry + 8 : start + (int)ReadUInt32(data, entry + 8, bigEndian: !littleEndian);
            if (count <= 0 || valueAt < 0 || valueAt + count > data.Length) return null;
            return Encoding.UTF8.GetString(data, valueAt, count).TrimEnd('\0');
        }

        return null;
    }

    private static bool ReadExactly(Stream stream, byte[] buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer, total, buffer.Length - total);
            if (read == 0) return false;
            total += read;
        }

        return true;
    }

    private static uint ReadUInt32(byte[] data, int offset, bool bigEndian)
    {
        uint value = 0;
        for (var i = 0; i < 4; i++)
        {
            var index = bigEndian ? offset + i : offset + 3 - i;
            value = (value << 8) | data[index];
        }

        return value;
    }

    private static ushort ReadUInt16(byte[] data, int offset, bool littleEndian)
    {
        return littleEndian
            ? (ushort)(data[offset] | (data[offset + 1] << 8))
            : (ushort)((data[offset] << 8) | data[offset + 1]);
    }
}
