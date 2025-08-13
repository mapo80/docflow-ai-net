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

    [Fact]
    public async Task Image_Small_Png_ProducesMarkdown()
    {
        if (!HasTesseract) return;
        var tmp = Path.GetTempFileName();
        try
        {
            GenerateSmallSkiaImage(tmp);
            await using var fs = File.OpenRead(tmp);
            var res = await _converter.ConvertImageAsync(fs, new MarkdownOptions());
            res.Markdown.Should().Contain("hi");
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Theory]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public async Task Image_Rotated_Png_ProducesMarkdown(int angle)
    {
        if (!HasTesseract) return;
        var tmp = Path.GetTempFileName();
        try
        {
            GenerateRotatedSkiaImage(tmp, angle, SKEncodedImageFormat.Png);
            await using var fs = File.OpenRead(tmp);
            var res = await _converter.ConvertImageAsync(fs, new MarkdownOptions());
            res.Markdown.Should().Contain("hello");
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Theory]
    [InlineData(50)]
    [InlineData(600)]
    public async Task Image_Tiff_WithDifferentDpi_ProducesMarkdown(int dpi)
    {
        if (!HasTesseract) return;
        var tmp = Path.GetTempFileName();
        try
        {
            GenerateTiffImage(tmp, dpi);
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

    private static void GenerateSmallSkiaImage(string path)
    {
        using var bmp = new SKBitmap(40, 40);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = SKColors.Black, TextSize = 12 };
        canvas.DrawText("hi", 2, 20, paint);
        using var image = SKImage.FromBitmap(bmp);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }

    private static void GenerateRotatedSkiaImage(string path, int angle, SKEncodedImageFormat format)
    {
        using var src = new SKBitmap(200, 100);
        using (var canvas = new SKCanvas(src))
        {
            canvas.Clear(SKColors.White);
            using var paint = new SKPaint { Color = SKColors.Black, TextSize = 24 };
            canvas.DrawText("hello", 10, 50, paint);
        }
        SKBitmap dst = angle % 180 == 0 ? new SKBitmap(src.Width, src.Height) : new SKBitmap(src.Height, src.Width);
        using (var canvas = new SKCanvas(dst))
        {
            canvas.Clear(SKColors.White);
            canvas.Translate(dst.Width / 2f, dst.Height / 2f);
            canvas.RotateDegrees(angle);
            canvas.Translate(-src.Width / 2f, -src.Height / 2f);
            canvas.DrawBitmap(src, 0, 0);
        }
        using var image = SKImage.FromBitmap(dst);
        using var data = image.Encode(format, 100);
        File.WriteAllBytes(path, data.ToArray());
    }

    private static void GenerateTiffImage(string path, int dpi = 96)
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
        tiff.SetField(TiffTag.XRESOLUTION, (float)dpi);
        tiff.SetField(TiffTag.YRESOLUTION, (float)dpi);
        tiff.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);
        for (int i = 0; i < height; i++)
        {
            var offset = i * width * 4;
            tiff.WriteScanline(pixels, offset, i, 0);
        }
    }
}
