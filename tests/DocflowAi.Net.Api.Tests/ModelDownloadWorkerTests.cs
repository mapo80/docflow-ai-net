using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using DocflowAi.Net.Api.Features.Models;
using DocflowAi.Net.Api.Features.Models.Downloaders;
using DocflowAi.Net.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.Api.Tests;

public class ModelDownloadWorkerTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelDownloadWorkerTests(TempDirFixture fx) => _fx = fx;

    private static ServiceProvider BuildProvider(TempDirFixture fx, string? token, out HttpClient http)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var cfgDict = new Dictionary<string, string?>
        {
            ["ModelStorage:Root"] = Path.Combine(fx.RootPath, "models")
        };
        if (token != null)
            cfgDict["HF_TOKEN"] = token;
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(cfgDict!).Build();
        services.AddSingleton<IConfiguration>(cfg);
        services.AddDbContext<ModelCatalogDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        http = new HttpClient(new HttpMessageHandlerStub());
        services.AddSingleton(http);
        services.AddSingleton<IModelDownloader, NoopDownloader>();
        services.AddSingleton<ModelDownloadWorker>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Adds_Bearer_Header_When_Token_Provided()
    {
        var provider = BuildProvider(_fx, "secret", out var http);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ModelCatalogDbContext>();
        db.Database.EnsureCreated();
        var model = new GgufModel
        {
            Name = "m",
            SourceType = ModelSourceType.HuggingFace,
            HfRepo = "r",
            HfFilename = "f"
        };
        db.Models.Add(model);
        db.SaveChanges();

        var worker = provider.GetRequiredService<ModelDownloadWorker>();
        worker.Enqueue(model.Id);
        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        for (int i = 0; i < 40; i++)
        {
            await Task.Delay(50);
            var m = await db.Models.AsNoTracking().FirstAsync(x => x.Id == model.Id);
            if (m.Status == ModelStatus.Available) break;
        }
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        http.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        http.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        http.DefaultRequestHeaders.Authorization!.Parameter.Should().Be("secret");
    }

    [Fact]
    public async Task Leaves_Header_Null_When_Token_Missing()
    {
        var provider = BuildProvider(_fx, null, out var http);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ModelCatalogDbContext>();
        db.Database.EnsureCreated();
        var model = new GgufModel
        {
            Name = "m",
            SourceType = ModelSourceType.HuggingFace,
            HfRepo = "r",
            HfFilename = "f"
        };
        db.Models.Add(model);
        db.SaveChanges();

        var worker = provider.GetRequiredService<ModelDownloadWorker>();
        worker.Enqueue(model.Id);
        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        for (int i = 0; i < 40; i++)
        {
            await Task.Delay(50);
            var m = await db.Models.AsNoTracking().FirstAsync(x => x.Id == model.Id);
            if (m.Status == ModelStatus.Available) break;
        }
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        http.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    private class NoopDownloader : IModelDownloader
    {
        public bool CanHandle(GgufModel model) => true;
        public Task DownloadAsync(GgufModel model, string targetPath, IProgress<int> progress, CancellationToken ct)
        {
            progress.Report(100);
            return Task.CompletedTask;
        }
    }

    private class HttpMessageHandlerStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new ByteArrayContent([]) });
    }
}
