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
    private sealed class TestService : ModelDispatchService
    {
        private readonly Queue<Func<Task<string>>> _openAi;
        private readonly Queue<Func<Task<string>>> _azure;
        public int OpenAiCalls { get; private set; }
        public int AzureCalls { get; private set; }

        public TestService(IModelRepository repo, ISecretProtector protector,
            IEnumerable<Func<Task<string>>> openAiBehaviors,
            IEnumerable<Func<Task<string>>> azureBehaviors)
            : base(repo, protector)
        {
            _openAi = new Queue<Func<Task<string>>>(openAiBehaviors);
            _azure = new Queue<Func<Task<string>>>(azureBehaviors);
        }

        protected override Task<string> InvokeOpenAiAsync(ModelDocument model, string payload, CancellationToken ct)
        {
            OpenAiCalls++;
            return _openAi.Dequeue()();
        }

        protected override Task<string> InvokeAzureAsync(ModelDocument model, string payload, CancellationToken ct)
        {
            AzureCalls++;
            return _azure.Dequeue()();
        }
    }

    [Fact]
    public async Task LocalModel_ReturnsPayload()
    {
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("local").Returns(new ModelDocument { Name = "local", Type = "local" });
        var protector = Substitute.For<ISecretProtector>();
        var svc = new ModelDispatchService(repo, protector);

        var payload = "{\"ping\":true}";
        var result = await svc.InvokeAsync("local", payload, CancellationToken.None);

        result.Should().Be(payload);
    }

    [Fact]
    public async Task OpenAiModel_RetriesAndReturnsResult()
    {
        var model = new ModelDocument { Name = "gpt", Type = "hosted-llm", Provider = "openai" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("gpt").Returns(model);
        var protector = Substitute.For<ISecretProtector>();

        var behaviors = new List<Func<Task<string>>>
        {
            () => throw new Exception(),
            () => throw new Exception(),
            () => Task.FromResult("ok")
        };
        var svc = new TestService(repo, protector, behaviors, Array.Empty<Func<Task<string>>>());

        var result = await svc.InvokeAsync("gpt", "{}", CancellationToken.None);

        result.Should().Be("ok");
        svc.OpenAiCalls.Should().Be(3);
    }

    [Fact]
    public async Task AzureModel_RetriesAndReturnsResult()
    {
        var model = new ModelDocument { Name = "az", Type = "hosted-llm", Provider = "azure" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("az").Returns(model);
        var protector = Substitute.For<ISecretProtector>();

        var behaviors = new List<Func<Task<string>>>
        {
            () => throw new Exception(),
            () => Task.FromResult("ok")
        };
        var svc = new TestService(repo, protector, Array.Empty<Func<Task<string>>>(), behaviors);

        var result = await svc.InvokeAsync("az", "{}", CancellationToken.None);

        result.Should().Be("ok");
        svc.AzureCalls.Should().Be(2);
    }

    [Fact]
    public async Task UnknownProvider_Throws()
    {
        var model = new ModelDocument { Name = "bad", Type = "hosted-llm", Provider = "other" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("bad").Returns(model);
        var protector = Substitute.For<ISecretProtector>();
        var svc = new ModelDispatchService(repo, protector);

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
        var svc = new ModelDispatchService(repo, protector);

        var act = () => svc.InvokeAsync("mystery", "{}", CancellationToken.None);
        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task MissingModel_Throws()
    {
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("missing").Returns((ModelDocument?)null);
        var protector = Substitute.For<ISecretProtector>();
        var svc = new ModelDispatchService(repo, protector);

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
        var svc = new ModelDispatchService(repo, protector);

        var act = () => svc.InvokeAsync("noUrl", "{}", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
