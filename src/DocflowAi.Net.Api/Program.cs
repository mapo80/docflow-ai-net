using System.Reflection;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Configuration;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Infrastructure.Llm;
using DocflowAi.Net.Infrastructure.Markitdown;
using DocflowAi.Net.Infrastructure.Orchestration;
using DocflowAi.Net.Infrastructure.Http;
using DocflowAi.Net.Infrastructure.Reasoning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext());

builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));
builder.Services.Configure<ServicesOptions>(builder.Configuration.GetSection(ServicesOptions.SectionName));
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));
builder.Services.Configure<ExtractionProfilesOptions>(builder.Configuration.GetSection(ExtractionProfilesOptions.SectionName));

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

builder.Services.AddHttpClient<IMarkitdownClient, MarkitdownClient>()
    .AddPolicyHandler((sp,_) => HttpPolicies.GetRetryPolicy(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServicesOptions>>()))
    .AddPolicyHandler((sp,_) => HttpPolicies.GetTimeoutPolicy(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServicesOptions>>()));

builder.Services.AddScoped<IReasoningModeAccessor, ReasoningModeAccessor>();
builder.Services.AddSingleton<ILlamaExtractor, LlamaExtractor>();
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

public static class ApiKeyDefaults { public const string SchemeName = "ApiKey"; }
