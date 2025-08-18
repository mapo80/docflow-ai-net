using System.Reflection;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Configuration;
using DocflowAi.Net.Infrastructure.Llm;
using DocflowAi.Net.Infrastructure.Markdown;
using DocflowAi.Net.Infrastructure.Orchestration;
using DocflowAi.Net.Infrastructure.Reasoning;
using DocflowAi.Net.BBoxResolver;
using DocflowAi.Net.Api.Options;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Services;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.JobQueue.Endpoints;
using DocflowAi.Net.Api.JobQueue.Hosted;
using DocflowAi.Net.Api.Contracts;
using DocflowAi.Net.Api.Model.Abstractions;
using DocflowAi.Net.Api.Model.Repositories;
using DocflowAi.Net.Api.Model.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Hellang.Middleware.ProblemDetails;
using FluentValidation;
using DocflowAi.Net.Api.Model.Endpoints;
using DocflowAi.Net.Api.Templates.Endpoints;
using DocflowAi.Net.Api.Templates.Data;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Threading.RateLimiting;
using DocflowAi.Net.Api.Health;
using Microsoft.OpenApi.Models;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.JobQueue.Repositories;
using DocflowAi.Net.Api.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using DocflowAi.Net.Api.Security;
using System.IO;
using System.Collections.Generic;
using Hangfire.Dashboard;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using DocflowAi.Net.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

var level = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
var parsed = Enum.TryParse<Serilog.Events.LogEventLevel>(level, true, out var lvl) ? lvl : Serilog.Events.LogEventLevel.Information;
if (Log.Logger.GetType().Name == "SilentLogger")
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Is(parsed)
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .CreateLogger();
}
builder.Host.UseSerilog();

builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));
builder.Services.Configure<ResolverOptions>(builder.Configuration.GetSection("Resolver"));
builder.Services.Configure<BBoxOptions>(builder.Configuration.GetSection("BBox"));
builder.Services.PostConfigure<BBoxOptions>(o => builder.Configuration.GetSection("Resolver:TokenFirst").Bind(o));
builder.Services.Configure<PointerOptions>(builder.Configuration.GetSection("Resolver:Pointer"));
builder.Services.Configure<JobQueueOptions>(builder.Configuration.GetSection(JobQueueOptions.SectionName));
builder.Services.Configure<HangfireDashboardAuthOptions>(builder.Configuration.GetSection(HangfireDashboardAuthOptions.SectionName));

builder.Services.AddAuthentication(ApiKeyDefaults.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DocflowAi.Net.Api.Security.ApiKeyAuthenticationHandler>(ApiKeyDefaults.SchemeName, _ => {});
builder.Services.AddAuthorization();
builder.Services.AddHttpClient();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "docflow-ai-net API",
        Version = "v1",
        Description = "Job queue and immediate processing API",
        Contact = new OpenApiContact { Name = "docflow-ai", Url = new Uri("https://github.com/mapo80/docflow-ai-net") },
        License = new OpenApiLicense { Name = "MIT" }
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme { Name = "X-API-Key", Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Description = "Provide your API key." });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }, Array.Empty<string>() } });
});

builder.Services.AddProblemDetails(o => { o.IncludeExceptionDetails = (ctx, ex) => builder.Environment.IsDevelopment(); });

builder.Services.AddDbContext<JobDbContext>((sp, opts) =>
{
    var cfg = sp.GetRequiredService<IOptions<JobQueueOptions>>().Value.Database;
    switch (cfg.Provider.ToLowerInvariant())
    {
        case "inmemory":
            opts.UseInMemoryDatabase(cfg.ConnectionString);
            break;
        default:
            opts.UseSqlite(cfg.ConnectionString);
            break;
    }
});
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<ISecretProtector, SecretProtector>();
builder.Services.AddScoped<IModelRepository, ModelRepository>();
builder.Services.AddScoped<IModelService, ModelService>();
builder.Services.AddScoped<DocflowAi.Net.Api.Templates.Abstractions.ITemplateRepository, DocflowAi.Net.Api.Templates.Repositories.TemplateRepository>();
builder.Services.AddScoped<ITemplateService, DocflowAi.Net.Api.Templates.Services.TemplateService>();
builder.Services.Configure<ModelDownloadOptions>(builder.Configuration.GetSection(ModelDownloadOptions.SectionName));
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<IModelDispatchService, ModelDispatchService>();
builder.Services.AddScoped<IProcessService, ProcessService>();
builder.Services.AddScoped<IJobRunner, JobRunner>();
builder.Services.AddSingleton<IConcurrencyGate, ConcurrencyGate>();
builder.Services.AddHostedService<ReschedulerService>();
builder.Services.AddSingleton<CleanupService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CleanupService>());

builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseMemoryStorage();
});
var workerCount = builder.Configuration.GetSection("JobQueue:Concurrency:HangfireWorkerCount").Get<int>();
if (workerCount > 0)
{
    builder.Services.AddHangfireServer((sp, opts) =>
    {
        var cfg = sp.GetRequiredService<IOptions<JobQueueOptions>>().Value;
        opts.WorkerCount = cfg.Concurrency.HangfireWorkerCount;
    });
}

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = (context, token) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RateLimiter");
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        int retry = 0;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retry = (int)Math.Ceiling(retryAfter.TotalSeconds);
            context.HttpContext.Response.Headers["Retry-After"] = retry.ToString();
        }
        var policy = context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown";
        logger.LogWarning("RateLimited {Policy} {RetryAfterSeconds}", policy, retry);
        return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(new ErrorResponse("rate_limited", null, retry), cancellationToken: token));
    };
    options.AddPolicy("General", context =>
    {
        var cfg = context.RequestServices.GetRequiredService<IOptions<JobQueueOptions>>().Value.RateLimit.General;
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = cfg.PermitPerWindow,
                Window = TimeSpan.FromSeconds(cfg.WindowSeconds),
                QueueLimit = cfg.QueueLimit
            });
    });
    options.AddPolicy("Submit", context =>
    {
        var cfg = context.RequestServices.GetRequiredService<IOptions<JobQueueOptions>>().Value.RateLimit.Submit;
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = cfg.PermitPerWindow,
                Window = TimeSpan.FromSeconds(cfg.WindowSeconds),
                QueueLimit = cfg.QueueLimit
            });
    });
});

builder.Services.AddSingleton<IMarkdownConverter, MarkdownNetConverter>();
builder.Services.AddScoped<IReasoningModeAccessor, ReasoningModeAccessor>();
builder.Services.AddScoped<ILlamaExtractor, LlamaExtractor>();
builder.Services.AddScoped<IProcessingOrchestrator, ProcessingOrchestrator>();
builder.Services.AddSingleton<LegacyBBoxResolver>();
builder.Services.AddSingleton<TokenFirstBBoxResolver>();
builder.Services.AddSingleton<PlainTextViewBuilder>();
builder.Services.AddSingleton<IPointerResolver, PointerResolver>();
builder.Services.AddSingleton<IResolverOrchestrator, ResolverOrchestrator>();
builder.Services.AddHttpClient<ILlmModelService, LlmModelService>();

builder.Services.AddHealthChecks()
    .AddCheck<JobQueueReadyHealthCheck>("jobqueue", tags: new[] { "ready" });

var app = builder.Build();
DefaultJobSeeder.Build(app);
DefaultModelSeeder.Build(app);
DefaultTemplateSeeder.Build(app);

app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (ctx, http) =>
    {
        ctx.Set("RequestId", http.TraceIdentifier);
        ctx.Set("UserAgent", http.Request.Headers["User-Agent"].ToString());
        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ip))
            ctx.Set("ClientIP", ip);
    };
});
app.UseProblemDetails();
app.UseRouting();
app.UseRateLimiter();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
var fs = app.Services.GetRequiredService<IFileSystemService>();
var jqOpts = app.Services.GetRequiredService<IOptions<JobQueueOptions>>().Value;
fs.EnsureDirectory(jqOpts.DataRoot);
if (jqOpts.EnableDashboard)
{
    var dashOpts = app.Services.GetRequiredService<IOptions<HangfireDashboardAuthOptions>>().Value;
    var dashboardOptions = new DashboardOptions();
    if (dashOpts.Enabled)
    {
        var apiOpts = app.Services.GetRequiredService<IOptions<ApiKeyOptions>>();
        dashboardOptions.Authorization = new[] { new ApiKeyDashboardFilter(apiOpts) };
        Log.Information("HangfireDashboardAuthEnabled");
    }
    app.UseHangfireDashboard("/hangfire", dashboardOptions);
}

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = async (ctx, _) =>
        {
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"status\":\"ok\"}");
        }
    }
);
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("ready"),
        ResponseWriter = async (ctx, report) =>
        {
            ctx.Response.ContentType = "application/json";
            var entry = report.Entries.First().Value;
            if (entry.Status == HealthStatus.Healthy)
            {
                await ctx.Response.WriteAsync("{\"status\":\"ok\"}");
            }
            else
            {
                var reasons = entry.Data.TryGetValue("reasons", out var val) && val is IEnumerable<string> arr ? arr.ToList() : new List<string>();
                var status = reasons.Contains("backpressure") ? "backpressure" : "unhealthy";
                await ctx.Response.WriteAsJsonAsync(new { status, reasons });
            }
        }
    }
);
app.MapModelEndpoints();
app.MapModelManagementEndpoints();
app.MapJobEndpoints();
app.MapTemplateEndpoints();
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program { }

public static class ApiKeyDefaults { public const string SchemeName = "ApiKey"; }
