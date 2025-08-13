using System.Reflection;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Configuration;
using DocflowAi.Net.Infrastructure.Llm;
using DocflowAi.Net.Infrastructure.Markdown;
using DocflowAi.Net.Infrastructure.Orchestration;
using DocflowAi.Net.Infrastructure.Reasoning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var level = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
var parsed = Enum.TryParse<Serilog.Events.LogEventLevel>(level, true, out var lvl) ? lvl : Serilog.Events.LogEventLevel.Information;
builder.Host.UseSerilog((ctx, lc) => lc
    .MinimumLevel.Is(parsed)
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));

builder.Services.AddAuthentication(ApiKeyDefaults.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DocflowAi.Net.Api.Security.ApiKeyAuthenticationHandler>(ApiKeyDefaults.SchemeName, _ => {});
builder.Services.AddAuthorization();

builder.Services.AddControllers().AddJsonOptions(o => { o.JsonSerializerOptions.PropertyNamingPolicy = null; });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "docflow-ai-net API", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme { Name = "X-API-Key", Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Description = "Provide your API key." });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }, Array.Empty<string>() } });
});

builder.Services.AddProblemDetails(o => { o.IncludeExceptionDetails = (ctx, ex) => builder.Environment.IsDevelopment(); });

builder.Services.AddSingleton<IMarkdownConverter, MarkdownNetConverter>();
builder.Services.AddScoped<IReasoningModeAccessor, ReasoningModeAccessor>();
builder.Services.AddScoped<ILlamaExtractor, LlamaExtractor>();
builder.Services.AddScoped<IProcessingOrchestrator, ProcessingOrchestrator>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseProblemDetails();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();

public partial class Program { }

public static class ApiKeyDefaults { public const string SchemeName = "ApiKey"; }
