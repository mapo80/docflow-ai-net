using DocflowAi.Net.BBoxResolver;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DocflowAi.Net.BBoxResolver.Tests;

public class PointerStrategyTests
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
    public void WordIdMapBuilder_Generates_Stable_Ids()
    {
        var index = BuildIndex(("a",0f), ("b",10f));
        var builder = new WordIdMapBuilder(Options.Create(new PointerOptions()));
        var m1 = builder.Build(index);
        var m2 = builder.Build(index);
        m1.map.Should().BeEquivalentTo(m2.map);
    }

    [Fact]
    public void PlainTextViewBuilder_Offsets_To_Words()
    {
        var index = BuildIndex(("hello",0f), ("world",10f));
        var builder = new PlainTextViewBuilder();
        var (text, spans) = builder.Build(index);
        text.Should().Be("hello world");
        spans.Should().Contain(x => x.page==0 && x.wordIdx==1 && x.start==6 && x.len==5);
    }

    [Fact]
    public void PointerResolver_Contiguity_With_MaxGap()
    {
        var index = BuildIndex(("w10",0f), ("w11",10f), ("w12",20f), ("w13",30f));
        var options = Options.Create(new PointerOptions { Strict = true, MaxGapBetweenIds = 1 });
        var resolver = new PointerResolver(options, new PlainTextViewBuilder());
        var field = new ExtractedField("f", "", 1.0, null, new Pointer(PointerMode.WordIds, new[]{"W0_0","W0_2"}, null, null));
        resolver.TryResolve(index, field, out var res).Should().BeTrue();
        res.Confidence.Should().BeLessThan(1.0);
        var fieldBad = new ExtractedField("f", "", 1.0, null, new Pointer(PointerMode.WordIds, new[]{"W0_0","W0_3"}, null, null));
        resolver.TryResolve(index, fieldBad, out _).Should().BeFalse();
    }

    [Fact]
    public void Pointer_WordIds_ACME_SPA_Exact()
    {
        var index = BuildIndex(("ACME",0f), ("SPA",10f));
        var resolver = new PointerResolver(Options.Create(new PointerOptions()), new PlainTextViewBuilder());
        var field = new ExtractedField("company", "ACME SPA", 0.9, null, new Pointer(PointerMode.WordIds, new[]{"W0_0","W0_1"}, null, null));
        resolver.TryResolve(index, field, out var res).Should().BeTrue();
        res.Spans[0].WordIndices.Should().BeEquivalentTo(new[]{0,1});
    }

    [Fact]
    public async Task Pointer_WordIds_InvalidIds_FallbackToTokenFirst()
    {
        var index = BuildIndex(("ACME",0f), ("SPA",10f));
        var pointerOptions = Options.Create(new ResolverOptions());
        var orchestrator = new ResolverOrchestrator(pointerOptions, new PointerResolver(Options.Create(new PointerOptions()), new PlainTextViewBuilder()), new TokenFirstBBoxResolver(Options.Create(new BBoxOptions())), new LegacyBBoxResolver(Options.Create(new BBoxOptions())));
        var field = new ExtractedField("company", "ACME SPA", 0.9, null, new Pointer(PointerMode.WordIds, new[]{"W0_99"}, null, null));
        var res = await orchestrator.ResolveAsync(index, new[]{field});
        res[0].Spans.Should().NotBeEmpty();
        res[0].Pointer.Should().BeNull();
    }

    [Fact]
    public void Pointer_Offsets_ACME_SPA_Exact()
    {
        var index = BuildIndex(("ACME",0f), ("SPA",10f));
        var builder = new PlainTextViewBuilder();
        var (text, spans) = builder.Build(index);
        var start = spans[0].start;
        var end = spans[1].start + spans[1].len;
        var resolver = new PointerResolver(Options.Create(new PointerOptions { Mode = PointerMode.Offsets }), builder);
        var field = new ExtractedField("company", "ACME SPA", 0.9, null, new Pointer(PointerMode.Offsets, null, start, end));
        resolver.TryResolve(index, field, out var res).Should().BeTrue();
        res.Spans[0].WordIndices.Should().BeEquivalentTo(new[]{0,1});
    }

    [Fact]
    public async Task Pointer_Offsets_Empty_Fallback()
    {
        var index = BuildIndex(("ACME",0f), ("SPA",10f));
        var ropts = new ResolverOptions { Pointer = new PointerOptions { Mode = PointerMode.Offsets } };
        var orchestrator = new ResolverOrchestrator(Options.Create(ropts), new PointerResolver(Options.Create(ropts.Pointer), new PlainTextViewBuilder()), new TokenFirstBBoxResolver(Options.Create(new BBoxOptions())), new LegacyBBoxResolver(Options.Create(new BBoxOptions())));
        var field = new ExtractedField("company", "ACME SPA", 0.9, null, new Pointer(PointerMode.Offsets, null, 0, 0));
        var res = await orchestrator.ResolveAsync(index, new[]{field});
        res[0].Spans.Should().NotBeEmpty();
    }
}

