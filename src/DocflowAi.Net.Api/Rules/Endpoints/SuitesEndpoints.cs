using DocflowAi.Net.Api.Rules.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocflowAi.Net.Api.Rules.Endpoints;

/// <summary>
/// Minimal API endpoints for managing test suites used to group rule test cases.
/// </summary>
public static class SuitesEndpoints
{
    public static IEndpointRouteBuilder MapSuitesEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/suites")
            .WithTags("Suites")
            .RequireAuthorization();

        group.MapGet("/", async (SuiteService svc, CancellationToken ct) =>
        {
            var items = await svc.GetAllAsync(ct);
            return Results.Ok(new { items });
        });

        group.MapPost("/", async (SuiteUpsert req, SuiteService svc, CancellationToken ct) =>
        {
            var (suite, conflict) = await svc.CreateAsync(req.Name, req.Color, req.Description, ct);
            return conflict
                ? Results.Conflict(new { message = "Suite already exists" })
                : Results.Created($"/api/v1/suites/{suite!.Id}", suite);
        });

        group.MapPut("/{id:guid}", async (Guid id, SuiteUpsert req, SuiteService svc, CancellationToken ct) =>
        {
            var ok = await svc.UpdateAsync(id, req.Name, req.Color, req.Description, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id:guid}", async (Guid id, SuiteService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/clone", async (Guid id, SuiteCloneReq req, SuiteService svc, CancellationToken ct) =>
        {
            var (suite, notFound, conflict) = await svc.CloneAsync(id, req.NewName, ct);
            if (notFound) return Results.NotFound();
            if (conflict) return Results.Conflict(new { message = "Suite with this name already exists" });
            return Results.Ok(new { id = suite!.Id, name = suite.Name });
        });

        return builder;
    }

    public record SuiteUpsert(string Name, string? Color, string? Description);
    public record SuiteCloneReq(string NewName);
}

