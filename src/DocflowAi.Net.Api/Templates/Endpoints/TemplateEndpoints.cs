using DocflowAi.Net.Api.Contracts;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;

namespace DocflowAi.Net.Api.Templates.Endpoints;

public static class TemplateEndpoints
{
    public static IEndpointRouteBuilder MapTemplateEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/templates")
            .WithTags("Templates")
            .RequireAuthorization()
            .RequireRateLimiting("General");

        group.MapGet("", (string? q, int? page, int? pageSize, string? sort, ITemplateService service) =>
        {
            var result = service.GetPaged(q, page ?? 1, pageSize ?? 20, sort);
            return Results.Ok(result);
        })
        .WithName("Templates_List")
        .WithSummary("List templates")
        .Produces<PagedResult<TemplateSummary>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", (Guid id, ITemplateService service) =>
        {
            var tpl = service.GetById(id);
            return tpl != null ? Results.Ok(tpl) : Results.NotFound();
        })
        .WithName("Templates_Get")
        .WithSummary("Template details")
        .Produces<TemplateDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", (CreateTemplateRequest request, ITemplateService service) =>
        {
            try
            {
                var tpl = service.Create(request);
                return Results.Created($"/api/v1/templates/{tpl.Id}", tpl);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new ErrorResponse("conflict", ex.Message), statusCode: 409);
            }
            catch (ArgumentException ex)
            {
                return Results.Json(new ErrorResponse("validation_error", ex.Message), statusCode: 422);
            }
        })
        .WithName("Templates_Create")
        .WithSummary("Create template")
        .Produces<TemplateDto>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        group.MapPatch("/{id:guid}", (Guid id, UpdateTemplateRequest request, ITemplateService service) =>
        {
            try
            {
                var tpl = service.Update(id, request);
                return Results.Ok(tpl);
            }
            catch (KeyNotFoundException)
            {
                return Results.Json(new ErrorResponse("not_found", "template not found"), statusCode: 404);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new ErrorResponse("conflict", ex.Message), statusCode: 409);
            }
            catch (ArgumentException ex)
            {
                return Results.Json(new ErrorResponse("validation_error", ex.Message), statusCode: 422);
            }
        })
        .WithName("Templates_Update")
        .WithSummary("Update template")
        .Produces<TemplateDto>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/{id:guid}", (Guid id, ITemplateService service) =>
        {
            try
            {
                service.Delete(id);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.Json(new ErrorResponse("not_found", "template not found"), statusCode: 404);
            }
        })
        .WithName("Templates_Delete")
        .WithSummary("Delete template")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return builder;
    }
}
