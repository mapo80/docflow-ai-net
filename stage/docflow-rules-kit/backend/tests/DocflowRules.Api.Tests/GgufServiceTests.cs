using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DocflowRules.Api.Services;
using DocflowRules.Storage.EF;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class GgufServiceTests
{
    private AppDbContext NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    private IConfiguration NewCfg(string dir)
    {
        var dict = new System.Collections.Generic.Dictionary<string,string?> { ["LLM:Local:ModelsDir"] = dir };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task Delete_ok_when_not_used()
    {
        var dir = Path.Combine(Path.GetTempPath(), "gguf-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "m.gguf");
        File.WriteAllText(file, "dummy");
        await using var db = NewDb();
        var svc = new GgufService(db, NewCfg(dir), new System.Net.Http.HttpClientFactoryMock(), NullLogger<GgufService>.Instance);
        var ok = await svc.DeleteAvailableAsync(file, CancellationToken.None);
        ok.Should().BeTrue();
        File.Exists(file).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_blocked_if_enabled_model_refers()
    {
        var dir = Path.Combine(Path.GetTempPath(), "gguf-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "m.gguf");
        File.WriteAllText(file, "dummy");
        await using var db = NewDb();
        db.LlmModels.Add(new LlmModel { Id = Guid.NewGuid(), Provider="LlamaSharp", Enabled=true, ModelPathOrId=Path.GetFullPath(file), Name="local"});
        await db.SaveChangesAsync();
        var svc = new GgufService(db, NewCfg(dir), new System.Net.Http.HttpClientFactoryMock(), NullLogger<GgufService>.Instance);
        Func<Task> act = async ()=> await svc.DeleteAvailableAsync(file, CancellationToken.None);
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("abilitato");
        File.Exists(file).Should().BeTrue();
    }

    [Fact]
    public async Task Delete_blocked_if_active_model_refers()
    {
        var dir = Path.Combine(Path.GetTempPath(), "gguf-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "m.gguf");
        File.WriteAllText(file, "dummy");
        await using var db = NewDb();
        var mid = Guid.NewGuid();
        db.LlmModels.Add(new LlmModel { Id = mid, Provider="LlamaSharp", Enabled=false, ModelPathOrId=Path.GetFullPath(file), Name="local"});
        db.LlmSettings.Add(new LlmSettings { Id=1, ActiveModelId = mid, TurboProfile=false });
        await db.SaveChangesAsync();
        var svc = new GgufService(db, NewCfg(dir), new System.Net.Http.HttpClientFactoryMock(), NullLogger<GgufService>.Instance);
        Func<Task> act = async ()=> await svc.DeleteAvailableAsync(file, CancellationToken.None);
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("modello attivo");
        File.Exists(file).Should().BeTrue();
    }

    [Fact]
    public async Task Delete_blocked_if_outside_models_dir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "gguf-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.GetTempFileName();
        await using var db = NewDb();
        var svc = new GgufService(db, NewCfg(dir), new System.Net.Http.HttpClientFactoryMock(), NullLogger<GgufService>.Instance);
        Func<Task> act = async ()=> await svc.DeleteAvailableAsync(file, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

namespace System.Net.Http
{
    // minimal IHttpClientFactory mock for ctor signature; not used in these tests
    public class HttpClientFactoryMock : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient(new HttpMessageHandlerMock());
    }
    public class HttpMessageHandlerMock : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
