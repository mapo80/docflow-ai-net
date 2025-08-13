using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocflowAi.Net.Infrastructure.Llm;

public sealed class LlmModelService : ILlmModelService
{
    private readonly HttpClient _httpClient;
    private readonly LlmOptions _options;
    private readonly ILogger<LlmModelService> _logger;

    public LlmModelService(HttpClient httpClient, IOptions<LlmOptions> options, ILogger<LlmModelService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SwitchModelAsync(string hfKey, string modelRepo, string modelFile, int contextSize, CancellationToken ct)
    {
        var modelsDir = Environment.GetEnvironmentVariable("MODELS_DIR") ?? Path.Combine(AppContext.BaseDirectory, "models");
        Directory.CreateDirectory(modelsDir);
        var dest = Path.Combine(modelsDir, modelFile);
        if (!File.Exists(dest))
        {
            var url = $"https://huggingface.co/{modelRepo}/resolve/main/{modelFile}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", hfKey);
            using var resp = await _httpClient.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                    throw new FileNotFoundException($"Model {modelRepo}/{modelFile} not found on HuggingFace");
                resp.EnsureSuccessStatusCode();
            }
            await using var fs = File.Create(dest);
            await resp.Content.CopyToAsync(fs, ct);
        }
        _options.ModelPath = dest;
        _options.ContextTokens = contextSize;
        _logger.LogInformation("LLM model switched to {ModelPath}", dest);
    }
}
