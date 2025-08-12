using System.Security.Claims; using System.Text.Encodings.Web; using DocflowAi.Net.Application.Configuration; using Microsoft.AspNetCore.Authentication; using Microsoft.Extensions.Options;
namespace DocflowAi.Net.Api.Security;
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
    private readonly ApiKeyOptions _opts;
    public ApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IOptions<ApiKeyOptions> apiOpts) : base(options, logger, encoder, clock) { _opts = apiOpts.Value; }
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        if (!Request.Headers.TryGetValue(_opts.HeaderName, out var provided)) return Task.FromResult(AuthenticateResult.Fail("Missing API Key header."));
        if (_opts.Keys.Length == 0) return Task.FromResult(AuthenticateResult.Fail("No API keys configured."));
        if (!_opts.Keys.Contains(provided.ToString())) return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "api-key-user") };
        var identity = new ClaimsIdentity(claims, Scheme.Name); var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
