using DocflowAi.Net.Api.Rules.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DocflowAi.Net.Api.Rules.Endpoints;

public static class AiTestsEndpoints
{
    public static IEndpointRouteBuilder MapAiTestsEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/ai/tests")
            .WithTags("AiTests")
            .RequireAuthorization();

        group.MapPost("/suggest", async ([FromQuery] Guid ruleId, SuggestReq req, SuggestionService svc, CancellationToken ct) =>
        {
            var (suggs, model, total, inTok, outTok, dur, cost) = await svc.SuggestAsync(ruleId, req.UserPrompt, req.Budget ?? 20, req.Temperature ?? 0.2, ct);
            var shaped = suggs.Select(s => new
            {
                id = s.Id,
                reason = s.Reason,
                score = s.Score,
                coverageDelta = JsonSerializer.Deserialize<object>(s.CoverageDeltaJson),
                payload = JsonSerializer.Deserialize<object>(s.PayloadJson),
                createdAt = s.CreatedAt,
                model = s.Model
            }).ToList();
            return Results.Ok(new SuggestRes(shaped, model, total, inTok, outTok, dur, cost));
        });

        group.MapPost("/import", async ([FromQuery] Guid ruleId, ImportReq req, SuggestionService svc, ILoggerFactory lf, CancellationToken ct) =>
        {
            var count = await svc.ImportAsync(ruleId, req.Ids, req.Suite, req.Tags, ct);
            var log = lf.CreateLogger("AiTestsEndpoints");
            log.LogInformation("Imported {Count} AI suggestions into tests for rule {RuleId}", count, ruleId);
            return Results.Ok(new { imported = count });
        });

        return builder;
    }

    public record SuggestReq(string? UserPrompt, int? Budget, double? Temperature);
    public record SuggestRes(IEnumerable<object> Suggestions, string Model, int TotalSkeletons, int InputTokens, int OutputTokens, long DurationMs, double CostUsd);
    public record ImportReq(Guid[] Ids, string? Suite, string[]? Tags);
}
