using System.Text.Json;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.Templates.Abstractions;
using DocflowAi.Net.Api.Templates.Models;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.JobQueue.Services;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.BBoxResolver;
using DocflowAi.Net.Domain.Extraction;
using DocflowAi.Net.Api.Options;
using FluentAssertions;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocflowAi.Net.Api.Tests;

public class ProcessServiceTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ProcessServiceTests(TempDirFixture fx) => _fx = fx;
    [Fact]
    public async Task Returns_error_when_template_missing()
    {
        var svc = new ProcessService(new StubRepo(null), new StubConverter(), new StubLlama(), new StubResolver(), new StubFs(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var res = await svc.ExecuteAsync(new ProcessInput(Guid.NewGuid(), "nofile", Path.GetTempFileName(), Path.GetTempFileName(), "missing", "m", Guid.NewGuid()), CancellationToken.None);
        res.Success.Should().BeFalse();
        res.ErrorMessage.Should().Be("template not found");
    }

    [Fact]
    public async Task Produces_output_and_metrics()
    {
        var tpl = new TemplateDocument { Token = "tok", Name = "tpl", FieldsJson = "[{\"Key\":\"f\",\"Type\":\"string\"}]", PromptMarkdown = "p" };
        var repo = new StubRepo(tpl);
        var fs = new StubFs();
        var svc = new ProcessService(repo, new StubConverter(), new StubLlama(), new StubResolver(), fs, Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, "data");
        var res = await svc.ExecuteAsync(new ProcessInput(Guid.NewGuid(), tmp, Path.Combine(Path.GetTempPath(), "md.md"), Path.Combine(Path.GetTempPath(), "pr.md"), "tok", "m", Guid.NewGuid()), CancellationToken.None);
        res.Success.Should().BeTrue();
        var json = JsonDocument.Parse(res.OutputJson);
        json.RootElement.GetProperty("fields")[0].GetProperty("key").GetString().Should().Be("f");
        json.RootElement.GetProperty("fields")[0].GetProperty("spans").ValueKind.Should().Be(JsonValueKind.Array);
        json.RootElement.GetProperty("metrics").GetProperty("total_ms").GetDouble().Should().BeGreaterThan(0);
        res.Markdown.Should().Be("md");
        fs.Files["pr.md"].Should().Contain("[SYSTEM]");
        fs.Files["pr.md"].Should().Contain("[USER]");
    }

    [Fact]
    public async Task Reuses_existing_markdown_on_retry()
    {
        var tpl = new TemplateDocument { Token = "tok", Name = "tpl", FieldsJson = "[]" };
        var repo = new StubRepo(tpl);
        var converter = new CountingConverter();
        var llama = new StubLlama();
        var resolver = new StubResolver();
        var fsSvc = new FileSystemService(Microsoft.Extensions.Options.Options.Create(new JobQueueOptions { DataRoot = _fx.RootPath }), NullLogger<FileSystemService>.Instance);
        var jobId = Guid.NewGuid();
        var dir = fsSvc.CreateJobDirectory(jobId);
        var inputPath = Path.Combine(dir, "input.pdf");
        await File.WriteAllTextAsync(inputPath, "data");
        var markdownPath = Path.Combine(dir, "markdown.md");
        var promptPath = Path.Combine(dir, "prompt.md");
        var svc = new ProcessService(repo, converter, llama, resolver, fsSvc, Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var input = new ProcessInput(jobId, inputPath, markdownPath, promptPath, "tok", "m", Guid.NewGuid());

        await svc.ExecuteAsync(input, CancellationToken.None);
        await svc.ExecuteAsync(input, CancellationToken.None);

        converter.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Reprocesses_markdown_when_existing_file_empty()
    {
        var tpl = new TemplateDocument { Token = "tok", Name = "tpl", FieldsJson = "[]" };
        var repo = new StubRepo(tpl);
        var converter = new CountingConverter();
        var llama = new StubLlama();
        var resolver = new StubResolver();
        var fsSvc = new FileSystemService(Microsoft.Extensions.Options.Options.Create(new JobQueueOptions { DataRoot = _fx.RootPath }), NullLogger<FileSystemService>.Instance);
        var jobId = Guid.NewGuid();
        var dir = fsSvc.CreateJobDirectory(jobId);
        var inputPath = Path.Combine(dir, "input.pdf");
        await File.WriteAllTextAsync(inputPath, "data");
        var markdownPath = Path.Combine(dir, "markdown.md");
        var promptPath = Path.Combine(dir, "prompt.md");
        var svc = new ProcessService(repo, converter, llama, resolver, fsSvc, Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var input = new ProcessInput(jobId, inputPath, markdownPath, promptPath, "tok", "m", Guid.NewGuid());

        await svc.ExecuteAsync(input, CancellationToken.None);
        File.WriteAllText(markdownPath, string.Empty);
        await svc.ExecuteAsync(input, CancellationToken.None);

        converter.Calls.Should().Be(2);
    }

    [Fact]
    public async Task Returns_markdown_created_at_on_llm_failure()
    {
        var tpl = new TemplateDocument { Token = "tok", Name = "tpl", FieldsJson = "[]" };
        var repo = new StubRepo(tpl);
        var fs = new StubFs();
        var svc = new ProcessService(repo, new StubConverter(), new ThrowingLlama(), new StubResolver(), fs, Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, "data");
        var res = await svc.ExecuteAsync(new ProcessInput(Guid.NewGuid(), tmp, Path.Combine(Path.GetTempPath(), "md.md"), Path.Combine(Path.GetTempPath(), "pr.md"), "tok", "m", Guid.NewGuid()), CancellationToken.None);
        res.Success.Should().BeFalse();
        res.MarkdownCreatedAt.Should().NotBeNull();
    }

    private sealed class StubRepo : ITemplateRepository
    {
        private readonly TemplateDocument? _doc;
        public StubRepo(TemplateDocument? doc) => _doc = doc;
        public TemplateDocument? GetByToken(string token) => _doc;
        public TemplateDocument? GetById(Guid id) => _doc;
        public (IReadOnlyList<TemplateDocument> items, int total) GetPaged(string? q, int page, int pageSize, string? sort) => (new List<TemplateDocument>(),0);
        public bool ExistsByName(string name, Guid? excludeId = null) => false;
        public bool ExistsByToken(string token, Guid? excludeId = null) => false;
        public void Add(TemplateDocument template){}
        public void Update(TemplateDocument template){}
        public void Delete(Guid id){}
        public void SaveChanges(){}
    }

    private static MarkdownResult Md()
        => new("md", new[] { new PageInfo(1,100,100) }, new[] { new DocflowAi.Net.Application.Markdown.Box(1,0,0,1,1,0,0,1,1,"hi") }, "{}");

    private sealed class StubConverter : IMarkdownConverter
    {
        public Task<MarkdownResult> ConvertPdfAsync(Stream pdf, MarkdownOptions opts, Guid? systemId = null, CancellationToken ct = default)
            => Task.FromResult(Md());
        public Task<MarkdownResult> ConvertImageAsync(Stream image, MarkdownOptions opts, Guid? systemId = null, CancellationToken ct = default)
            => Task.FromResult(Md());
    }

    private sealed class CountingConverter : IMarkdownConverter
    {
        public int Calls { get; private set; }
        public Task<MarkdownResult> ConvertPdfAsync(Stream pdf, MarkdownOptions opts, Guid? systemId = null, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(Md());
        }
        public Task<MarkdownResult> ConvertImageAsync(Stream image, MarkdownOptions opts, Guid? systemId = null, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(Md());
        }
    }

    private sealed class StubLlama : ILlamaExtractor
    {
        public async Task<LlamaExtractionResult> ExtractAsync(
            string markdown,
            string templateName,
            string prompt,
            IReadOnlyList<FieldSpec> fieldsSpec,
            CancellationToken ct,
            Func<string, string, Task>? onBeforeSend = null)
        {
            if (onBeforeSend != null)
                await onBeforeSend("sys", "user");
            return new LlamaExtractionResult(
                new DocumentAnalysisResult(templateName, new List<ExtractedField> { new("f", "v", 1, null, null) }, "it", null),
                "sys",
                "user");
        }
        public void Dispose() {}
    }

    private sealed class ThrowingLlama : ILlamaExtractor
    {
        public Task<LlamaExtractionResult> ExtractAsync(string markdown, string templateName, string prompt, IReadOnlyList<FieldSpec> fieldsSpec, CancellationToken ct, Func<string, string, Task>? onBeforeSend = null)
            => throw new Exception("fail");
        public void Dispose() {}
    }

    private sealed class StubResolver : IResolverOrchestrator
    {
        public Task<IReadOnlyList<BBoxResolveResult>> ResolveAsync(DocumentIndex index, IReadOnlyList<ExtractedField> fields, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BBoxResolveResult>>(fields.Select(f => new BBoxResolveResult(f.Key, f.Value, f.Confidence, f.Evidence ?? new List<SpanEvidence>(), f.Pointer)).ToList());
    }

    private sealed class StubFs : IFileSystemService
    {
        public Dictionary<string, string> Files { get; } = new();
        public void EnsureDirectory(string path) { }
        public string CreateJobDirectory(Guid jobId) => string.Empty;
        public Task<string> SaveInputAtomic(Guid jobId, IFormFile file, CancellationToken ct = default) => Task.FromResult(string.Empty);
        public Task<string> SaveTextAtomic(Guid jobId, string filename, string content, CancellationToken ct = default)
        {
            Files[filename] = content;
            return Task.FromResult(string.Empty);
        }
    }
}
