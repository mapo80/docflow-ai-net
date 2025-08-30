using System.Text.Json.Nodes;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Rules.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocflowAi.Net.Api.Rules.Endpoints;

/// <summary>
/// Minimal API endpoints for managing rule functions and executing them.
/// </summary>
public static class RulesEndpoints
{
    public static IEndpointRouteBuilder MapRulesEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/rules")
            .WithTags("Rules")
            .RequireAuthorization();

        group.MapGet("/", async ([FromQuery] string? search, [FromQuery] string? sortBy, [FromQuery] string? sortDir, [FromQuery] int? page, [FromQuery] int? pageSize, RuleService svc, CancellationToken ct) =>
        {
            var p = page.GetValueOrDefault(1);
            var ps = pageSize.GetValueOrDefault(20);
            var (total, items) = await svc.GetAllAsync(search, sortBy, sortDir, p, ps, ct);
            var res = new
            {
                total,
                page = p,
                pageSize = ps,
                items = items.Select(r => new { r.Id, r.Name, r.Version, r.Enabled, r.UpdatedAt })
            };
            return Results.Ok(res);
        });

        group.MapGet("/{id:guid}", async (Guid id, RuleService svc, CancellationToken ct) =>
        {
            var r = await svc.GetAsync(id, ct);
            return r == null
                ? Results.NotFound()
                : Results.Ok(r);
        });

        group.MapPost("/", async (RuleUpsert req, RuleService svc, CancellationToken ct) =>
        {
            var (rule, conflict) = await svc.CreateAsync(req.Name, req.Description, req.Code, req.ReadsCsv, req.WritesCsv, req.Enabled, ct);
            return conflict
                ? Results.Conflict(new { message = "Name already exists" })
                : Results.Created($"/api/v1/rules/{rule!.Id}", rule);
        });

        group.MapPut("/{id:guid}", async (Guid id, RuleUpsert req, RuleService svc, CancellationToken ct) =>
        {
            var res = await svc.UpdateAsync(id, req.Name, req.Description, req.Code, req.ReadsCsv, req.WritesCsv, req.Enabled, ct);
            return res switch
            {
                RuleService.UpdateResult.Ok => Results.NoContent(),
                RuleService.UpdateResult.NotFound => Results.NotFound(),
                RuleService.UpdateResult.Builtin => Results.BadRequest(new { message = "Built-in rules are read-only" }),
                _ => Results.BadRequest()
            };
        });

        group.MapPost("/{id:guid}/stage", async (Guid id, RuleService svc, CancellationToken ct) =>
        {
            var ok = await svc.StageAsync(id, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/{id:guid}/publish", async (Guid id, RuleService svc, CancellationToken ct) =>
        {
            var ok = await svc.PublishAsync(id, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/{id:guid}/compile", async (Guid id, RuleService svc, CancellationToken ct) =>
        {
            var res = await svc.CompileAsync(id, ct);
            return res == null ? Results.NotFound() : Results.Ok(res);
        });

        group.MapPost("/{id:guid}/run", async (Guid id, RunBody body, RuleService svc, CancellationToken ct) =>
        {
            var res = await svc.RunAsync(id, body.Input, ct);
            return res == null ? Results.NotFound() : Results.Ok(res);
        });

        group.MapPost("/{id:guid}/clone", async (Guid id, CloneRuleRequest req, RuleService svc, CancellationToken ct) =>
        {
            var clone = await svc.CloneAsync(id, req.NewName, req.IncludeTests == true, ct);
            return clone == null
                ? Results.NotFound()
                : Results.Ok(new { id = clone.Id, name = clone.Name });
        });

        return builder;
    }

    public record RuleUpsert(string Name, string? Description, string Code, string? ReadsCsv, string? WritesCsv, bool Enabled);
    public record RunBody(JsonObject Input);
    public record CloneRuleRequest(string? NewName, bool? IncludeTests);
}

