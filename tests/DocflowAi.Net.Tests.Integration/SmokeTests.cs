using FluentAssertions;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace DocflowAi.Net.Tests.Integration;

public class SmokeTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public SmokeTests(WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");
        res.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Swagger_RequiresNoAuth()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/swagger/v1/swagger.json");
        res.IsSuccessStatusCode.Should().BeTrue();
    }
}

public class WebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<BackgroundJobServerHostedService>();
        });
    }
}
