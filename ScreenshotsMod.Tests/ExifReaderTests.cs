using System.Text;
using Xunit;

namespace Nf3t.VintageStory.Screenshots.Tests;

public class ExifReaderTests
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static void Chunk(MemoryStream ms, string type, byte[] data)
    {
        var length = BitConverter.GetBytes((uint)data.Length);
        Array.Reverse(length);
        ms.Write(length);
        ms.Write(Encoding.ASCII.GetBytes(type));
        ms.Write(data);
        ms.Write(new byte[4]); // CRC: not checked by the reader
    }

    /// <summary>A little-endian TIFF block with one ImageDescription entry (inline when it fits in four bytes).</summary>
    private static byte[] Tiff(string description, bool withExifPrefix = true)
    {
        var text = Encoding.UTF8.GetBytes(description + "\0");
        var ms = new MemoryStream();
        if (withExifPrefix) ms.Write("Exif\0\0"u8);
        ms.Write("II*\0"u8);
        ms.Write(BitConverter.GetBytes(8u)); // IFD0 at offset 8
        ms.Write(BitConverter.GetBytes((ushort)1)); // one entry
        ms.Write(BitConverter.GetBytes((ushort)0x010E)); // ImageDescription
        ms.Write(BitConverter.GetBytes((ushort)2)); // ASCII
        ms.Write(BitConverter.GetBytes((uint)text.Length));
        if (text.Length <= 4)
        {
            var inline = new byte[4];
            text.CopyTo(inline, 0);
            ms.Write(inline);
        }
        else
        {
            var valueOffset = (uint)(ms.Position - (withExifPrefix ? 6 : 0) + 4 + 4);
            ms.Write(BitConverter.GetBytes(valueOffset));
        }

        ms.Write(new byte[4]); // no next IFD
        if (text.Length > 4) ms.Write(text);
        return ms.ToArray();
    }

    private static MemoryStream Png(params (string type, byte[] data)[] chunks)
    {
        var ms = new MemoryStream();
        ms.Write(Signature);
        foreach (var (type, data) in chunks) Chunk(ms, type, data);
        Chunk(ms, "IEND", []);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Reads_the_description_from_the_eXIf_chunk()
    {
        const string json = "{\"Type\":\"screenshotLocationMetaData\",\"WorldSeed\":42}";
        using var png = Png(("IHDR", new byte[13]), ("eXIf", Tiff(json)), ("IDAT", new byte[10]));

        Assert.Equal(json, ExifReader.ReadImageDescription(png));
    }

    [Fact]
    public void Finds_eXIf_after_image_data()
    {
        using var png = Png(("IHDR", new byte[13]), ("IDAT", new byte[100]), ("eXIf", Tiff("hello world")));

        Assert.Equal("hello world", ExifReader.ReadImageDescription(png));
    }

    [Fact]
    public void Reads_a_short_description_stored_inside_the_entry()
    {
        using var png = Png(("IHDR", new byte[13]), ("eXIf", Tiff("ab", withExifPrefix: false)));

        Assert.Equal("ab", ExifReader.ReadImageDescription(png));
    }

    [Fact]
    public void Returns_null_without_eXIf()
    {
        using var png = Png(("IHDR", new byte[13]), ("IDAT", new byte[10]));

        Assert.Null(ExifReader.ReadImageDescription(png));
    }

    [Fact]
    public void Returns_null_for_a_file_that_is_not_a_png()
    {
        using var ms = new MemoryStream("this is a jpeg, honest"u8.ToArray());

        Assert.Null(ExifReader.ReadImageDescription(ms));
    }

    [Fact]
    public void Returns_null_for_a_truncated_file_without_throwing()
    {
        // Once the eXIf chunk is complete its text is returned; before that, and for every other cut, it is null.
        using var full = Png(("IHDR", new byte[13]), ("eXIf", Tiff("some text")));
        var bytes = full.ToArray();
        for (var length = 0; length < bytes.Length; length++)
        {
            using var truncated = new MemoryStream(bytes, 0, length);
            var text = ExifReader.ReadImageDescription(truncated);
            Assert.True(text == null || text == "some text");
        }
    }

    [Fact]
    public void Returns_null_for_a_corrupt_TIFF_block()
    {
        using var png = Png(("eXIf", "II*\0\xFF\xFF\xFF\xFF"u8.ToArray()));

        Assert.Null(ExifReader.ReadImageDescription(png));
    }
}
