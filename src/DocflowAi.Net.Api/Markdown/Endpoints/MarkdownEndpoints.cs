using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Api.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace DocflowAi.Net.Api.Markdown.Endpoints;

public static class MarkdownEndpoints
{
    public static IEndpointRouteBuilder MapMarkdownEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/markdown")
            .WithTags("Markdown")
            .RequireRateLimiting("General");

        group.MapPost(string.Empty, ConvertFileAsync)
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<string>(StatusCodes.Status200OK, "text/plain")
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .WithName("Markdown_Convert");

        return builder;
    }

    internal static async Task<IResult> ConvertFileAsync(IFormFile? file, IMarkdownConverter conv, IOptions<MarkdownOptions> opts)
    {
        if (file == null || file.Length == 0)
            return Results.Json(new ErrorResponse("bad_request", "file required"), statusCode: 400);

        await using var stream = file.OpenReadStream();
        MarkdownResult result;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext == ".pdf")
            result = await conv.ConvertPdfAsync(stream, opts.Value);
        else
            result = await conv.ConvertImageAsync(stream, opts.Value);

        return Results.Text(result.Markdown, "text/plain");
    }
}
