using System;
using System.Collections.Generic;
using System.Linq;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Api.Contracts;
using DocflowAi.Net.Infrastructure.Markdown;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace DocflowAi.Net.Api.Markdown.Endpoints;

public static class MarkdownEndpoints
{
    public static IEndpointRouteBuilder MapMarkdownEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/markdown")
            .WithTags("Markdown")
            .RequireRateLimiting("General")
            .RequireAuthorization();

        group.DisableAntiforgery();

        group.MapPost(string.Empty, ConvertFileAsync)
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<MarkdownResult>(StatusCodes.Status200OK, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithName("Markdown_Convert")
        .WithOpenApi(op =>
        {
            var p = op.Parameters?.FirstOrDefault(x => x.Name == "language");
            if (p != null)
            {
                p.Required = true;
                if (p.Schema != null)
                {
                    p.Schema.Enum = new OpenApiArray
                    {
                        new OpenApiString("ita"),
                        new OpenApiString("eng"),
                    };
                }
            }
            return op;
        });

        return builder;
    }

    internal static async Task<IResult> ConvertFileAsync(IFormFile? file, string? language, IMarkdownConverter conv, IOptions<MarkdownOptions> opts)
    {
        if (file == null || file.Length == 0)
            return Results.Json(new ErrorResponse("bad_request", "file required"), statusCode: 400);
        if (string.IsNullOrWhiteSpace(language) || (language != "ita" && language != "eng"))
            return Results.Json(new ErrorResponse("bad_request", "language must be 'ita' or 'eng'"), statusCode: 400);

        await using var stream = file.OpenReadStream();
        try
        {
            MarkdownResult result;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var mdOpts = new MarkdownOptions
            {
                OcrDataPath = opts.Value.OcrDataPath,
                OcrLanguages = language,
                PdfRasterDpi = opts.Value.PdfRasterDpi,
                MinimumNativeWordThreshold = opts.Value.MinimumNativeWordThreshold,
                NormalizeMarkdown = opts.Value.NormalizeMarkdown
            };
            if (ext == ".pdf")
                result = await conv.ConvertPdfAsync(stream, mdOpts);
            else
                result = await conv.ConvertImageAsync(stream, mdOpts);

            return Results.Json(result);
        }
        catch (MarkdownConversionException ex)
        {
            var status = ex.Code == "unsupported_format" ? 400 : 422;
            return Results.Json(new ErrorResponse(ex.Code, ex.Message), statusCode: status);
        }
        catch (DllNotFoundException ex)
        {
            return Results.Json(new ErrorResponse("native_library_missing", ex.Message), statusCode: 500);
        }
        catch (Exception ex)
        {
            return Results.Json(new ErrorResponse("markdown_conversion_failed", ex.Message), statusCode: 500);
        }
    }
}
