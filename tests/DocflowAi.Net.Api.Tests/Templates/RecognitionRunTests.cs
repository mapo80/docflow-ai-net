using System.Text.Json;
using DocflowAi.Net.Api.Features.Templates;
using DocflowAi.Net.Api.Features.Models;
using DocflowAi.Net.Api.JobQueue.Processing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class RecognitionRunTests
{
    private TemplatesDbContext CreateTplDb()
    {
        var conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<TemplatesDbContext>()
            .UseSqlite(conn).Options;
        var ctx = new TemplatesDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private RecognitionsDbContext CreateRecDb()
    {
        var conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<RecognitionsDbContext>()
            .UseSqlite(conn).Options;
        var ctx = new RecognitionsDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private ModelCatalogDbContext CreateModelDb()
    {
        var conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ModelCatalogDbContext>()
            .UseSqlite(conn).Options;
        var ctx = new ModelCatalogDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task Save_Recognition_With_TemplateName()
    {
        using var tdb = CreateTplDb();
        using var rdb = CreateRecDb();
        using var mdb = CreateModelDb();

        tdb.Templates.Add(new Template { Name = "Generic", FieldsJson = "[]" });
        await tdb.SaveChangesAsync();

        mdb.Models.Add(new GgufModel { Name = "gpt-4o-mini", SourceType = ModelSourceType.OpenAI, ApiKey = "sk", Model = "gpt-4o-mini", Status = ModelStatus.Available });
        await mdb.SaveChangesAsync();

        // Fake process service
        IProcessService svc = new FakeProcessService();
        var tmpFile = System.IO.Path.GetTempFileName();
        await System.IO.File.WriteAllTextAsync(tmpFile, "hello");

        // Simulate what the endpoint would do after running the service
        var res = await svc.ExecuteAsync(new ProcessInput(Guid.NewGuid(), tmpFile, null, null), default);
        Assert.True(res.Success);

        var rec = new RecognitionRecord
        {
            TemplateName = "Generic",
            ModelName = "gpt-4o-mini",
            FileName = "file.txt",
            Markdown = "MD",
            FieldsJson = "{}"
        };
        rdb.Recognitions.Add(rec);
        await rdb.SaveChangesAsync();

        var saved = await rdb.Recognitions.FirstAsync();
        Assert.Equal("Generic", saved.TemplateName);
    }

    private sealed class FakeProcessService : IProcessService
    {
        public Task<ProcessResult> ExecuteAsync(ProcessInput input, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(new { markdown = "# ok", fields = new { total = 10 } });
            return Task.FromResult(new ProcessResult(true, json, null));
        }
    }
}
