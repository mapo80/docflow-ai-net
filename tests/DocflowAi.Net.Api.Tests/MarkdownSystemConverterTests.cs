using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DocflowAi.Net.Api.Markdown.Services;
using DocflowAi.Net.Api.MarkdownSystem.Abstractions;
using DocflowAi.Net.Api.MarkdownSystem.Models;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using NSubstitute;
using Xunit;

namespace DocflowAi.Net.Api.Tests;

public class MarkdownSystemConverterTests
{
    [Fact]
    public async Task ConvertPdfAsync_Uses_Matching_Provider()
    {
        var repo = Substitute.For<IMarkdownSystemRepository>();
        var protector = Substitute.For<ISecretProtector>();
        var provider = Substitute.For<IMarkdownSystemProvider>();
        var converter = Substitute.For<IMarkdownConverter>();
        var systemId = Guid.NewGuid();
        provider.Name.Returns("docling");
        repo.GetById(systemId).Returns(new MarkdownSystemDocument
        {
            Id = systemId,
            Provider = "docling",
            Endpoint = "http://x",
            ApiKeyEncrypted = "enc"
        });
        protector.Unprotect("enc").Returns("key");
        provider.Create("http://x", "key").Returns(converter);
        var svc = new MarkdownSystemConverter(repo, new[] { provider }, protector);
        var opts = new MarkdownOptions();
        await svc.ConvertPdfAsync(Stream.Null, opts, systemId);
        provider.Received(1).Create("http://x", "key");
        await converter.Received(1).ConvertPdfAsync(Stream.Null, opts, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConvertPdfAsync_UnknownProvider_Throws()
    {
        var repo = Substitute.For<IMarkdownSystemRepository>();
        repo.GetAll().Returns(new[] { new MarkdownSystemDocument { Provider = "x", Endpoint = "http://x" } });
        var protector = Substitute.For<ISecretProtector>();
        var svc = new MarkdownSystemConverter(repo, Array.Empty<IMarkdownSystemProvider>(), protector);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ConvertPdfAsync(Stream.Null, new MarkdownOptions()));
    }

    [Fact]
    public async Task ConvertImageAsync_NoSystems_Throws()
    {
        var repo = Substitute.For<IMarkdownSystemRepository>();
        repo.GetAll().Returns(Array.Empty<MarkdownSystemDocument>());
        var protector = Substitute.For<ISecretProtector>();
        var svc = new MarkdownSystemConverter(repo, Array.Empty<IMarkdownSystemProvider>(), protector);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ConvertImageAsync(Stream.Null, new MarkdownOptions()));
    }

    [Fact]
    public async Task ConvertImageAsync_Defaults_To_First_System()
    {
        var repo = Substitute.For<IMarkdownSystemRepository>();
        var protector = Substitute.For<ISecretProtector>();
        var provider = Substitute.For<IMarkdownSystemProvider>();
        var converter = Substitute.For<IMarkdownConverter>();
        provider.Name.Returns("azure-di");
        repo.GetAll().Returns(new[]
        {
            new MarkdownSystemDocument { Provider = "azure-di", Endpoint = "http://y" }
        });
        provider.Create("http://y", null).Returns(converter);
        var svc = new MarkdownSystemConverter(repo, new[] { provider }, protector);
        var opts = new MarkdownOptions();
        await svc.ConvertImageAsync(Stream.Null, opts);
        await converter.Received(1).ConvertImageAsync(Stream.Null, opts, null, Arg.Any<CancellationToken>());
    }
}
