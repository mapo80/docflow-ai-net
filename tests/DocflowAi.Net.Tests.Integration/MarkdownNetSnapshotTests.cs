using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Infrastructure.Markdown;
using Microsoft.Extensions.Logging.Abstractions;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Core;
using VerifyXunit;

namespace DocflowAi.Net.Tests.Integration;

public class MarkdownNetSnapshotTests
{
    [Fact]
    public async Task Pdf_Snapshot()
    {
        var converter = new MarkdownNetConverter(NullLogger<MarkdownNetConverter>.Instance);
        var tmp = Path.GetTempFileName();
        try
        {
            var builder = new PdfDocumentBuilder();
            var page = builder.AddPage(100, 100);
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);
            page.AddText("hello", 12, new PdfPoint(10, 90), font);
            await File.WriteAllBytesAsync(tmp, builder.Build());
            await using var fs = File.OpenRead(tmp);
            var res = await converter.ConvertPdfAsync(fs, new MarkdownOptions());
            await Verify(res);
        }
        finally
        {
            File.Delete(tmp);
        }
    }
}
