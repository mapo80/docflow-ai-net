using DocflowAi.Net.Api.Contracts;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.Options;
using DocflowAi.Net.Api.MarkdownSystem.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Microsoft.AspNetCore.Http;

namespace DocflowAi.Net.Api.JobQueue.Endpoints;

public static class JobEndpoints
{
    private record SubmitRequest(string FileBase64, string FileName, string Model, string TemplateToken, string Language, Guid MarkdownSystemId);

    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/jobs")
            .WithTags("Jobs")
            .RequireRateLimiting("General");

        group.MapGet(string.Empty, (int? page, int? pageSize, IJobRepository store) =>
        {
            var p = page ?? 1;
            var ps = pageSize ?? 20;
            if (p < 1)
            {
                return Results.Json(new ErrorResponse("bad_request", "page must be >= 1"), statusCode: 400);
            }
            if (ps > 100) ps = 100;
            var (items, total) = store.ListPaged(p, ps);
            var response = new PagedJobsResponse(
                p,
                ps,
                total,
                items.Select(i => new JobSummary(
                    i.Id,
                    i.Status,
                    MapDerivedStatus(i.Status),
                    i.Progress,
                    i.Attempts,
                    i.CreatedAt,
                    i.UpdatedAt,
                    i.Model,
                    i.TemplateToken,
                    i.Language,
                    i.MarkdownSystemName
                )).ToList()
            );
            return Results.Ok(response);
        })
        .WithName("Jobs_List")
        .Produces<PagedJobsResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .WithOpenApi(op =>
        {
            op.Responses["200"].Content["application/json"].Example = new OpenApiObject
            {
                ["page"] = new OpenApiInteger(1),
                ["pageSize"] = new OpenApiInteger(2),
                ["total"] = new OpenApiInteger(2),
                ["items"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["id"] = new OpenApiString("00000000-0000-0000-0000-000000000001"),
                        ["status"] = new OpenApiString("Queued"),
                        ["derivedStatus"] = new OpenApiString("Pending"),
                        ["progress"] = new OpenApiInteger(0),
                        ["createdAt"] = new OpenApiString("2024-01-01T00:00:00Z"),
                        ["updatedAt"] = new OpenApiString("2024-01-01T00:00:00Z"),
                        ["language"] = new OpenApiString("eng"),
                        ["markdownSystem"] = new OpenApiString("docling")
                    },
                    new OpenApiObject
                    {
                        ["id"] = new OpenApiString("00000000-0000-0000-0000-000000000002"),
                        ["status"] = new OpenApiString("Succeeded"),
                        ["derivedStatus"] = new OpenApiString("Completed"),
                        ["progress"] = new OpenApiInteger(100),
                        ["createdAt"] = new OpenApiString("2024-01-01T00:00:00Z"),
                        ["updatedAt"] = new OpenApiString("2024-01-01T00:01:00Z"),
                        ["language"] = new OpenApiString("ita"),
                        ["markdownSystem"] = new OpenApiString("docling")
                    },
                    new OpenApiObject
                    {
                        ["id"] = new OpenApiString("00000000-0000-0000-0000-000000000003"),
                        ["status"] = new OpenApiString("Failed"),
                        ["derivedStatus"] = new OpenApiString("Failed"),
                        ["progress"] = new OpenApiInteger(0),
                        ["createdAt"] = new OpenApiString("2024-01-01T00:00:00Z"),
                        ["updatedAt"] = new OpenApiString("2024-01-01T00:02:00Z"),
                        ["language"] = new OpenApiString("lat"),
                        ["markdownSystem"] = new OpenApiString("docling")
                    }
                }
            };
            return op;
        });

        group.MapGet("/{id}/files/{file}", (Guid id, string file, IJobRepository store) =>
        {
            var job = store.Get(id);
            if (job == null) return Results.NotFound();
            var name = Path.GetFileName(file);
            var candidates = new[]
            {
                job.Paths.Input?.Path,
                job.Paths.Output?.Path,
                job.Paths.Error?.Path,
                job.Paths.Markdown?.Path,
                job.Paths.Prompt?.Path
            }
                .Where(p => !string.IsNullOrEmpty(p));
            var match = candidates.FirstOrDefault(p => Path.GetFileName(p!)
                .Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match == null) return Results.NotFound();
            var path = Path.GetFullPath(match);
            if (!File.Exists(path)) return Results.NotFound();
            var contentType = GetContentType(name);
            return Results.File(path, contentType, name);
        })
        .WithName("Jobs_File")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost(string.Empty, async (HttpRequest req, IJobRepository store, IUnitOfWork uow, IFileSystemService fs, IMarkdownSystemRepository msRepo, IBackgroundJobClient jobs, IOptions<JobQueueOptions> opts, ILoggerFactory lf) =>
        {
            var logger = lf.CreateLogger("JobEndpoints");
            var sw = Stopwatch.StartNew();
            logger.LogInformation("SubmitJobStarted");

            SubmitRequest? payload = await req.ReadFromJsonAsync<SubmitRequest>();
            if (payload is null || string.IsNullOrEmpty(payload.FileBase64) || string.IsNullOrEmpty(payload.FileName)
                || string.IsNullOrEmpty(payload.Model) || string.IsNullOrEmpty(payload.TemplateToken) || string.IsNullOrEmpty(payload.Language)
                || payload.MarkdownSystemId == Guid.Empty)
                return Results.Json(new ErrorResponse("bad_request", "file, model, template, language and markdown system required"), statusCode: 400);

            var system = msRepo.GetById(payload.MarkdownSystemId);
            if (system == null)
                return Results.Json(new ErrorResponse("bad_request", "markdown system not found"), statusCode: 400);

            if (payload.Language != "ita" && payload.Language != "eng" && payload.Language != "lat")
                return Results.Json(new ErrorResponse("bad_request", "language must be 'ita', 'eng', or 'lat'"), statusCode: 400);

            var bytes = Convert.FromBase64String(payload.FileBase64);
            var optsVal = opts.Value;
            if (bytes.Length > optsVal.UploadLimits.MaxRequestBodyMB * 1024 * 1024)
            {
                logger.LogWarning("PayloadTooLarge {Bytes} {LimitMb}", bytes.Length, optsVal.UploadLimits.MaxRequestBodyMB);
                return Results.Json(new ErrorResponse("payload_too_large", "payload too large"), statusCode: 413);
            }
            var ext = Path.GetExtension(payload.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
            if (!allowed.Contains(ext))
            {
                logger.LogWarning("UnsupportedFileType {FileExt}", ext);
                return Results.Json(new ErrorResponse("bad_request", "unsupported file type"), statusCode: 400);
            }
            var dataRoot = Path.GetFullPath(optsVal.DataRoot);
            var drive = new DriveInfo(Path.GetPathRoot(dataRoot)!);
            if (drive.AvailableFreeSpace < bytes.Length + 1_000_000)
            {
                logger.LogWarning("InsufficientStorage {Bytes} {FreeBytes}", bytes.Length, drive.AvailableFreeSpace);
                return Results.Json(new ErrorResponse("insufficient_storage", "insufficient storage"), statusCode: 507);
            }
            var safeName = Path.GetFileName(payload.FileName);
            var formFile = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", safeName);

            if (store.CountPending() >= optsVal.Queue.MaxQueueLength)
            {
                logger.LogWarning("BackpressureTriggered {PendingCount} {MaxQueueLength}", store.CountPending(), optsVal.Queue.MaxQueueLength);
                logger.LogWarning("RejectedDueToBackpressure");
                var retry = 60;
                req.HttpContext.Response.Headers["Retry-After"] = retry.ToString();
                return Results.Json(new ErrorResponse("queue_full", "queue is full", retry), statusCode: 429);
            }

            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var idemKey = req.Headers["Idempotency-Key"].FirstOrDefault();
            if (!string.IsNullOrEmpty(idemKey))
            {
                var existing = store.FindByIdempotencyKey(idemKey, TimeSpan.FromMinutes(30));
                if (existing != null)
                {
                    logger.LogInformation("IdempotencyHit {JobId}", existing.Id);
                    return Results.Accepted($"/api/v1/jobs/{existing.Id}", new SubmitAcceptedResponse(existing.Id, $"/api/v1/jobs/{existing.Id}", optsVal.EnableHangfireDashboard ? "/hangfire" : null));
                }
            }

            var dup = store.FindRecentByHash(hash, TimeSpan.FromMinutes(30));
            if (dup != null)
            {
                logger.LogInformation("HashDedupeHit {JobId}", dup.Id);
                return Results.Accepted($"/api/v1/jobs/{dup.Id}", new SubmitAcceptedResponse(dup.Id, $"/api/v1/jobs/{dup.Id}", optsVal.EnableHangfireDashboard ? "/hangfire" : null));
            }

            var jobId = Guid.NewGuid();
            fs.CreateJobDirectory(jobId);
            var inputPath = await fs.SaveInputAtomic(jobId, formFile);
            var manifest = JsonSerializer.Serialize(new { jobId, payload.FileName, ext, hash, createdAtUtc = DateTimeOffset.UtcNow, model = payload.Model, templateToken = payload.TemplateToken, language = payload.Language, markdownSystem = system.Name });
            await fs.SaveTextAtomic(jobId, "manifest.json", manifest);

            var doc = new JobDocument
            {
                Id = jobId,
                Status = "Queued",
                Progress = 0,
                Attempts = 0,
                Hash = hash,
                IdempotencyKey = idemKey,
                Model = payload.Model,
                TemplateToken = payload.TemplateToken,
                Language = payload.Language,
                MarkdownSystemId = system.Id,
                MarkdownSystemName = system.Name,
                Paths = new JobDocument.PathInfo
                {
                    Dir = Path.GetDirectoryName(inputPath)!,
                    Input = new JobDocument.DocumentInfo { Path = inputPath, CreatedAt = DateTimeOffset.UtcNow },
                    Prompt = new JobDocument.DocumentInfo { Path = Path.Combine(Path.GetDirectoryName(inputPath)!, "prompt.md") },
                    Output = new JobDocument.DocumentInfo { Path = Path.Combine(Path.GetDirectoryName(inputPath)!, "output.json") },
                    Error = new JobDocument.DocumentInfo { Path = Path.Combine(Path.GetDirectoryName(inputPath)!, "error.txt") },
                    Markdown = new JobDocument.DocumentInfo { Path = Path.Combine(Path.GetDirectoryName(inputPath)!, "markdown.md") }
                }
            };
            store.Create(doc);
            uow.SaveChanges();
            jobs.Enqueue<IJobRunner>(r => r.Run(jobId, JobCancellationToken.Null, CancellationToken.None, true, null));
            logger.LogInformation("SubmitJobCompleted {JobId} {ElapsedMs}", jobId, sw.ElapsedMilliseconds);
            return Results.Accepted($"/api/v1/jobs/{jobId}", new SubmitAcceptedResponse(jobId, $"/api/v1/jobs/{jobId}", optsVal.EnableHangfireDashboard ? "/hangfire" : null));
        }).RequireRateLimiting("Submit")
          .WithName("Jobs_Create")
          .Produces<SubmitAcceptedResponse>(StatusCodes.Status202Accepted)
          .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
          .Produces<ErrorResponse>(StatusCodes.Status413PayloadTooLarge)
          .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests)
          .Produces<ErrorResponse>(StatusCodes.Status507InsufficientStorage)
          .WithOpenApi(op =>
          {
              op.Parameters.Add(new OpenApiParameter
              {
                  Name = "Idempotency-Key",
                  In = ParameterLocation.Header,
                  Required = false,
                  Description = "Optional idempotency key",
                  Schema = new OpenApiSchema { Type = "string" }
              });
              op.Responses["202"].Content["application/json"].Example = new OpenApiObject
              {
                  ["job_id"] = new OpenApiString("00000000-0000-0000-0000-000000000000"),
                  ["status_url"] = new OpenApiString("/api/v1/jobs/{id}"),
                  ["dashboard_url"] = new OpenApiString("/hangfire")
              };
              op.Responses["429"].Headers["Retry-After"] = new OpenApiHeader
              {
                  Description = "Seconds to wait before retrying",
                  Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
              };
              op.Responses["429"].Content["application/json"].Examples = new Dictionary<string, OpenApiExample>
              {
                  ["queue_full"] = new()
                  {
                      Value = new OpenApiObject
                      {
                          ["error"] = new OpenApiString("queue_full"),
                          ["message"] = new OpenApiString("queue is full"),
                          ["retry_after_seconds"] = new OpenApiInteger(60)
                      }
                  }
              };
              op.Responses["413"].Content["application/json"].Example = new OpenApiObject
              {
                  ["error"] = new OpenApiString("payload_too_large"),
                  ["message"] = new OpenApiString("payload too large")
              };
              return op;
          });

        group.MapGet("/{id}", (Guid id, IJobRepository store, ILoggerFactory lf) =>
        {
            var logger = lf.CreateLogger("JobEndpoints");
            var sw = Stopwatch.StartNew();
            logger.LogInformation("GetJobStarted {JobId}", id);
            var job = store.Get(id);
            if (job == null)
            {
                logger.LogWarning("JobNotFound {JobId}", id);
                return Results.Json(new ErrorResponse("not_found", "job not found"), statusCode: 404);
            }
            var apiPaths = new JobDocument.PathInfo
            {
                Dir = string.Empty,
                Input = ToPublicDoc(job.Id, job.Paths.Input),
                Prompt = ToPublicDoc(job.Id, job.Paths.Prompt),
                Output = ToPublicDoc(job.Id, job.Paths.Output),
                Error = ToPublicDoc(job.Id, job.Paths.Error),
                Markdown = ToPublicDoc(job.Id, job.Paths.Markdown)
            };
            var resp = new JobDetailResponse(job.Id, job.Status, MapDerivedStatus(job.Status), job.Progress, job.Attempts, job.CreatedAt, job.UpdatedAt, job.Metrics, apiPaths, job.ErrorMessage, job.Model, job.TemplateToken, job.Language, job.MarkdownSystemName);
            logger.LogInformation("GetJobCompleted {JobId} {ElapsedMs}", id, sw.ElapsedMilliseconds);
            return Results.Ok(resp);
        })
        .WithName("Jobs_GetById")
        .Produces<JobDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .WithOpenApi(op =>
        {
            op.Responses["200"].Content["application/json"].Example = new OpenApiObject
            {
                ["id"] = new OpenApiString("00000000-0000-0000-0000-000000000000"),
                ["status"] = new OpenApiString("Queued"),
                ["derivedStatus"] = new OpenApiString("Pending"),
                ["progress"] = new OpenApiInteger(0),
                ["attempts"] = new OpenApiInteger(0),
                ["createdAt"] = new OpenApiString("2024-01-01T00:00:00Z"),
                ["updatedAt"] = new OpenApiString("2024-01-01T00:00:00Z"),
                ["metrics"] = new OpenApiObject(),
                ["paths"] = new OpenApiObject
                {
                    ["input"] = new OpenApiObject
                    {
                        ["path"] = new OpenApiString("/api/v1/jobs/00000000-0000-0000-0000-000000000000/files/input.pdf"),
                        ["createdAt"] = new OpenApiString("2024-01-01T00:00:00Z")
                    },
                    ["prompt"] = new OpenApiObject
                    {
                        ["path"] = new OpenApiString("/api/v1/jobs/00000000-0000-0000-0000-000000000000/files/prompt.md"),
                        ["createdAt"] = new OpenApiString("2024-01-01T00:00:00Z")
                    },
                    ["output"] = new OpenApiObject
                    {
                        ["path"] = new OpenApiString("/api/v1/jobs/00000000-0000-0000-0000-000000000000/files/output.json"),
                        ["createdAt"] = new OpenApiString("2024-01-01T00:00:00Z")
                    },
                    ["error"] = new OpenApiObject
                    {
                        ["path"] = new OpenApiString("/api/v1/jobs/00000000-0000-0000-0000-000000000000/files/error.txt"),
                        ["createdAt"] = new OpenApiString("2024-01-01T00:00:00Z")
                    },
                    ["markdown"] = new OpenApiObject
                    {
                        ["path"] = new OpenApiString("/api/v1/jobs/00000000-0000-0000-0000-000000000000/files/markdown.md"),
                        ["createdAt"] = new OpenApiString("2024-01-01T00:00:00Z")
                    }
                },
                ["model"] = new OpenApiString("model"),
                ["templateToken"] = new OpenApiString("template"),
                ["language"] = new OpenApiString("eng"),
                ["markdownSystem"] = new OpenApiString("docling")
            };
            op.Responses["404"].Content["application/json"].Example = new OpenApiObject
            {
                ["error"] = new OpenApiString("not_found"),
                ["message"] = new OpenApiString("job not found")
            };
            return op;
        });

        group.MapDelete("/{id}", (Guid id, IJobRepository store, IUnitOfWork uow, ILoggerFactory lf) =>
        {
            var logger = lf.CreateLogger("JobEndpoints");
            var sw = Stopwatch.StartNew();
            logger.LogInformation("DeleteJobStarted {JobId}", id);
            var job = store.Get(id);
            if (job == null)
            {
                logger.LogWarning("JobNotFound {JobId}", id);
                return Results.Json(new ErrorResponse("not_found", "job not found"), statusCode: 404);
            }
            if (job.Status is "Queued" or "Running")
            {
                store.MarkCancelled(id, "cancelled by user");
                uow.SaveChanges();
                logger.LogWarning("JobCancelled {JobId}", id);
                logger.LogInformation("DeleteJobCompleted {JobId} {ElapsedMs}", id, sw.ElapsedMilliseconds);
                return Results.Accepted($"/api/v1/jobs/{id}", new { ok = true, status = "Cancelled" });
            }
            logger.LogWarning("CancelConflict {JobId} {Status}", id, job.Status);
            return Results.Json(new ErrorResponse("conflict", "job already in terminal state"), statusCode: 409);
        })
        .WithName("Jobs_Delete")
        .Produces(StatusCodes.Status202Accepted)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .WithOpenApi(op =>
        {
            op.Responses["202"].Content ??= new Dictionary<string, OpenApiMediaType>();
            op.Responses["202"].Content["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiObject
                {
                    ["ok"] = new OpenApiBoolean(true),
                    ["status"] = new OpenApiString("Cancelled")
                }
            };
            op.Responses["404"].Content["application/json"].Example = new OpenApiObject
            {
                ["error"] = new OpenApiString("not_found"),
                ["message"] = new OpenApiString("job not found")
            };
            op.Responses["409"].Content["application/json"].Example = new OpenApiObject
            {
                ["error"] = new OpenApiString("conflict"),
                ["message"] = new OpenApiString("job already in terminal state"),
                ["status"] = new OpenApiString("Succeeded")
            };
            return op;
        });

        return builder;
    }

    private static JobDocument.DocumentInfo? ToPublicDoc(Guid id, JobDocument.DocumentInfo? doc)
    {
        if (doc == null || string.IsNullOrEmpty(doc.Path)) return null;
        var full = Path.GetFullPath(doc.Path);
        if (!File.Exists(full)) return null;
        return new JobDocument.DocumentInfo { Path = $"/api/v1/jobs/{id}/files/{Path.GetFileName(full)}", CreatedAt = doc.CreatedAt };
    }

    private static string GetContentType(string fileName) =>
        fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? "application/json" :
        fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? "text/markdown" :
        fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ? "text/plain" :
        "application/octet-stream";

    private static string MapDerivedStatus(string status) => status switch
    {
        "Queued" => "Pending",
        "Running" => "Processing",
        "Succeeded" => "Completed",
        "Failed" => "Failed",
        "Cancelled" => "Cancelled",
        _ => status
    };
}

