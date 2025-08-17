using System.Text.Json;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.Features.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace DocflowAi.Net.Api.Features.Templates;

public static class RecognizeEndpoints
{
    public static IEndpointRouteBuilder MapRecognitionEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/recognitions").WithTags("Recognitions");

        // POST /api/recognitions/run
        // form-data: file (required), modelName (optional), modelId (optional Guid), templateName (required)
        g.MapPost("/run", async Task<Results<Ok<RecognitionRunResponse>, BadRequest<string>>> (
            HttpRequest http,
            TemplatesDbContext tdb,
            RecognitionsDbContext rdb,
            ModelCatalogDbContext mdb,
            IProcessService process,
            CancellationToken ct) =>
        {
            if (!http.HasFormContentType) return TypedResults.BadRequest("multipart/form-data required");
            var form = await http.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0) return TypedResults.BadRequest("file required");

            var templateName = form["templateName"].ToString();
            if (string.IsNullOrWhiteSpace(templateName)) return TypedResults.BadRequest("templateName required");

            var modelName = form["modelName"].ToString();
            var modelIdStr = form["modelId"].ToString();
            GgufModel? model = null;

            if (Guid.TryParse(modelIdStr, out var mid))
            {
                model = await mdb.Models.AsNoTracking().FirstOrDefaultAsync(m => m.Id == mid, ct);
            }
            else if (!string.IsNullOrWhiteSpace(modelName))
            {
                model = await mdb.Models.AsNoTracking().FirstOrDefaultAsync(m => m.Name == modelName, ct);
            }

            if (model is null) return TypedResults.BadRequest("model not found");

            var tpl = await tdb.Templates.AsNoTracking().FirstOrDefaultAsync(t => t.Name == templateName, ct);
            if (tpl is null) return TypedResults.BadRequest("template not found");

            // Save upload to temp dir
            var tmpDir = Path.Combine(Path.GetTempPath(), "recognitions", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmpDir);
            var inputPath = Path.Combine(tmpDir, file.FileName);
            await using (var fs = File.Create(inputPath))
            {
                await file.CopyToAsync(fs, ct);
            }

            // Execute current processing pipeline (uses job abstraction) with minimal inputs
            var pi = new ProcessInput(Guid.NewGuid(), inputPath, promptPath: null, fieldsPath: null);
            var result = await process.ExecuteAsync(pi, ct);
            if (!result.Success)
            {
                return TypedResults.BadRequest($"recognition failed: {result.ErrorMessage}");
            }

            // Expect OutputJson with at least markdown + fields keys; fall back if not present
            string markdown = "";
            string fieldsJson = "{}";
            try
            {
                using var doc = JsonDocument.Parse(result.OutputJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("markdown", out var md)) markdown = md.GetString() ?? "";
                if (root.TryGetProperty("fields", out var fj)) fieldsJson = fj.GetRawText();
                if (string.IsNullOrEmpty(markdown)) markdown = result.OutputJson;
            }
            catch
            {
                markdown = result.OutputJson;
            }

            var rec = new RecognitionRecord
            {
                TemplateName = tpl.Name,
                ModelName = model.Name,
                FileName = file.FileName,
                Markdown = markdown,
                FieldsJson = fieldsJson
            };
            rdb.Recognitions.Add(rec);
            await rdb.SaveChangesAsync(ct);

            var response = new RecognitionRunResponse(rec.Id, rec.TemplateName, rec.ModelName, markdown, fieldsJson, rec.CreatedAt);
            return TypedResults.Ok(response);
        })
        .DisableAntiforgery();

        return app;
    }
}
