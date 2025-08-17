using System.Net;
using Microsoft.Extensions.Primitives;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _apiKeys;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration cfg)
    {
        _next = next;
        var keys = cfg.GetSection("Auth:WorkerKeys").Get<string[]>() ?? Array.Empty<string>();
        _apiKeys = new HashSet<string>(keys.Where(k => !string.IsNullOrWhiteSpace(k)));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/swagger") || path == "/") { await _next(context); return; }

        var ok = ValidateKey(context);
        if (!ok)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.Headers["WWW-Authenticate"] = "ApiKey";
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
        await _next(context);
    }

    private bool ValidateKey(HttpContext ctx)
    {
        if (_apiKeys.Count == 0) return true; // dev
        if (ctx.Request.Headers.TryGetValue("X-API-Key", out StringValues v) && _apiKeys.Contains(v.ToString())) return true;
        if (ctx.Request.Query.TryGetValue("api_key", out var q) && _apiKeys.Contains(q.ToString())) return true;
        return false;
    }
}
