using DocflowAi.Net.Api.Rules.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocflowAi.Net.Api.Rules.Endpoints;

/// <summary>
/// Minimal API endpoints for validating and compiling rule builder blocks.
/// </summary>
public static class RuleBuilderEndpoints
{
    public static IEndpointRouteBuilder MapRuleBuilderEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/rulebuilder")
            .WithTags("RuleBuilder")
            .RequireAuthorization();

        group.MapPost("/validate", (RuleBuilderService.CompileReq req, RuleBuilderService svc) =>
        {
            var res = svc.Validate(req);
            return Results.Ok(new { ok = res.Ok, errors = res.Errors });
        });

        group.MapPost("/compile", (RuleBuilderService.CompileReq req, RuleBuilderService svc) =>
        {
            var res = svc.Compile(req);
            return res.Ok
                ? Results.Ok(new { ok = true, code = res.Code })
                : Results.BadRequest(new { ok = false, errors = res.Errors });
        });

        return builder;
    }
}

