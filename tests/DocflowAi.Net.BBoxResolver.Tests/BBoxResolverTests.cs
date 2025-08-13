using DocflowAi.Net.BBoxResolver;
using FluentAssertions;
using Xunit;

namespace DocflowAi.Net.BBoxResolver.Tests;

public class BBoxResolverTests
{
    private static DocumentIndex BuildIndex(params (string text, float x)[] words)
    {
        var pages = new List<DocumentIndexBuilder.SourcePage> { new(1, 100, 100) };
        var wds = new List<DocumentIndexBuilder.SourceWord>();
        for (int i = 0; i < words.Length; i++)
        {
            var w = words[i];
            wds.Add(new DocumentIndexBuilder.SourceWord(1, w.text, w.x / 100, 0, 0.1f, 0.1f, false));
        }
        return DocumentIndexBuilder.Build(pages, wds);
    }

    [Fact]
    public async Task ExactMatch_IsResolved()
    {
        var index = BuildIndex(("Hello",0f), ("World",10f));
        var field = new ExtractedField("greet", "hello world", 0.5);
        var resolver = new TokenFirstBBoxResolver(Microsoft.Extensions.Options.Options.Create(new BBoxOptions()));
        var res = await resolver.ResolveAsync(index, new[]{field});
        res[0].Spans.Should().NotBeEmpty();
        res[0].Spans[0].Text.Should().Be("Hello World");
    }

    [Fact]
    public async Task NumericFormats_Normalize()
    {
        var index = BuildIndex(("1,234.56",0f));
        var field = new ExtractedField("num", "1.234,56", 0.5);
        var resolver = new TokenFirstBBoxResolver(Microsoft.Extensions.Options.Options.Create(new BBoxOptions()));
        var res = await resolver.ResolveAsync(index, new[]{field});
        res[0].Spans.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Acronym_Match()
    {
        var index = BuildIndex(("ACME",0f), ("S.p.A.",10f));
        var field = new ExtractedField("company", "ACME SPA", 0.5);
        var resolver = new TokenFirstBBoxResolver(Microsoft.Extensions.Options.Options.Create(new BBoxOptions()));
        var res = await resolver.ResolveAsync(index, new[]{field});
        res[0].Spans.Should().NotBeEmpty();
        res[0].Spans[0].WordIndices.Should().BeEquivalentTo(new[]{0,1});
    }

    [Fact]
    public async Task ShortValue_AdaptiveThreshold()
    {
        var index = BuildIndex(("ABCDEFG",0f));
        var field = new ExtractedField("code", "ABCDXYG", 0.5);
        var resolver = new TokenFirstBBoxResolver(Microsoft.Extensions.Options.Options.Create(new BBoxOptions()));
        var res = await resolver.ResolveAsync(index, new[]{field});
        res[0].Spans.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Duplicates_LabelProximity()
    {
        var index = BuildIndex(("Code",0f), ("123",10f), ("123",20f));
        var field = new ExtractedField("Code", "123", 0.5);
        var resolver = new TokenFirstBBoxResolver(Microsoft.Extensions.Options.Options.Create(new BBoxOptions()));
        var res = await resolver.ResolveAsync(index, new[]{field});
        res[0].Spans.Should().NotBeEmpty();
        res[0].Spans[0].WordIndices[0].Should().Be(1);
    }

    [Fact]
    public async Task Performance_Smoke()
    {
        var pages = new List<DocumentIndexBuilder.SourcePage>{ new(1,100,100) };
        var words = new List<DocumentIndexBuilder.SourceWord>();
        for(int i=0;i<6000;i++)
            words.Add(new DocumentIndexBuilder.SourceWord(1,$"w{i}", i/6000f,0,0.1f,0.1f,false));
        var index = DocumentIndexBuilder.Build(pages, words);
        var fields = Enumerable.Range(0,20).Select(i=> new ExtractedField($"f{i}", $"w{i*3}",0.5)).ToList();
        var resolver = new TokenFirstBBoxResolver(Microsoft.Extensions.Options.Options.Create(new BBoxOptions()));
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var res = await resolver.ResolveAsync(index, fields);
        sw.Stop();
        (sw.Elapsed.TotalMilliseconds/fields.Count).Should().BeLessThan(50);
        res.Should().HaveCount(20);
    }
}
