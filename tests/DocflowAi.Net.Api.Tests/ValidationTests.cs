using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Serilog.Sinks.TestCorrelator;
using System.Net;
using System.Net.Http.Json;
using DocflowAi.Net.Api.JobQueue.Data;

namespace DocflowAi.Net.Api.Tests;

public class ValidationTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ValidationTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Payload_too_large_returns_413()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath, uploadLimitMb:1);
        var client = factory.CreateClient();
        var big = new byte[2 * 1024 * 1024];
        Guid msId;
        using (var scopeMs = factory.Services.CreateScope())
        {
            var dbMs = scopeMs.ServiceProvider.GetRequiredService<JobDbContext>();
            var ms = dbMs.MarkdownSystems.FirstOrDefault();
            if (ms == null)
            {
                ms = new DocflowAi.Net.Api.MarkdownSystem.Models.MarkdownSystemDocument { Id = Guid.NewGuid(), Name = "d", Provider = "docling", Endpoint = "http://localhost", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
                dbMs.MarkdownSystems.Add(ms);
                dbMs.SaveChanges();
            }
            msId = ms.Id;
        }
        var resp = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = Convert.ToBase64String(big), fileName = "a.pdf", model = "m", templateToken = "t", language = "eng", markdownSystemId = msId });
        resp.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        Directory.GetDirectories(factory.DataRootPath).Should().BeEmpty();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        store.CountPending().Should().Be(0);
    }

    [Fact]
    public async Task Invalid_mime_returns_400()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var bytes = new byte[10];
        Guid msId2;
        using (var scopeMs2 = factory.Services.CreateScope())
        {
            var dbMs2 = scopeMs2.ServiceProvider.GetRequiredService<JobDbContext>();
            var ms2 = dbMs2.MarkdownSystems.FirstOrDefault();
            if (ms2 == null)
            {
                ms2 = new DocflowAi.Net.Api.MarkdownSystem.Models.MarkdownSystemDocument { Id = Guid.NewGuid(), Name = "d", Provider = "docling", Endpoint = "http://localhost", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
                dbMs2.MarkdownSystems.Add(ms2);
                dbMs2.SaveChanges();
            }
            msId2 = ms2.Id;
        }
        var resp = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = Convert.ToBase64String(bytes), fileName = "a.exe", model = "m", templateToken = "t", language = "eng", markdownSystemId = msId2 });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Directory.GetDirectories(factory.DataRootPath).Should().BeEmpty();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        store.CountPending().Should().Be(0);
    }

    [Fact]
    public async Task Missing_language_returns_400()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var bytes = new byte[10];
        Guid msId3;
        using (var scopeMs3 = factory.Services.CreateScope())
        {
            var dbMs3 = scopeMs3.ServiceProvider.GetRequiredService<JobDbContext>();
            var ms3 = dbMs3.MarkdownSystems.FirstOrDefault();
            if (ms3 == null)
            {
                ms3 = new DocflowAi.Net.Api.MarkdownSystem.Models.MarkdownSystemDocument { Id = Guid.NewGuid(), Name = "d", Provider = "docling", Endpoint = "http://localhost", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
                dbMs3.MarkdownSystems.Add(ms3);
                dbMs3.SaveChanges();
            }
            msId3 = ms3.Id;
        }
        var resp = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = Convert.ToBase64String(bytes), fileName = "a.pdf", model = "m", templateToken = "t", markdownSystemId = msId3 });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invalid_language_returns_400()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var bytes = new byte[10];
        Guid msId4;
        using (var scopeMs4 = factory.Services.CreateScope())
        {
            var dbMs4 = scopeMs4.ServiceProvider.GetRequiredService<JobDbContext>();
            var ms4 = dbMs4.MarkdownSystems.FirstOrDefault();
            if (ms4 == null)
            {
                ms4 = new DocflowAi.Net.Api.MarkdownSystem.Models.MarkdownSystemDocument { Id = Guid.NewGuid(), Name = "d", Provider = "docling", Endpoint = "http://localhost", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
                dbMs4.MarkdownSystems.Add(ms4);
                dbMs4.SaveChanges();
            }
            msId4 = ms4.Id;
        }
        var resp = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = Convert.ToBase64String(bytes), fileName = "a.pdf", model = "m", templateToken = "t", language = "fra", markdownSystemId = msId4 });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
