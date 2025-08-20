using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using MkdnConverter = MarkItDownNet.MarkIt\u0044ownConverter;
using MkdnOptions = MarkItDownNet.MarkIt\u0044ownOptions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Tesseract;
using System.Runtime.InteropServices;

namespace DocflowAi.Net.Infrastructure.Markdown;

/// <summary>Adapter around the MarkItDownNet library.</summary>
public sealed class MarkdownNetConverter : IMarkdownConverter
{
    private readonly ILogger<MarkdownNetConverter> _logger;
    private readonly Serilog.ILogger _mdLogger;
    private static bool _ocrLibsLoaded;

    public MarkdownNetConverter(ILogger<MarkdownNetConverter> logger)
    {
        _logger = logger;
        var verbose = string.Equals(Environment.GetEnvironmentVariable("MARKDOWNNET_VERBOSE"), "true", StringComparison.OrdinalIgnoreCase);
        _mdLogger = new LoggerConfiguration()
            .MinimumLevel.Is(verbose ? LogEventLevel.Debug : LogEventLevel.Information)
            .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
            .CreateLogger();
    }

    public Task<MarkdownResult> ConvertPdfAsync(Stream pdf, MarkdownOptions opts, CancellationToken ct = default)
        => ConvertAsync(pdf, "application/pdf", opts, ct);

    public Task<MarkdownResult> ConvertImageAsync(Stream image, MarkdownOptions opts, CancellationToken ct = default)
        => ConvertAsync(image, "image/unknown", opts, ct);

    private async Task<MarkdownResult> ConvertAsync(Stream input, string mime, MarkdownOptions opts, CancellationToken ct)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        var tmp = Path.GetTempFileName();
        try
        {
            _logger.LogDebug("Copying input stream to temp file {TempFile}", tmp);
            await CopyWithRetryAsync(input, tmp, ct);
            _logger.LogDebug("Starting MarkItDownNet conversion for {Mime} into {TempFile}", mime, tmp);
            var options = new MkdnOptions
            {
                NormalizeMarkdown = opts.NormalizeMarkdown,
                OcrDataPath = opts.OcrDataPath ?? Environment.GetEnvironmentVariable("OCR_DATA_PATH"),
                OcrLanguages = opts.OcrLanguages,
                PdfRasterDpi = opts.PdfRasterDpi,
                MinimumNativeWordThreshold = opts.MinimumNativeWordThreshold
            };
            var converter = new MkdnConverter(options, _mdLogger);
            EnsureOcrLibraries();
            var res = await converter.ConvertAsync(tmp, mime, ct);

            var pages = res.Pages.Select(p => new PageInfo(p.Number, p.Width, p.Height)).ToList();
            var dict = pages.ToDictionary(p => p.Number);
            var boxes = res.Words.Select(w =>
            {
                var page = dict[w.Page];
                var x = w.BBox.X * page.Width;
                var y = w.BBox.Y * page.Height;
                var width = w.BBox.Width * page.Width;
                var height = w.BBox.Height * page.Height;
                return new Box(w.Page, x, y, width, height, w.BBox.X, w.BBox.Y, w.BBox.Width, w.BBox.Height, w.Text);
            }).ToList();

            _logger.LogInformation("MarkItDownNet produced {PageCount} pages", pages.Count);
            _logger.LogDebug("Converted to markdown with {Length} chars and {Boxes} boxes", res.Markdown.Length, boxes.Count);
            return new MarkdownResult(res.Markdown, pages, boxes);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Unsupported input {Mime}", mime);
            throw new MarkdownConversionException("unsupported_format", ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conversion failed");
            throw new MarkdownConversionException("conversion_failed", ex.Message, ex);
        }
        finally
        {
            try { File.Delete(tmp); } catch { }
        }
    }

    private static void EnsureOcrLibraries()
    {
        if (_ocrLibsLoaded) return;

        var dir = Path.Combine(AppContext.BaseDirectory, "x64");
        if (Directory.Exists(dir))
        {
            TesseractEnviornment.CustomSearchPath = dir;
            foreach (var lib in new[] { "libleptonica-1.82.0.so", "libtesseract50.so" })
            {
                var path = Path.Combine(dir, lib);
                if (File.Exists(path))
                {
                    try { NativeLibrary.Load(path); } catch { }
                }
            }
        }
        else
        {
            foreach (var lib in new[] { "libleptonica-1.82.0.so", "libtesseract.so.5", "libtesseract50.so" })
            {
                try { NativeLibrary.Load(lib); } catch { }
            }
        }

        _ocrLibsLoaded = true;
    }

    private static async Task CopyWithRetryAsync(Stream input, string path, CancellationToken ct)
    {
        const int retries = 3;
        for (var i = 0; i < retries; i++)
        {
            try
            {
                input.Position = 0;
                await using var fs = File.Create(path);
                await input.CopyToAsync(fs, ct);
                return;
            }
            catch (IOException) when (i < retries - 1)
            {
                await Task.Delay(100, ct);
            }
        }
    }
}

/// <summary>Error during markdown conversion.</summary>
public sealed class MarkdownConversionException : Exception
{
    public string Code { get; }
    public string? Details { get; }

    public MarkdownConversionException(string code, string message, Exception? inner = null, string? details = null)
        : base(message, inner)
    {
        Code = code;
        Details = details;
    }
}
