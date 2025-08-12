using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DocflowAi.Net.Application.Configuration;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Infrastructure.Llm;
using DocflowAi.Net.Infrastructure.Markitdown;
using DocflowAi.Net.Infrastructure.Orchestration;
using DocflowAi.Net.Infrastructure.Reasoning;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace DocflowAi.Net.Tests.Integration;

public class DatasetProcessingTests : IClassFixture<MarkitdownServiceFixture>, IDisposable
{
    private readonly ProcessingOrchestrator _orchestrator;
    private readonly LlamaExtractor _llama;
    private readonly string _root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private readonly ITestOutputHelper _output;
    private readonly string _prompt;
    private readonly List<FieldSpec> _specs;

    public DatasetProcessingTests(MarkitdownServiceFixture fx, ITestOutputHelper output)
    {
        _output = output;

        _prompt = File.ReadAllText(Path.Combine(_root, "dataset/prompt.txt"));

        _specs = File.ReadLines(Path.Combine(_root, "dataset/valori_campi.txt"))
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0])
            .Select(k => new FieldSpec { Key = k })
            .ToList();

        var modelPath = Environment.GetEnvironmentVariable("LLM__ModelPath")
            ?? Path.Combine(_root, "models/SmolLM-135M-Instruct.Q4_K_S.gguf");
        var llmOpts = Options.Create(new LlmOptions { ModelPath = modelPath });
        _llama = new LlamaExtractor(llmOpts, new ReasoningModeAccessor(), NullLogger<LlamaExtractor>.Instance);

        var http = new HttpClient();
        var servicesOpts = Options.Create(new ServicesOptions
        {
            Markitdown = new MarkitdownOptions { BaseUrl = fx.BaseUrl, TimeoutSeconds = 30 }
        });
        var mdk = new MarkitdownClient(http, servicesOpts, NullLogger<MarkitdownClient>.Instance);
        _orchestrator = new ProcessingOrchestrator(mdk, _llama, NullLogger<ProcessingOrchestrator>.Instance);
    }

    [Fact]
    public async Task Png_processing_raises_exception()
    {
        var path = Path.Combine(_root, "dataset/sample_invoice.png");
        await using var fs = File.OpenRead(path);
        var form = new FormFile(fs, 0, fs.Length, "file", Path.GetFileName(path))
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        await Assert.ThrowsAnyAsync<Exception>(() => _orchestrator.ProcessAsync(form, "default", _prompt, _specs, default));
    }

    [Fact]
    public async Task Dataset_files_are_processed_and_reported()
    {
        var datasetDir = Path.Combine(_root, "dataset");
        int processed = 0;

        foreach (var path in Directory.EnumerateFiles(datasetDir).Where(p => p.EndsWith(".pdf") || p.EndsWith(".png")))
        {
            await using var fs = File.OpenRead(path);
            var form = new FormFile(fs, 0, fs.Length, "file", Path.GetFileName(path))
            {
                Headers = new HeaderDictionary(),
                ContentType = path.EndsWith(".pdf") ? "application/pdf" : "image/png",
            };

            try
            {
                var result = await _orchestrator.ProcessAsync(form, "default", _prompt, _specs, default);
                var json = JsonSerializer.Serialize(result);
                _output.WriteLine($"{Path.GetFileName(path)} => {json}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"{Path.GetFileName(path)} => ERROR {ex}");
            }
            processed++;
        }

        processed.Should().BeGreaterThan(0);
    }

    public void Dispose() => _llama.Dispose();
}

