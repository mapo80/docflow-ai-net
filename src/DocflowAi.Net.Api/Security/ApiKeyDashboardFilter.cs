using DocflowAi.Net.Application.Configuration;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DocflowAi.Net.Api.Security;

public class ApiKeyDashboardFilter : IDashboardAuthorizationFilter
{
    private readonly ApiKeyOptions _options;

    public ApiKeyDashboardFilter(IOptions<ApiKeyOptions> options)
    {
        _options = options.Value;
    }

    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var provided = http.Request.Query["api_key"].FirstOrDefault()
                      ?? http.Request.Headers[_options.HeaderName].FirstOrDefault();
        if (_options.Keys == null || _options.Keys.Length == 0) return true;
        if (!string.IsNullOrEmpty(provided) && _options.Keys.Contains(provided))
        {
            return true;
        }
        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false;
    }
}
