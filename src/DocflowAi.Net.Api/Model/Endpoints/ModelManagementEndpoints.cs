using DocflowAi.Net.Api.Contracts;
using DocflowAi.Net.Application.Abstractions;
using System.Collections.Generic;

namespace DocflowAi.Net.Api.Model.Endpoints;

public static class ModelManagementEndpoints
{
    public static IEndpointRouteBuilder MapModelManagementEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/models")
            .WithTags("Models")
            .RequireAuthorization()
            .RequireRateLimiting("General");

        group.MapGet("", (IModelService service) =>
            Results.Ok(service.GetAll()))
            .WithName("Models_List")
            .WithSummary("List models")
            .Produces<IEnumerable<ModelDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", (Guid id, IModelService service) =>
        {
            var model = service.GetById(id);
            return model != null ? Results.Ok(model) : Results.NotFound();
        })
        .WithName("Models_Get")
        .WithSummary("Model details")
        .Produces<ModelDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", (CreateModelRequest request, IModelService service) =>
        {
            try
            {
                var model = service.Create(request);
                return Results.Created($"/api/models/{model.Id}", model);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new ErrorResponse("conflict", ex.Message), statusCode: 409);
            }
        })
        .WithName("Models_Create")
        .WithSummary("Create model")
        .Produces<ModelDto>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/download", (Guid id, IModelService service) =>
        {
            try
            {
                service.StartDownload(id);
                return Results.NoContent();
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        })
        .WithName("Models_StartDownload")
        .WithSummary("Start model download")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/download-log", (Guid id, IModelService service) =>
        {
            var text = service.GetDownloadLog(id);
            return string.IsNullOrEmpty(text) ? Results.NotFound() : Results.Text(text);
        })
        .WithName("Models_DownloadLog")
        .WithSummary("Get download log")
        .Produces<string>(StatusCodes.Status200OK, "text/plain")
        .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}", (Guid id, UpdateModelRequest request, IModelService service) =>
        {
            try
            {
                var model = service.Update(id, request);
                return Results.Ok(model);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse("error", ex.Message));
            }
        })
        .WithName("Models_Update")
        .WithSummary("Update model")
        .Produces<ModelDto>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", (Guid id, IModelService service) =>
        {
            service.Delete(id);
            return Results.NoContent();
        })
        .WithName("Models_Delete")
        .WithSummary("Delete model")
        .Produces(StatusCodes.Status204NoContent);

        return builder;
    }
}
