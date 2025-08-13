using System.Text.Json;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Infrastructure.Markdown;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using System.Runtime.InteropServices;
using Tesseract;

namespace DocflowAi.Net.Tests.Integration;

public class DatasetMarkdownNetTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
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
    public async Task SamplePdf_ConversionMatchesReference()
    {
        if (!HasTesseract) return;
        var pdfPath = Path.Combine(Root, "dataset", "sample_invoice.pdf");
        await using var fs = File.OpenRead(pdfPath);
        var result = await _converter.ConvertPdfAsync(fs, new MarkdownOptions());

        // Save JSON output for inspection
        var outDir = Path.Combine(Root, "dataset", "test-pdf-markitdownnet");
        Directory.CreateDirectory(outDir);
        var jsonPath = Path.Combine(outDir, "result.json");
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        if (!File.Exists(jsonPath))
            await File.WriteAllTextAsync(jsonPath, json);

        var expectedMarkdownPath = Path.Combine(Root, "dataset", "test-pdf", "markitdown.txt");
        var expectedMarkdown = await File.ReadAllTextAsync(expectedMarkdownPath);
        result.Markdown.Trim().Should().Be(expectedMarkdown.Trim());
    }

    [Fact]
    public async Task SamplePng_ContainsExpectedWords()
    {
        if (!HasTesseract) return;
        var pngPath = Path.Combine(Root, "dataset", "sample_invoice.png");
        await using var fs = File.OpenRead(pngPath);
        var result = await _converter.ConvertImageAsync(fs, new MarkdownOptions());

        var outDir = Path.Combine(Root, "dataset", "test-png-markitdownnet");
        Directory.CreateDirectory(outDir);
        var jsonPath = Path.Combine(outDir, "result.json");
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        if (!File.Exists(jsonPath))
            await File.WriteAllTextAsync(jsonPath, json);

        var expectedJsonPath = Path.Combine(Root, "dataset", "test-png", "markitdown.txt");
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(expectedJsonPath));
        var words = doc.RootElement.GetProperty("ocr").GetProperty("words").EnumerateArray().Select(e => e.GetProperty("text").GetString()!).ToList();
        foreach (var w in words)
        {
            result.Markdown.Should().Contain(w);
        }
    }
}
