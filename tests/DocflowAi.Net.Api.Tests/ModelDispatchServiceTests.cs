using System.Net;
using System.Net.Http;
using System.Linq;
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
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public HttpRequestMessage? Request { get; private set; }
        public FakeHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(_response);
        }
    }

    [Fact]
    public async Task LocalModel_ReturnsPayload()
    {
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("local").Returns(new ModelDocument { Name = "local", Type = "local" });
        var protector = Substitute.For<ISecretProtector>();
        var handler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var svc = new ModelDispatchService(repo, client, protector);

        var payload = "{\"ping\":true}";
        var result = await svc.InvokeAsync("local", payload, CancellationToken.None);

        result.Should().Be(payload);
        handler.Request.Should().BeNull();
    }

    [Fact]
    public async Task OpenAiModel_CallsExpectedEndpoint()
    {
        var model = new ModelDocument { Name = "gpt", Type = "hosted-llm", Provider = "openai", BaseUrl = "https://api.example", ApiKeyEncrypted = "enc" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("gpt").Returns(model);
        var protector = Substitute.For<ISecretProtector>();
        protector.Unprotect("enc").Returns("secret");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}") };
        var handler = new FakeHandler(response);
        var client = new HttpClient(handler);
        var svc = new ModelDispatchService(repo, client, protector);

        var result = await svc.InvokeAsync("gpt", "{}", CancellationToken.None);

        result.Should().Be("{\"ok\":true}");
        handler.Request!.RequestUri.Should().Be(new Uri("https://api.example/v1/chat/completions"));
        handler.Request.Headers.Authorization!.Parameter.Should().Be("secret");
        handler.Request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task AzureModel_CallsExpectedEndpoint()
    {
        var model = new ModelDocument { Name = "azureModel", Type = "hosted-llm", Provider = "azure", BaseUrl = "https://azure.example", ApiKeyEncrypted = "enc" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("azureModel").Returns(model);
        var protector = Substitute.For<ISecretProtector>();
        protector.Unprotect("enc").Returns("secret");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}") };
        var handler = new FakeHandler(response);
        var client = new HttpClient(handler);
        var svc = new ModelDispatchService(repo, client, protector);

        var result = await svc.InvokeAsync("azureModel", "{}", CancellationToken.None);

        result.Should().Be("{\"ok\":true}");
        handler.Request!.RequestUri.Should().Be(new Uri("https://azure.example/openai/deployments/azureModel/chat/completions?api-version=2024-02-01"));
        handler.Request.Headers.GetValues("api-key").Single().Should().Be("secret");
    }

    [Fact]
    public async Task UnknownProvider_Throws()
    {
        var model = new ModelDocument { Name = "bad", Type = "hosted-llm", Provider = "other" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("bad").Returns(model);
        var protector = Substitute.For<ISecretProtector>();
        var handler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var svc = new ModelDispatchService(repo, new HttpClient(handler), protector);

        var act = () => svc.InvokeAsync("bad", "{}", CancellationToken.None);
        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task UnknownType_Throws()
    {
        var model = new ModelDocument { Name = "bad", Type = "mystery" };
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("bad").Returns(model);
        var protector = Substitute.For<ISecretProtector>();
        var handler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var svc = new ModelDispatchService(repo, new HttpClient(handler), protector);

        var act = () => svc.InvokeAsync("bad", "{}", CancellationToken.None);
        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task MissingModel_Throws()
    {
        var repo = Substitute.For<IModelRepository>();
        repo.GetByName("missing").Returns((ModelDocument?)null);
        var protector = Substitute.For<ISecretProtector>();
        var handler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var svc = new ModelDispatchService(repo, new HttpClient(handler), protector);

        var act = () => svc.InvokeAsync("missing", "{}", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
