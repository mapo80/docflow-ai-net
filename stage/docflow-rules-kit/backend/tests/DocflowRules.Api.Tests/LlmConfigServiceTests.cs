using System;
using System.Threading;
using System.Threading.Tasks;
using DocflowRules.Api.Services;
using DocflowRules.Storage.EF;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class LlmConfigServiceTests
{
    private AppDbContext NewDb()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opt);
    }

    [Fact]
    public async Task Crud_and_activate_model()
    {
        await using var db = NewDb();
        var svc = new LlmConfigService(db);

        // Create
        var m = await svc.CreateAsync(new LlmModel { Name="local", Provider="LlamaSharp", ModelPathOrId="/models/a.gguf", Enabled=true }, CancellationToken.None);
        m.Id.Should().NotBe(Guid.Empty);

        // List
        var list = await svc.ListAsync(CancellationToken.None);
        list.Should().HaveCount(1);

        // Update
        m.Enabled = false; m.Name = "local-2";
        var upd = await svc.UpdateAsync(m, CancellationToken.None);
        upd.Enabled.Should().BeFalse();
        upd.Name.Should().Be("local-2");

        // Activate & turbo
        await svc.SetActiveAsync(m.Id, turbo: true, CancellationToken.None);
        var (active, turbo) = await svc.GetActiveWithTurboAsync(CancellationToken.None);
        active!.Id.Should().Be(m.Id);
        turbo.Should().BeTrue();

        // Delete
        await svc.DeleteAsync(m.Id, CancellationToken.None);
        (await svc.ListAsync(CancellationToken.None)).Should().BeEmpty();
    }
}
