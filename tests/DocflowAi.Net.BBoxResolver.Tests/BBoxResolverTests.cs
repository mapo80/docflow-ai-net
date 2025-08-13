using DocflowAi.Net.BBoxResolver;
using FluentAssertions;
using Xunit;

namespace DocflowAi.Net.BBoxResolver.Tests;

public class BBoxResolverTests
{
    private static DocumentIndex BuildIndex(params (string text, float x)[] words)
    {
        var pages = new List<DocumentIndexBuilder.SourcePage>{ new(1,100,100) };
        var wds = new List<DocumentIndexBuilder.SourceWord>();
        for (int i=0;i<words.Length;i++)
        {
            var w = words[i];
            wds.Add(new DocumentIndexBuilder.SourceWord(1, w.text, w.x/100, 0, 0.1f, 0.1f, false));
        }
        return DocumentIndexBuilder.Build(pages, wds);
    }

    [Fact]
    public async Task ExactMatch_IsResolved()
    {
        var index = BuildIndex(("Hello",0f), ("World",10f));
        var field = new ExtractedField("greet", "hello world", 0.5);
        var resolver = new BBoxResolver(Microsoft.Extensions.Options.Options.Create(new BBoxOptions()));
        var res = await resolver.ResolveAsync(index, new[]{field});
        res[0].Spans.Should().NotBeEmpty();
        res[0].Spans[0].Text.Should().Be("Hello World");
    }

    [Fact]
    public async Task NumericFormats_Normalize()
    {
        var index = BuildIndex(("1,234.56",0f));
        var field = new ExtractedField("num", "1.234,56", 0.5);
        var resolver = new BBoxResolver(Microsoft.Extensions.Options.Options.Create(new BBoxOptions()));
        var res = await resolver.ResolveAsync(index, new[]{field});
        res[0].Spans.Should().NotBeEmpty();
    }
}
