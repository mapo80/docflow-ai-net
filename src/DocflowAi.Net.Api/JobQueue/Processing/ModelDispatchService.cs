using System.Net.Http.Headers;
using System.Text;
using DocflowAi.Net.Api.Model.Abstractions;
using DocflowAi.Net.Api.Model.Models;
using DocflowAi.Net.Application.Abstractions;
using Serilog;

namespace DocflowAi.Net.Api.JobQueue.Processing;

/// <summary>
/// Resolves the model definition based on the provided token and invokes the
/// corresponding backend: local models, OpenAI compatible APIs or Azure OpenAI.
/// </summary>
public class ModelDispatchService : IModelDispatchService
{
    private readonly IModelRepository _repo;
    private readonly HttpClient _httpClient;
    private readonly ISecretProtector _protector;
    private readonly Serilog.ILogger _logger = Log.ForContext<ModelDispatchService>();
    private const string AzureApiVersion = "2024-02-01";

    public ModelDispatchService(IModelRepository repo, HttpClient httpClient, ISecretProtector protector)
    {
        _repo = repo;
        _httpClient = httpClient;
        _protector = protector;
    }

    public async Task<string> InvokeAsync(string modelToken, string payload, CancellationToken ct)
    {
        var model = _repo.GetByName(modelToken) ??
            throw new InvalidOperationException($"Model '{modelToken}' not found");

        return model.Type switch
        {
            "local" => HandleLocal(model, payload),
            "hosted-llm" => await HandleHostedAsync(model, payload, ct),
            _ => throw new NotSupportedException($"Unknown model type '{model.Type}'")
        };
    }

    private string HandleLocal(ModelDocument model, string payload)
    {
        _logger.Information("LocalModelInvoked {Name}", model.Name);
        return payload; // local execution pipeline will be implemented separately
    }

    private async Task<string> HandleHostedAsync(ModelDocument model, string payload, CancellationToken ct)
    {
        if (model.Provider == "openai")
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{model.BaseUrl}/v1/chat/completions")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrEmpty(model.ApiKeyEncrypted))
            {
                var apiKey = _protector.Unprotect(model.ApiKeyEncrypted);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
        if (model.Provider == "azure")
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{model.BaseUrl}/openai/deployments/{model.Name}/chat/completions?api-version={AzureApiVersion}")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrEmpty(model.ApiKeyEncrypted))
            {
                var apiKey = _protector.Unprotect(model.ApiKeyEncrypted);
                request.Headers.Add("api-key", apiKey);
            }
            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
        throw new NotSupportedException($"Unknown hosted provider '{model.Provider}'");
    }
}
