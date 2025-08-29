using System;
using System.ClientModel;
using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using DocflowAi.Net.Application.Abstractions;

namespace DocflowAi.Net.Infrastructure.Llm.Providers;

/// <summary>Hosted model provider using Azure OpenAI.</summary>
public sealed class AzureOpenAiModelProvider : IHostedModelProvider
{
    public string Name => "azure";

    public async Task<string> InvokeAsync(string model, string endpoint, string? apiKey, string payload, CancellationToken ct)
    {
        var credential = new ApiKeyCredential(apiKey ?? string.Empty);
        var client = new AzureOpenAIClient(new Uri(endpoint), credential);
        var chat = client.GetChatClient(model);
        var messages = new[] { new UserChatMessage(payload) };
        var completion = await chat.CompleteChatAsync(messages, cancellationToken: ct);
        return JsonSerializer.Serialize(completion);
    }
}
