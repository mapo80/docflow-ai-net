using DocflowAi.Net.Api.Features.Templates;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class TemplatesCrudTests
{
    private TemplatesDbContext CreateInMemory()
    {
        var conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<TemplatesDbContext>()
            .UseSqlite(conn).Options;
        var ctx = new TemplatesDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task Create_List_Update_Delete_Works()
    {
        using var db = CreateInMemory();
        db.Templates.Add(new Template { Name = "Invoice", DocumentType = "invoice", Language = "it", FieldsJson = "[]" });
        await db.SaveChangesAsync();

        var t = await db.Templates.FirstAsync(x => x.Name == "Invoice");
        Assert.Equal("invoice", t.DocumentType);

        t.Language = "en";
        await db.SaveChangesAsync();

        var updated = await db.Templates.FirstAsync(x => x.Name == "Invoice");
        Assert.Equal("en", updated.Language);

        db.Templates.Remove(updated);
        await db.SaveChangesAsync();

        Assert.False(await db.Templates.AnyAsync());
    }
}
