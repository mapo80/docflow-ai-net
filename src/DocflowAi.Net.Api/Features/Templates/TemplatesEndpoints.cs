using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace DocflowAi.Net.Api.Features.Templates;

public static class TemplatesEndpoints
{
    public static IEndpointRouteBuilder MapTemplatesEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/templates").WithTags("Templates");

        g.MapGet("/", async (TemplatesDbContext db, CancellationToken ct) =>
        {
            var items = await db.Templates.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
            return items.Select(TemplateDto.From);
        });

        g.MapGet("/{id:guid}", async Task<Results<Ok<TemplateDto>, NotFound>> (Guid id, TemplatesDbContext db, CancellationToken ct) =>
        {
            var t = await db.Templates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null) return TypedResults.NotFound();
            return TypedResults.Ok(TemplateDto.From(t));
        });

        g.MapPost("/", async Task<Results<Ok<TemplateDto>, BadRequest<string>>> (TemplateUpsertRequest req, TemplatesDbContext db, CancellationToken ct) =>
        {
            if (await db.Templates.AnyAsync(x => x.Name == req.Name, ct))
                return TypedResults.BadRequest("Template name already exists");
            var t = new Template
            {
                Name = req.Name,
                DocumentType = req.DocumentType ?? "generic",
                Language = req.Language ?? "auto",
                FieldsJson = string.IsNullOrWhiteSpace(req.FieldsJson) ? "[]" : req.FieldsJson,
                Notes = req.Notes
            };
            db.Templates.Add(t);
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(TemplateDto.From(t));
        });

        g.MapPut("/{id:guid}", async Task<Results<Ok<TemplateDto>, NotFound, BadRequest<string>>> (Guid id, TemplateUpsertRequest req, TemplatesDbContext db, CancellationToken ct) =>
        {
            var t = await db.Templates.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null) return TypedResults.NotFound();
            if (!string.Equals(t.Name, req.Name, StringComparison.OrdinalIgnoreCase) &&
                await db.Templates.AnyAsync(x => x.Name == req.Name, ct))
                return TypedResults.BadRequest("Template name already exists");
            t.Name = req.Name;
            t.DocumentType = req.DocumentType ?? t.DocumentType;
            t.Language = req.Language ?? t.Language;
            t.FieldsJson = string.IsNullOrWhiteSpace(req.FieldsJson) ? t.FieldsJson : req.FieldsJson!;
            t.Notes = req.Notes;
            t.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(TemplateDto.From(t));
        });

        g.MapDelete("/{id:guid}", async Task<Results<Ok, NotFound>> (Guid id, TemplatesDbContext db, CancellationToken ct) =>
        {
            var t = await db.Templates.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null) return TypedResults.NotFound();
            db.Templates.Remove(t);
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok();
        });

        return app;
    }
}
