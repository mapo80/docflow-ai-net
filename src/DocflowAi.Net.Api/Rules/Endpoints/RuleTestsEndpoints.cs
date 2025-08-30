using System.Text.Json.Nodes;
using System.Linq;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Rules.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocflowAi.Net.Api.Rules.Endpoints;

/// <summary>
/// Minimal API endpoints for managing and executing rule test cases.
/// </summary>
public static class RuleTestsEndpoints
{
    public static IEndpointRouteBuilder MapRuleTestsEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/rules/{ruleId:guid}/tests")
            .WithTags("RuleTests")
            .RequireAuthorization();

        group.MapGet("/", async (Guid ruleId, [FromQuery] string? search, [FromQuery] string? suite, [FromQuery] string? tag, [FromQuery] string? sortBy, [FromQuery] string? sortDir, [FromQuery] int? page, [FromQuery] int? pageSize, RuleTestCaseService svc, CancellationToken ct) =>
        {
            var p = page.GetValueOrDefault(1);
            var ps = pageSize.GetValueOrDefault(20);
            var (total, items) = await svc.GetAllAsync(ruleId, search, suite, tag, sortBy, sortDir, p, ps, ct);
            var res = new
            {
                total,
                page = p,
                pageSize = ps,
                items = items.Select(t => new { t.Id, t.Name, t.Suite, Tags = (t.TagsCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), t.Priority, t.UpdatedAt })
            };
            return Results.Ok(res);
        });

        group.MapPost("/", async (Guid ruleId, TestUpsert req, RuleTestCaseService svc, CancellationToken ct) =>
        {
            var t = await svc.CreateAsync(ruleId, req.Name, req.Input, req.Expect, req.Suite, req.Tags, req.Priority, ct);
            return Results.Created($"/api/v1/rules/{ruleId}/tests/{t.Id}", new { t.Id, t.Name });
        });

        group.MapPut("/{testId:guid}", async (Guid ruleId, Guid testId, UpdateMeta req, RuleTestCaseService svc, CancellationToken ct) =>
        {
            var ok = await svc.UpdateMetaAsync(ruleId, testId, req.Name, req.Suite, req.Tags, req.Priority, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapPut("/{testId:guid}/content", async (Guid ruleId, Guid testId, UpdateContent req, RuleTestCaseService svc, CancellationToken ct) =>
        {
            var ok = await svc.UpdateContentAsync(ruleId, testId, req.Input, req.Expect, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{testId:guid}", async (Guid ruleId, Guid testId, RuleTestCaseService svc, CancellationToken ct) =>
        {
            var ok = await svc.DeleteAsync(ruleId, testId, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/{testId:guid}/clone", async (Guid ruleId, Guid testId, CloneTestRequest req, RuleTestCaseService svc, CancellationToken ct) =>
        {
            var clone = await svc.CloneAsync(ruleId, testId, req.NewName, req.Suite, req.Tags, ct);
            return clone == null ? Results.NotFound() : Results.Ok(new { id = clone.Id, name = clone.Name });
        });

        group.MapPost("/run", async (Guid ruleId, RuleTestCaseService svc, CancellationToken ct) =>
        {
            var res = await svc.RunAllAsync(ruleId, ct);
            return res == null ? Results.NotFound() : Results.Ok(res);
        });

        group.MapPost("/run-selected", async (Guid ruleId, RunSelectedRequest req, RuleTestCaseService svc, CancellationToken ct) =>
        {
            var res = await svc.RunSelectedAsync(ruleId, req.Ids, ct);
            return res == null ? Results.NotFound() : Results.Ok(res);
        });

        group.MapGet("/coverage", async (Guid ruleId, RuleTestCaseService svc, CancellationToken ct) =>
        {
            var res = await svc.CoverageAsync(ruleId, ct);
            return res == null ? Results.NotFound() : Results.Ok(res);
        });

        return builder;
    }

    public record TestUpsert(string Name, JsonObject Input, JsonObject Expect, string? Suite, string[]? Tags, int? Priority);
    public record UpdateMeta(string? Name, string? Suite, string[]? Tags, int? Priority);
    public record UpdateContent(JsonObject Input, JsonObject Expect);
    public record CloneTestRequest(string? NewName, string? Suite, string[]? Tags);
    public record RunSelectedRequest(List<Guid> Ids);
}

