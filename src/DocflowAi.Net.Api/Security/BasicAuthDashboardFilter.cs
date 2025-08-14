using System.Text;
using Hangfire.Dashboard;
using Microsoft.Extensions.Options;
using DocflowAi.Net.Api.Options;

namespace DocflowAi.Net.Api.Security;

public class BasicAuthDashboardFilter : IDashboardAuthorizationFilter
{
    private readonly HangfireDashboardAuthOptions _options;

    public BasicAuthDashboardFilter(IOptions<HangfireDashboardAuthOptions> options)
    {
        _options = options.Value;
    }

    public bool Authorize(DashboardContext context)
    {
        if (!_options.Enabled)
            return true;
        var http = context.GetHttpContext();
        var header = http.Request.Headers["Authorization"].FirstOrDefault();
        if (header != null && header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            var encoded = header.Substring("Basic ".Length).Trim();
            var cred = Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Split(':', 2);
            if (cred.Length == 2 && cred[0] == _options.Username && cred[1] == _options.Password)
                return true;
        }
        http.Response.StatusCode = 401;
        http.Response.Headers["WWW-Authenticate"] = "Basic realm=Hangfire";
        return false;
    }
}
