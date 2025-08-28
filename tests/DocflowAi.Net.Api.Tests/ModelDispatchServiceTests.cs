using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.Model.Abstractions;
using DocflowAi.Net.Api.Model.Models;
using DocflowAi.Net.Application.Abstractions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DocflowAi.Net.Api.Tests;

public class ModelDispatchServiceTests
{
    private sealed class StubProvider : IHostedModelProvider
    {
        private readonly Queue<Func<Task<string>>> _behaviors;
        public int Calls { get; private set; }

        public StubProvider(string name, IEnumerable<Func<Task<string>>> behaviors)
        {
            Name = name;
            _behaviors = new Queue<Func<Task<string>>>(behaviors);
        }

        public string Name { get; }

        public Task<string> InvokeAsync(string model, string endpoint, string? apiKey, string payload, CancellationToken ct)
        {
            Calls++;
            return _behaviors.Dequeue()();
        }
    }

    [Fact]
    public async Task LocalModel_ReturnsPayload()
    {
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("local").Returns(new ModelDocument { Name = "local", Type = "local" });
        var protector = Substitute.For<ISecretProtector>();
        var svc = new ModelDispatchService(repo, protector, Array.Empty<IHostedModelProvider>());

        var payload = "{\"ping\":true}";
        var result = await svc.InvokeAsync("local", payload, CancellationToken.None);

        result.Should().Be(payload);
    }

    [Fact]
    public async Task OpenAiModel_RetriesAndReturnsResult()
    {
        var model = new ModelDocument { Name = "gpt", Type = "hosted-llm", Provider = "openai", BaseUrl = "http://x" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("gpt").Returns(model);
        var protector = Substitute.For<ISecretProtector>();

        var behaviors = new List<Func<Task<string>>>
        {
            () => throw new Exception(),
            () => throw new Exception(),
            () => Task.FromResult("ok")
        };
        var provider = new StubProvider("openai", behaviors);
        var svc = new ModelDispatchService(repo, protector, new[] { provider });

        var result = await svc.InvokeAsync("gpt", "{}", CancellationToken.None);

        result.Should().Be("ok");
        provider.Calls.Should().Be(3);
    }

    [Fact]
    public async Task AzureModel_RetriesAndReturnsResult()
    {
        var model = new ModelDocument { Name = "az", Type = "hosted-llm", Provider = "azure", BaseUrl = "http://y" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("az").Returns(model);
        var protector = Substitute.For<ISecretProtector>();

        var behaviors = new List<Func<Task<string>>>
        {
            () => throw new Exception(),
            () => Task.FromResult("ok")
        };
        var provider = new StubProvider("azure", behaviors);
        var svc = new ModelDispatchService(repo, protector, new[] { provider });

        var result = await svc.InvokeAsync("az", "{}", CancellationToken.None);

        result.Should().Be("ok");
        provider.Calls.Should().Be(2);
    }

    [Fact]
    public async Task UnknownProvider_Throws()
    {
        var model = new ModelDocument { Name = "bad", Type = "hosted-llm", Provider = "other" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("bad").Returns(model);
        var protector = Substitute.For<ISecretProtector>();
        var svc = new ModelDispatchService(repo, protector, Array.Empty<IHostedModelProvider>());

        var act = () => svc.InvokeAsync("bad", "{}", CancellationToken.None);
        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task UnknownType_Throws()
    {
        var model = new ModelDocument { Name = "mystery", Type = "strange" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("mystery").Returns(model);
        var protector = Substitute.For<ISecretProtector>();
        var svc = new ModelDispatchService(repo, protector, Array.Empty<IHostedModelProvider>());

        var act = () => svc.InvokeAsync("mystery", "{}", CancellationToken.None);
        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task MissingModel_Throws()
    {
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("missing").Returns((ModelDocument?)null);
        var protector = Substitute.For<ISecretProtector>();
        var svc = new ModelDispatchService(repo, protector, Array.Empty<IHostedModelProvider>());

        var act = () => svc.InvokeAsync("missing", "{}", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HostedModel_Without_BaseUrl_Throws()
    {
        var model = new ModelDocument { Name = "noUrl", Type = "hosted-llm", Provider = "openai", BaseUrl = null, ApiKeyEncrypted = "enc" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("noUrl").Returns(model);
        var protector = Substitute.For<ISecretProtector>();
        protector.Unprotect("enc").Returns("key");
        var provider = new StubProvider("openai", Array.Empty<Func<Task<string>>>());
        var svc = new ModelDispatchService(repo, protector, new[] { provider });

        var act = () => svc.InvokeAsync("noUrl", "{}", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
