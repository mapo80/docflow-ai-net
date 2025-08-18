using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DocflowAi.Net.Api.JobQueue.Processing;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DocflowAi.Net.Api.Tests;

public class ProcessServiceTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_Dispatcher_Result()
    {
        var dispatcher = Substitute.For<IModelDispatchService>();
        dispatcher.InvokeAsync("m", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("out");
        var svc = new ProcessService(dispatcher);
        var temp = Path.GetTempFileName();
        await File.WriteAllTextAsync(temp, "data");
        var input = new ProcessInput(Guid.NewGuid(), temp, "t", "m");

        var result = await svc.ExecuteAsync(input, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OutputJson.Should().Be("out");
    }

    [Fact]
    public async Task ExecuteAsync_Propagates_Cancellation()
    {
        var dispatcher = Substitute.For<IModelDispatchService>();
        dispatcher.InvokeAsync("m", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<string>(new OperationCanceledException()));
        var svc = new ProcessService(dispatcher);
        var temp = Path.GetTempFileName();
        var input = new ProcessInput(Guid.NewGuid(), temp, "t", "m");

        await Assert.ThrowsAsync<OperationCanceledException>(() => svc.ExecuteAsync(input, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_Returns_Failure_On_Exception()
    {
        var dispatcher = Substitute.For<IModelDispatchService>();
        dispatcher.InvokeAsync("m", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<string>(new InvalidOperationException("boom")));
        var svc = new ProcessService(dispatcher);
        var temp = Path.GetTempFileName();
        var input = new ProcessInput(Guid.NewGuid(), temp, "t", "m");

        var result = await svc.ExecuteAsync(input, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("boom");
    }
}
