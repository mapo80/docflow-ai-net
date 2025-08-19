using DocflowAi.Net.Api.Tests.Fixtures;
using FluentAssertions;
using Hangfire.Console.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DocflowAi.Net.Api.Tests;

public class HangfireConsoleExtensionsTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public HangfireConsoleExtensionsTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public void Registers_Hangfire_Console_Services()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var manager = factory.Services.GetService<IJobManager>();
        manager.Should().NotBeNull();
    }
}
