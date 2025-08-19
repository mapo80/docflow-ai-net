using System.Text.Json;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.Templates.Abstractions;
using DocflowAi.Net.Api.Templates.Models;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.BBoxResolver;
using DocflowAi.Net.Domain.Extraction;
using FluentAssertions;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Options;

namespace DocflowAi.Net.Api.Tests;

public class ProcessServiceTests
{
    [Fact]
    public async Task Returns_error_when_template_missing()
    {
        var svc = new ProcessService(new StubRepo(null), new StubConverter(), new StubLlama(), new StubResolver(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var res = await svc.ExecuteAsync(new ProcessInput(Guid.NewGuid(), "nofile", "missing", "m"), CancellationToken.None);
        res.Success.Should().BeFalse();
        res.ErrorMessage.Should().Be("template not found");
    }

    [Fact]
    public async Task Produces_output_and_metrics()
    {
        var tpl = new TemplateDocument { Token = "tok", Name = "tpl", FieldsJson = "[{\"Key\":\"f\",\"Type\":\"string\"}]", PromptMarkdown = "p" };
        var repo = new StubRepo(tpl);
        var svc = new ProcessService(repo, new StubConverter(), new StubLlama(), new StubResolver(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, "data");
        var res = await svc.ExecuteAsync(new ProcessInput(Guid.NewGuid(), tmp, "tok", "m"), CancellationToken.None);
        res.Success.Should().BeTrue();
        var json = JsonDocument.Parse(res.OutputJson);
        json.RootElement.GetProperty("fields")[0].GetProperty("key").GetString().Should().Be("f");
        json.RootElement.GetProperty("metrics").GetProperty("total_ms").GetDouble().Should().BeGreaterThan(0);
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

    private sealed class StubConverter : IMarkdownConverter
    {
        public Task<MarkdownResult> ConvertPdfAsync(Stream pdf, MarkdownOptions opts, CancellationToken ct = default)
            => Task.FromResult(Md());
        public Task<MarkdownResult> ConvertImageAsync(Stream image, MarkdownOptions opts, CancellationToken ct = default)
            => Task.FromResult(Md());
        private static MarkdownResult Md()
            => new("md", new[] { new PageInfo(1,100,100) }, new[] { new DocflowAi.Net.Application.Markdown.Box(1,0,0,1,1,0,0,1,1,"hi") });
    }

    private sealed class StubLlama : ILlamaExtractor
    {
        public Task<DocumentAnalysisResult> ExtractAsync(string markdown, string templateName, string prompt, IReadOnlyList<FieldSpec> fieldsSpec, CancellationToken ct)
            => Task.FromResult(new DocumentAnalysisResult(templateName, new List<ExtractedField> { new("f","v",1,null,null) }, "it", null));
        public void Dispose() {}
    }

    private sealed class StubResolver : IResolverOrchestrator
    {
        public Task<IReadOnlyList<BBoxResolveResult>> ResolveAsync(DocumentIndex index, IReadOnlyList<ExtractedField> fields, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BBoxResolveResult>>(fields.Select(f => new BBoxResolveResult(f.Key, f.Value, f.Confidence, f.Evidence ?? new List<SpanEvidence>(), f.Pointer)).ToList());
    }
}
