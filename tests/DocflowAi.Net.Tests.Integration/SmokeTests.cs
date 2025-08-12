using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
namespace DocflowAi.Net.Tests.Integration;
public class SmokeTests : IClassFixture<WebAppFactory> {
    private readonly WebAppFactory _factory; public SmokeTests(WebAppFactory factory) => _factory = factory;
    [Fact] public async Task Health_ReturnsOk() { var client = _factory.CreateClient(); var res = await client.GetAsync("/health"); res.IsSuccessStatusCode.Should().BeTrue(); }
    [Fact] public async Task Swagger_RequiresNoAuth() { var client = _factory.CreateClient(); var res = await client.GetAsync("/swagger/v1/swagger.json"); res.IsSuccessStatusCode.Should().BeTrue(); }
}
public class WebAppFactory : WebApplicationFactory<Program> { protected override IHost CreateHost(IHostBuilder builder) { builder.UseEnvironment("Development"); return base.CreateHost(builder); } }
