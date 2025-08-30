using DocflowAi.Net.Api.Rules.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocflowAi.Net.Api.Rules.Endpoints;

/// <summary>
/// Minimal API endpoints for managing test tags used to categorize rule test cases.
/// </summary>
public static class TagsEndpoints
{
    public static IEndpointRouteBuilder MapTagsEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/tags")
            .WithTags("Tags")
            .RequireAuthorization();

        group.MapGet("/", async (TagService svc, CancellationToken ct) =>
        {
            var items = await svc.GetAllAsync(ct);
            return Results.Ok(new { items });
        });

        group.MapPost("/", async (TagUpsert req, TagService svc, CancellationToken ct) =>
        {
            var (tag, conflict) = await svc.CreateAsync(req.Name, req.Color, req.Description, ct);
            return conflict
                ? Results.Conflict(new { message = "Tag already exists" })
                : Results.Created($"/api/v1/tags/{tag!.Id}", tag);
        });

        group.MapPut("/{id:guid}", async (Guid id, TagUpsert req, TagService svc, CancellationToken ct) =>
        {
            var ok = await svc.UpdateAsync(id, req.Name, req.Color, req.Description, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id:guid}", async (Guid id, TagService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        return builder;
    }

    public record TagUpsert(string Name, string? Color, string? Description);
}
