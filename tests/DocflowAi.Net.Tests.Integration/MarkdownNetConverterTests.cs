using System.Linq;
using System.Runtime.InteropServices;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Infrastructure.Markdown;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Core;
using SkiaSharp;
using BitMiracle.LibTiff.Classic;
using Xunit;
using Tesseract;

namespace DocflowAi.Net.Tests.Integration;

public class MarkdownNetConverterTests
{
    private readonly MarkdownNetConverter _converter = new(NullLogger<MarkdownNetConverter>.Instance);

    private static readonly bool HasTesseract = CheckTesseract();

    private static bool CheckTesseract()
    {
        try
        {
            bool lept = NativeLibrary.TryLoad("liblept.so.5", out var lh);
            if (lept) NativeLibrary.Free(lh);
            bool tess = NativeLibrary.TryLoad("libtesseract.so.5", out var th);
            if (tess) NativeLibrary.Free(th);
            if (!(lept && tess)) return false;
            using var engine = new TesseractEngine("/usr/share/tesseract-ocr/4.00/tessdata", "eng", EngineMode.Default);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public async Task Pdf_MultiPage_ProducesBoxesPerPage()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            var builder = new PdfDocumentBuilder();
            var page1 = builder.AddPage(100, 100);
            var page2 = builder.AddPage(100, 100);
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);
            page1.AddText("hello", 12, new PdfPoint(10, 90), font);
            page2.AddText("world", 12, new PdfPoint(10, 90), font);
            await File.WriteAllBytesAsync(tmp, builder.Build());
            await using var fs = File.OpenRead(tmp);
            var res = await _converter.ConvertPdfAsync(fs, new MarkdownOptions());
            res.Pages.Should().HaveCount(2);
            res.Boxes.Select(b => b.Page).Distinct().Should().HaveCount(2);
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public async Task EmptyStream_Throws()
    {
        await using var ms = new MemoryStream();
        await Assert.ThrowsAsync<MarkdownConversionException>(() => _converter.ConvertPdfAsync(ms, new MarkdownOptions()));
    }

    [Fact]
    public async Task Pdf_WithoutText_YieldsNoBoxes()
    {
        if (!HasTesseract) return;
        var tmp = Path.GetTempFileName();
        try
        {
            var builder = new PdfDocumentBuilder();
            builder.AddPage(100, 100);
            await File.WriteAllBytesAsync(tmp, builder.Build());
            await using var fs = File.OpenRead(tmp);
            var res = await _converter.ConvertPdfAsync(fs, new MarkdownOptions());
            res.Markdown.Should().BeEmpty();
            res.Boxes.Should().BeEmpty();
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public async Task Image_Png_ProducesMarkdown()
    {
        if (!HasTesseract) return;
        var tmp = Path.GetTempFileName();
        try
        {
            GenerateSkiaImage(tmp, SKEncodedImageFormat.Png);
            await using var fs = File.OpenRead(tmp);
            var res = await _converter.ConvertImageAsync(fs, new MarkdownOptions());
            res.Markdown.Should().Contain("hello");
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public async Task Image_Jpeg_ProducesMarkdown()
    {
        if (!HasTesseract) return;
        var tmp = Path.GetTempFileName();
        try
        {
            GenerateSkiaImage(tmp, SKEncodedImageFormat.Jpeg);
            await using var fs = File.OpenRead(tmp);
            var res = await _converter.ConvertImageAsync(fs, new MarkdownOptions());
            res.Markdown.Should().Contain("hello");
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public async Task Image_Tiff_ProducesMarkdown()
    {
        if (!HasTesseract) return;
        var tmp = Path.GetTempFileName();
        try
        {
            GenerateTiffImage(tmp);
            await using var fs = File.OpenRead(tmp);
            var res = await _converter.ConvertImageAsync(fs, new MarkdownOptions());
            res.Markdown.Should().Contain("hello");
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    private static void GenerateSkiaImage(string path, SKEncodedImageFormat format)
    {
        using var bmp = new SKBitmap(200, 100);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = SKColors.Black, TextSize = 24 };
        canvas.DrawText("hello", 10, 50, paint);
        using var image = SKImage.FromBitmap(bmp);
        using var data = image.Encode(format, 100);
        File.WriteAllBytes(path, data.ToArray());
    }

    private static void GenerateTiffImage(string path)
    {
        const int width = 200;
        const int height = 100;
        using var bmp = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = SKColors.Black, TextSize = 24 };
        canvas.DrawText("hello", 10, 50, paint);
        var size = bmp.BytesPerPixel * width * height;
        var pixels = new byte[size];
        System.Runtime.InteropServices.Marshal.Copy(bmp.GetPixels(), pixels, 0, size);
        using var tiff = Tiff.Open(path, "w");
        tiff.SetField(TiffTag.IMAGEWIDTH, width);
        tiff.SetField(TiffTag.IMAGELENGTH, height);
        tiff.SetField(TiffTag.BITSPERSAMPLE, 8);
        tiff.SetField(TiffTag.SAMPLESPERPIXEL, 4);
        tiff.SetField(TiffTag.ROWSPERSTRIP, height);
        tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
        tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
        tiff.SetField(TiffTag.COMPRESSION, Compression.LZW);
        for (int i = 0; i < height; i++)
        {
            var offset = i * width * 4;
            tiff.WriteScanline(pixels, offset, i, 0);
        }
    }
}
