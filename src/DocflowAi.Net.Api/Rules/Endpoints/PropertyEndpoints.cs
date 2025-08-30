using DocflowAi.Net.Api.Rules.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace DocflowAi.Net.Api.Rules.Endpoints;

public static class PropertyEndpoints
{
    public static IEndpointRouteBuilder MapPropertyEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/rules/{ruleId:guid}/properties")
            .WithTags("Properties")
            .RequireAuthorization();

        group.MapPost("/run", async ([FromRoute] Guid ruleId, [FromQuery] int? trials, [FromQuery] int? seed, PropertyTestService svc, CancellationToken ct) =>
        {
            var res = await svc.RunForRuleAsync(ruleId, trials ?? 100, seed, ct);
            return Results.Ok(res);
        });

        group.MapPost("/import-failures", async ([FromRoute] Guid ruleId, ImportFailuresReq req, PropertyTestService svc, ILoggerFactory lf, CancellationToken ct) =>
        {
            var imported = await svc.ImportFailuresAsync(ruleId, req.Failures ?? Array.Empty<PropertyFailure>(), req.Suite, req.Tags, ct);
            var log = lf.CreateLogger("PropertyEndpoints");
            log.LogInformation("Imported {Count} property failures as tests for rule {RuleId}", imported, ruleId);
            return Results.Ok(new { imported });
        });

        builder.MapPost("/api/v1/rules/properties/run-from-blocks", async (BlocksReq req, PropertyTestService svc, CancellationToken ct) =>
        {
            var res = await svc.RunFromBlocksAsync(req.Blocks, req.Trials, req.Seed, ct);
            return Results.Ok(res);
        })
        .WithTags("Properties")
        .RequireAuthorization();

        return builder;
    }

    public record BlocksReq(JsonArray Blocks, int Trials, int? Seed);
    public record ImportFailuresReq(PropertyFailure[]? Failures, string? Suite, string[]? Tags);
}
