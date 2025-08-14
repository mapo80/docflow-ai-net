using DocflowAi.Net.Api.Contracts;
using DocflowAi.Net.Api.Models;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.Api.Model.Endpoints;

public static class ModelEndpoints
{
    public static IEndpointRouteBuilder MapModelEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/model")
            .WithTags("Model")
            .RequireAuthorization()
            .RequireRateLimiting("General");

        group.MapPost("/switch", async (SwitchModelRequest request, ILlmModelService service, ILoggerFactory lf, CancellationToken ct) =>
        {
            var logger = lf.CreateLogger("ModelEndpoints");
            if (string.IsNullOrWhiteSpace(request.HfKey) ||
                string.IsNullOrWhiteSpace(request.ModelRepo) ||
                string.IsNullOrWhiteSpace(request.ModelFile) ||
                request.ContextSize <= 0)
            {
                return Results.Json(new ErrorResponse("bad_request", "invalid request"), statusCode: 400);
            }
            try
            {
                logger.LogInformation("ModelSwitchStarted {Repo} {File} {ContextSize}", request.ModelRepo, request.ModelFile, request.ContextSize);
                await service.SwitchModelAsync(request.HfKey, request.ModelRepo, request.ModelFile, request.ContextSize, ct);
                logger.LogInformation("ModelSwitchCompleted");
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "ModelSwitchFailed {Error}", ex.Message);
                return Results.Json(new ErrorResponse("conflict", ex.Message), statusCode: 409);
            }
            catch (FileNotFoundException ex)
            {
                logger.LogWarning(ex, "ModelSwitchFailed {Error}", ex.Message);
                return Results.Json(new ErrorResponse("not_found", ex.Message), statusCode: 404);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ModelSwitchFailed {Error}", ex.Message);
                return Results.Json(new ErrorResponse("server_error", null), statusCode: 500);
            }
        })
        .WithName("Model_Switch")
        .WithSummary("Switch model")
        .WithDescription("Starts downloading and activating the specified model")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .WithOpenApi(op =>
        {
            op.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiObject
                        {
                            ["hfKey"] = new OpenApiString("***"),
                            ["modelRepo"] = new OpenApiString("TheOrg/the-model"),
                            ["modelFile"] = new OpenApiString("model.gguf"),
                            ["contextSize"] = new OpenApiInteger(4096)
                        }
                    }
                }
            };
            op.Responses["200"].Content["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiObject { }
            };
            return op;
        });

        group.MapGet("/status", (ILlmModelService service, ILoggerFactory lf) =>
        {
            var logger = lf.CreateLogger("ModelEndpoints");
            var status = service.GetStatus();
            logger.LogInformation("ModelStatusFetched {Completed} {Percentage}", status.Completed, status.Percentage);
            return Results.Ok(status);
        })
        .WithName("Model_Status")
        .WithSummary("Model switch status")
        .WithDescription("Gets progress for the current model switch")
        .Produces<ModelDownloadStatus>(StatusCodes.Status200OK)
        .WithOpenApi(op =>
        {
            op.Responses["200"].Content["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiObject
                {
                    ["completed"] = new OpenApiBoolean(false),
                    ["percentage"] = new OpenApiDouble(35),
                    ["message"] = new OpenApiString("downloading...")
                }
            };
            return op;
        });

        return builder;
    }
}
