using DocflowAi.Net.Api.Rules.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace DocflowAi.Net.Api.Rules.Endpoints;

public static class FuzzEndpoints
{
    public static IEndpointRouteBuilder MapFuzzEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/rules/{ruleId:guid}/fuzz")
            .WithTags("Fuzz")
            .RequireAuthorization();

        group.MapPost("/preview", async ([FromRoute] Guid ruleId, [FromQuery] int? maxPerField, FuzzService svc, CancellationToken ct) =>
        {
            var arr = await svc.GenerateAsync(ruleId, maxPerField ?? 5, ct);
            return Results.Ok(new { items = arr });
        });

        group.MapPost("/import", async ([FromRoute] Guid ruleId, FuzzImportReq req, FuzzService svc, ILoggerFactory lf, CancellationToken ct) =>
        {
            var n = await svc.ImportAsync(ruleId, req.Items, req.Suite, req.Tags, ct);
            var log = lf.CreateLogger("FuzzEndpoints");
            log.LogInformation("Imported {Count} fuzz tests for rule {RuleId}", n, ruleId);
            return Results.Ok(new { imported = n });
        });

        return builder;
    }

    public record FuzzImportReq(JsonArray Items, string? Suite, string[]? Tags);
}
