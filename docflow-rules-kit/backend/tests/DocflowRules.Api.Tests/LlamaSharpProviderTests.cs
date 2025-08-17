using System.Threading;
using System.Threading.Tasks;
using DocflowRules.Api.LLM;
using DocflowRules.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class LlamaSharpProviderTests
{
    [Fact]
    public async Task Missing_model_path_returns_empty()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new []{
            new System.Collections.Generic.KeyValuePair<string,string?>("LLM:Local:ModelPath","") // missing
        }).Build();
        var p = new LlamaSharpProvider(cfg, NullLogger<LlamaSharpProvider>.Instance);
        // Do not set runtime model; EnsureModel will throw and be caught, returning empty
        var res = await p.RefineAsync("r","code", new System.Text.Json.Nodes.JsonArray(), null, 3, 0.1, CancellationToken.None);
        res.Refined.Count.Should().Be(0);
        res.DurationMs.Should().BeGreaterThanOrEqualTo(0);
    }
}
