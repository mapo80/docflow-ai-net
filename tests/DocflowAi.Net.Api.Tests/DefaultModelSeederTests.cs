using System.Collections.Generic;
using System.IO;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Features.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocflowAi.Net.Api.Tests;

public class DefaultModelSeederTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fixture;

    public DefaultModelSeederTests(TempDirFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Seeds_Default_Model()
    {
        var extra = new Dictionary<string, string?>
        {
            ["ModelCatalog:SeedDefaults"] = "true",
            ["ModelStorage:Root"] = Path.Combine(_fixture.RootPath, "models")
        };
        using var factory = new TestWebAppFactory(_fixture.RootPath, extra: extra);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ModelCatalogDbContext>();

        var model = await db.Models.AsNoTracking().FirstOrDefaultAsync();
        model.Should().NotBeNull();
        model!.HfRepo.Should().Be("unsloth/Qwen3-0.6B-GGUF");
        model.HfFilename.Should().Be("Qwen3-0.6B-Q4_0.gguf");
    }
}
