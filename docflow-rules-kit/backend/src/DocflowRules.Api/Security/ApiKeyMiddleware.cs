using System.Net;
using Microsoft.Extensions.Primitives;

namespace DocflowRules.Api.Security;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _apiKeys;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration cfg)
    {
        _next = next;
        var keys = cfg.GetSection("Auth:ApiKeys").Get<string[]>() ?? Array.Empty<string>();
        _apiKeys = new HashSet<string>(keys.Where(k => !string.IsNullOrWhiteSpace(k)));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        // Allow swagger and root without key
        if (path.StartsWith("/swagger") || path == "/" )
        {
            await _next(context); return;
        }

        // Protect API and LSP
        if (path.StartsWith("/api") || path.StartsWith("/lsp") || path.StartsWith("/worker"))
        {
            var ok = ValidateKey(context);
            if (!ok)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.Headers["WWW-Authenticate"] = "ApiKey";
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
        }

        await _next(context);
    }

    private bool ValidateKey(HttpContext ctx)
    {
        if (_apiKeys.Count == 0) return true; // if not configured, allow (dev mode)
        if (ctx.Request.Headers.TryGetValue("X-API-Key", out StringValues header))
        {
            if (_apiKeys.Contains(header.ToString())) return true;
        }
        // WebSocket/Browser fallback: query string
        if (ctx.Request.Query.TryGetValue("api_key", out var qv))
        {
            if (_apiKeys.Contains(qv.ToString())) return true;
        }
        return false;
    }
}
