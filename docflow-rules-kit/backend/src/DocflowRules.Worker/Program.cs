using System.Text.Json.Nodes;
using DocflowRules.Sdk;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, cfg) => cfg.WriteTo.Console());
builder.Services.AddDocflowRulesCore();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder.CreateDefault().AddService("docflow-worker"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("DocflowRules")
        .AddOtlpExporter())
    .WithMetrics(m => m
        .SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder.CreateDefault().AddService("docflow-worker"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseSerilogRequestLogging();

app.MapGet("/health", () => Results.Ok("ok"));

app.UseMiddleware<ApiKeyMiddleware>();

app.MapPost("/compile", async (IScriptRunner runner, HttpContext ctx, CompileRequest req, CancellationToken ct) =>
{
    var (ok, errors) = await runner.CompileAsync(req.Code, ct);
    return Results.Ok(new { ok, errors });
});
app.MapPost("/run", async (IScriptRunner runner, RunRequest req, CancellationToken ct) =>
{
    var input = req.Input ?? new JsonObject();
    var (before, after, mutations, durationMs, logs) = await runner.RunAsync(req.Code, input, ct);
    return Results.Ok(new { before, after, mutations, durationMs, logs });
});

app.Run("http://127.0.0.1:5095");

public record CompileRequest(string Code);
public record RunRequest(string Code, JsonObject? Input);
