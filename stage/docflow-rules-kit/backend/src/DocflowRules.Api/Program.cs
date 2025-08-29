using System.Text.Json.Serialization;
using DocflowRules.Api.Services;
using DocflowRules.Sdk;
using DocflowRules.Storage.EF;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
);

// DbContext (multi-provider: for now only SQLite)
var provider = builder.Configuration["Database:Provider"] ?? "sqlite";
var cs = builder.Configuration.GetConnectionString("db") ?? builder.Configuration["Database:ConnectionString"] ?? "Data Source=docflow.db";
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    switch (provider.ToLowerInvariant())
    {
        case "sqlite":
            opt.UseSqlite(cs);
            break;
        // case "postgres":
        //     opt.UseNpgsql(cs);
        //     break;
        // case "sqlserver":
        //     opt.UseSqlServer(cs);
        //     break;
        default:
            throw new NotSupportedException($"Database provider '{provider}' non supportato.");
    }
});

// Controllers & JSON
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.Configure<JsonOptions>(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Docflow Rules API", Version = "v1" });
    c.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearer" } },
            new string[] {}
        }
    });
});

// AuthN/AuthZ (OIDC + Local JWT)
var localIssuer = builder.Configuration["Auth:Local:Issuer"] ?? "docflow-local";
var localAudience = builder.Configuration["Auth:Local:Audience"] ?? "docflow-ui";
var localKey = builder.Configuration["Auth:Local:SigningKey"] ?? "dev-signing-key-change-me-please-please";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Bearer";
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
})
.AddPolicyScheme("Bearer", "Bearer", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var auth = context.Request.Headers["Authorization"].ToString();
        var source = context.Request.Headers["X-Auth-Source"].ToString();
        if (source.Equals("local", StringComparison.OrdinalIgnoreCase)) return "Local";
        if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = auth.Substring("Bearer ".Length).Trim();
            try
            {
                var parts = token.Split('.');
                if (parts.Length >= 2)
                {
                    string payload = parts[1];
                    // base64url decode
                    payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=').Replace('-', '+').Replace('_', '/');
                    var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("iss", out var iss) && iss.GetString() == localIssuer)
                        return "Local";
                }
            } catch { }
        }
        return "OIDC";
    };
})
.AddJwtBearer("Local", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateIssuerSigningKey = true, ValidateLifetime = true,
        ValidIssuer = localIssuer, ValidAudience = localAudience, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(localKey))
    };
    options.RequireHttpsMetadata = false;
})
.AddJwtBearer("OIDC", options =>
{
    options.Authority = builder.Configuration["Auth:Authority"];
    options.Audience = builder.Configuration["Auth:Audience"];
    options.RequireHttpsMetadata = false;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("viewer", p => p.RequireAuthenticatedUser().RequireRole("viewer","editor","reviewer","admin"));
    options.AddPolicy("editor", p => p.RequireAuthenticatedUser().RequireRole("editor","reviewer","admin"));
    options.AddPolicy("reviewer", p => p.RequireAuthenticatedUser().RequireRole("reviewer","admin"));
    options.AddPolicy("admin", p => p.RequireAuthenticatedUser().RequireRole("admin"));
});
    options.AddPolicy("editor", p => p.RequireAuthenticatedUser().RequireRole("editor","reviewer","admin"));
    options.AddPolicy("reviewer", p => p.RequireAuthenticatedUser().RequireRole("reviewer","admin"));
    options.AddPolicy("admin", p => p.RequireAuthenticatedUser().RequireRole("admin"));
});

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tp => {
        tp
            .SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder.CreateDefault().AddService("docflow-api"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("DocflowRules.LLM")
            .AddOtlpExporter();
    })
    .WithMetrics(mp => {
        mp
            .SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder.CreateDefault().AddService("docflow-api"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("DocflowRules.LLM")
            .AddRuntimeInstrumentation()
            .AddOtlpExporter();
    });

// CORS (dev friendly)
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Health checks
builder.Services.AddHealthChecks();

// Domain services
builder.Services.AddScoped<SuggestionService>();
builder.Services.AddScoped<ILlmConfigService, LlmConfigService>();
builder.Services.AddScoped<IFuzzService, FuzzService>();
builder.Services.AddScoped<IPropertyTestService, PropertyTestService>();
builder.Services.AddSingleton<IGgufService, GgufService>();
builder.Services.AddHttpClient("huggingface");
builder.Services.AddSingleton<ILLMProviderRegistry, LlmProviderRegistry>();
builder.Services.AddSingleton<DocflowRules.Api.LLM.OpenAiProvider>();
builder.Services.AddSingleton<DocflowRules.Api.LLM.LlamaSharpProvider>();
builder.Services.AddSingleton<DocflowRules.Api.Services.MockLLMProvider>();
builder.Services.AddHostedService<DocflowRules.Api.Services.LlmWarmupHostedService>();
builder.Services.AddHostedService<DocflowRules.Api.Services.AdminSeedHostedService>();
builder.Services.AddHostedService<DocflowRules.Api.Services.GgufService>();

// Validators
builder.Services.AddScoped<DocflowRules.Api.Validation.TestUpsertValidator>();

var app = builder.Build();

// Migrate DB (dev convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Seed admin user/role
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
    await DocflowRules.Api.Services.SeedData.RunAsync(db, cfg, log);
}

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
