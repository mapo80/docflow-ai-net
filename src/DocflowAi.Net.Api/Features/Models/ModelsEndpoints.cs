using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace DocflowAi.Net.Api.Features.Models;

public static class ModelsEndpoints
{
    public static IEndpointRouteBuilder MapModelCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/models").WithTags("Models");

        group.MapGet("/", async (ModelCatalogDbContext db, CancellationToken ct) =>
        {
            var items = await db.Models.AsNoTracking().OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
            return items.Select(ModelDto.FromEntity);
        });

        group.MapGet("/{id:guid}", async Task<Results<Ok<ModelDto>, NotFound>> (Guid id, ModelCatalogDbContext db, CancellationToken ct) =>
        {
            var e = await db.Models.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
            if (e is null) return TypedResults.NotFound();
            return TypedResults.Ok(ModelDto.FromEntity(e));
        });

        group.MapPost("/", async Task<Results<Ok<ModelDto>, BadRequest<string>>> (AddModelRequest req, ModelCatalogDbContext db, CancellationToken ct) =>
        {
            if (req.SourceType == ModelSourceType.Url && string.IsNullOrWhiteSpace(req.Url))
                return TypedResults.BadRequest("Url is required for Url source");
            if (req.SourceType == ModelSourceType.HuggingFace && (string.IsNullOrWhiteSpace(req.HfRepo) || string.IsNullOrWhiteSpace(req.HfFilename)))
                return TypedResults.BadRequest("HfRepo and HfFilename are required for HuggingFace source");

            
            // Additional validation for providers
            switch (req.SourceType)
            {
                case ModelSourceType.OpenAI:
                    if (string.IsNullOrWhiteSpace(req.ApiKey) || string.IsNullOrWhiteSpace(req.Model))
                        return TypedResults.BadRequest("OpenAI requires ApiKey and Model");
                    break;
                case ModelSourceType.AzureOpenAI:
                    if (string.IsNullOrWhiteSpace(req.Endpoint) || string.IsNullOrWhiteSpace(req.ApiKey) || string.IsNullOrWhiteSpace(req.Deployment))
                        return TypedResults.BadRequest("Azure OpenAI requires Endpoint, ApiKey and Deployment");
                    break;
                case ModelSourceType.OpenAICompatible:
                    if (string.IsNullOrWhiteSpace(req.Endpoint) || string.IsNullOrWhiteSpace(req.ApiKey) || string.IsNullOrWhiteSpace(req.Model))
                        return TypedResults.BadRequest("OpenAI-compatible requires Endpoint, ApiKey and Model");
                    break;
            }

            var e = new GgufModel
            {
                Name = req.Name,
                SourceType = req.SourceType,
                LocalPath = req.LocalPath,
                Url = req.Url,
                HfRepo = req.HfRepo,
                HfRevision = string.IsNullOrWhiteSpace(req.HfRevision) ? "main" : req.HfRevision,
                HfFilename = req.HfFilename,
                Sha256 = req.Sha256,
                Endpoint = req.Endpoint,
                ApiKey = req.ApiKey,
                Model = req.Model,
                Organization = req.Organization,
                ApiVersion = req.ApiVersion,
                Deployment = req.Deployment,
                ExtraHeadersJson = req.ExtraHeadersJson
            };
            db.Models.Add(e);
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(ModelDto.FromEntity(e));
        });

        group.MapPost("/{id:guid}/download", async Task<Results<Ok, NotFound>> (Guid id, ModelCatalogDbContext db, ModelDownloadWorker worker, CancellationToken ct) =>
        {
            var e = await db.Models.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (e is null) return TypedResults.NotFound();
            worker.Enqueue(id);
            return TypedResults.Ok();
        });

        group.MapPost("/{id:guid}/activate", async Task<Results<Ok, NotFound, BadRequest<string>>> (Guid id, ModelCatalogDbContext db, ModelRuntimeManager runtime, CancellationToken ct) =>
        {
            var e = await db.Models.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
            if (e is null) return TypedResults.NotFound();
            if (e.Status != ModelStatus.Available || string.IsNullOrWhiteSpace(e.LocalPath))
                return TypedResults.BadRequest("Model is not available");
            await runtime.ActivateAsync(id, ct);
            return TypedResults.Ok();
        });

        group.MapDelete("/{id:guid}", async Task<Results<Ok, NotFound, BadRequest<string>>> (Guid id, ModelCatalogDbContext db, CancellationToken ct) =>
        {
            var e = await db.Models.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (e is null) return TypedResults.NotFound();
            if (e.IsActive) return TypedResults.BadRequest("Cannot delete active model");
            e.Status = ModelStatus.Deleting;
            await db.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(e.LocalPath) && File.Exists(e.LocalPath))
            {
                try { File.Delete(e.LocalPath); } catch { /* ignore */ }
            }
            db.Models.Remove(e);
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok();
        });

        return app;
    }
}
