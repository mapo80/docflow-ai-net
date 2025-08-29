using System;
using System.ClientModel;
using System.Text.Json;
using OpenAI;
using OpenAI.Chat;
using DocflowAi.Net.Application.Abstractions;

namespace DocflowAi.Net.Infrastructure.Llm.Providers;

/// <summary>Hosted model provider using the OpenAI API.</summary>
public sealed class OpenAiModelProvider : IHostedModelProvider
{
    public string Name => "openai";

    public async Task<string> InvokeAsync(string model, string endpoint, string? apiKey, string payload, CancellationToken ct)
    {
        var credential = new ApiKeyCredential(apiKey ?? string.Empty);
        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
        var client = new ChatClient(model, credential, options);
        var messages = new[] { new UserChatMessage(payload) };
        var completion = await client.CompleteChatAsync(messages, cancellationToken: ct);
        return JsonSerializer.Serialize(completion);
    }
}
