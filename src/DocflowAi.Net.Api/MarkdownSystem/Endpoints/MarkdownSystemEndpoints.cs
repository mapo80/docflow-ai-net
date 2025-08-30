using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Api.Contracts;

namespace DocflowAi.Net.Api.MarkdownSystem.Endpoints;

public static class MarkdownSystemEndpoints
{
    public static IEndpointRouteBuilder MapMarkdownSystemEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/markdown-systems")
            .WithTags("MarkdownSystems")
            .RequireAuthorization()
            .RequireRateLimiting("General");

        group.MapGet("", (IMarkdownSystemService svc) => Results.Ok(svc.GetAll()))
            .WithName("MarkdownSystems_List")
            .WithSummary("List markdown systems")
            .Produces<IEnumerable<MarkdownSystemDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", (Guid id, IMarkdownSystemService svc) =>
        {
            var sys = svc.GetById(id);
            return sys != null ? Results.Ok(sys) : Results.NotFound();
        })
        .WithName("MarkdownSystems_Get")
        .WithSummary("Markdown system details")
        .Produces<MarkdownSystemDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", (CreateMarkdownSystemRequest req, IMarkdownSystemService svc) =>
        {
            try
            {
                var sys = svc.Create(req);
                return Results.Created($"/api/v1/markdown-systems/{sys.Id}", sys);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new ErrorResponse("conflict", ex.Message), statusCode: 409);
            }
        })
        .WithName("MarkdownSystems_Create")
        .WithSummary("Create markdown system")
        .Produces<MarkdownSystemDto>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPatch("/{id:guid}", (Guid id, UpdateMarkdownSystemRequest req, IMarkdownSystemService svc) =>
        {
            try
            {
                var sys = svc.Update(id, req);
                return Results.Ok(sys);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse("error", ex.Message));
            }
        })
        .WithName("MarkdownSystems_Update")
        .WithSummary("Update markdown system")
        .Produces<MarkdownSystemDto>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", (Guid id, IMarkdownSystemService svc) =>
        {
            svc.Delete(id);
            return Results.NoContent();
        })
        .WithName("MarkdownSystems_Delete")
        .WithSummary("Delete markdown system")
        .Produces(StatusCodes.Status204NoContent);

        return builder;
    }
}
