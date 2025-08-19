using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Templates.Repositories;
using DocflowAi.Net.Api.Templates.Services;
using DocflowAi.Net.Application.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace DocflowAi.Net.Api.Tests;

public class TemplateServiceTests
{
    private static ITemplateService CreateService(JobDbContext db)
    {
        var repo = new TemplateRepository(db);
        return new TemplateService(repo);
    }

    [Fact]
    public void CreateTemplate_SetsTimestamps()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        var dto = service.Create(new CreateTemplateRequest(
            "t1",
            "tok1",
            null,
            JsonDocument.Parse("[]").RootElement));
        dto.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        dto.UpdatedAt.Should().BeCloseTo(dto.CreatedAt, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreateTemplate_DuplicateName_Throws()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        service.Create(new CreateTemplateRequest("t1", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        Action act = () => service.Create(new CreateTemplateRequest("t1", "tok2", null, JsonDocument.Parse("[]" ).RootElement));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateTemplate_InvalidSlug_Throws()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        Action act = () => service.Create(new CreateTemplateRequest("t1", "bad token", null, JsonDocument.Parse("[]" ).RootElement));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FieldsJsonMustBeArray()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        var obj = JsonDocument.Parse("{}").RootElement;
        Action act = () => service.Create(new CreateTemplateRequest("t1", "tok1", null, obj));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateTemplate_ChangesValues()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        var tpl = service.Create(new CreateTemplateRequest("t1", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        var updated = service.Update(tpl.Id, new UpdateTemplateRequest("t2", "tok2", "p", JsonDocument.Parse("[{\"name\":\"b\",\"type\":\"string\"}]").RootElement));
        updated.Name.Should().Be("t2");
        updated.Token.Should().Be("tok2");
        updated.FieldsJson.EnumerateArray().First().GetProperty("name").GetString().Should().Be("b");
        updated.UpdatedAt.Should().BeAfter(updated.CreatedAt);
    }

    [Fact]
    public void GetPaged_OrdersAndPaginates()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        service.Create(new CreateTemplateRequest("t1", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        System.Threading.Thread.Sleep(10);
        service.Create(new CreateTemplateRequest("t2", "tok2", null, JsonDocument.Parse("[]" ).RootElement));
        System.Threading.Thread.Sleep(10);
        service.Create(new CreateTemplateRequest("t3", "tok3", null, JsonDocument.Parse("[]" ).RootElement));
        var page1 = service.GetPaged(null, 1, 2, null);
        page1.Total.Should().Be(3);
        page1.Items.Should().HaveCount(2);
        page1.Items.First().Name.Should().Be("t3");
        var page2 = service.GetPaged(null, 2, 2, null);
        page2.Items.Should().ContainSingle();
        page2.Items.First().Name.Should().Be("t1");
    }

    [Fact]
    public void DeleteTemplate_RemovesEntry()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        var tpl = service.Create(new CreateTemplateRequest("t1", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        service.Delete(tpl.Id);
        service.GetById(tpl.Id).Should().BeNull();
    }

    [Fact]
    public void CreateTemplate_DuplicateToken_Throws()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        service.Create(new CreateTemplateRequest("t1", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        Action act = () => service.Create(new CreateTemplateRequest("t2", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateTemplate_InvalidName_Throws()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        Action act = () => service.Create(new CreateTemplateRequest("", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetById_ReturnsNull_WhenMissing()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        service.GetById(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void UpdateTemplate_DuplicateName_Throws()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        var t1 = service.Create(new CreateTemplateRequest("t1", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        var t2 = service.Create(new CreateTemplateRequest("t2", "tok2", null, JsonDocument.Parse("[]" ).RootElement));
        Action act = () => service.Update(t2.Id, new UpdateTemplateRequest("t1", null, null, null));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateTemplate_DuplicateToken_Throws()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        var t1 = service.Create(new CreateTemplateRequest("t1", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        var t2 = service.Create(new CreateTemplateRequest("t2", "tok2", null, JsonDocument.Parse("[]" ).RootElement));
        Action act = () => service.Update(t2.Id, new UpdateTemplateRequest(null, "tok1", null, null));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateTemplate_InvalidToken_Throws()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        var t = service.Create(new CreateTemplateRequest("t1", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        Action act = () => service.Update(t.Id, new UpdateTemplateRequest(null, "bad token", null, null));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateTemplate_InvalidFieldsJson_Throws()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        var t = service.Create(new CreateTemplateRequest("t1", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        var obj = JsonDocument.Parse("{}").RootElement;
        Action act = () => service.Update(t.Id, new UpdateTemplateRequest(null, null, null, obj));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateTemplate_NotFound_Throws()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        Action act = () => service.Update(Guid.NewGuid(), new UpdateTemplateRequest(null, null, null, null));
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void DeleteTemplate_NotFound_Throws()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        Action act = () => service.Delete(Guid.NewGuid());
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void GetPaged_NormalizesInputs()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var service = CreateService(db);
        service.Create(new CreateTemplateRequest("t1", "tok1", null, JsonDocument.Parse("[]" ).RootElement));
        var r1 = service.GetPaged(null, 0, 0, null);
        r1.Page.Should().Be(1);
        r1.PageSize.Should().Be(20);
        var r2 = service.GetPaged(null, 1, 200, null);
        r2.PageSize.Should().Be(100);
    }
}
