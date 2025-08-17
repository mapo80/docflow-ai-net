using System.Threading;
using System.Threading.Tasks;
using DocflowRules.Api.LLM;
using DocflowRules.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class OpenAiProviderTests
{
    class DummyHttpFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient(new HttpClientHandler());
    }

    [Fact]
    public async Task Without_api_key_returns_empty_and_zero_usage()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new []{
            new System.Collections.Generic.KeyValuePair<string,string?>("LLM:Provider","OpenAI")
        }).Build();
        var p = new OpenAiProvider(new DummyHttpFactory(), cfg, NullLogger<OpenAiProvider>.Instance);
        var res = await p.RefineAsync("r","code", new System.Text.Json.Nodes.JsonArray(), null, 5, 0.2, CancellationToken.None);
        res.Refined.Count.Should().Be(0);
        res.InputTokens.Should().Be(0);
        res.OutputTokens.Should().Be(0);
    }
}
