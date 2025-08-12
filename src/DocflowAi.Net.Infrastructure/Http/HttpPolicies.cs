using DocflowAi.Net.Application.Configuration; using Microsoft.Extensions.Options; using Polly; using Polly.Contrib.WaitAndRetry; using Polly.Extensions.Http; using System.Net;
namespace DocflowAi.Net.Infrastructure.Http;
public static class HttpPolicies {
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IOptions<ServicesOptions> opts) {
        var retryCount = Math.Max(0, opts.Value.Markitdown.RetryCount);
        var delay = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(200), retryCount);
        return HttpPolicyExtensions.HandleTransientHttpError().OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests).WaitAndRetryAsync(delay);
    }
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(IOptions<ServicesOptions> opts) {
        var timeout = TimeSpan.FromSeconds(Math.Max(1, opts.Value.Markitdown.TimeoutSeconds)); return Policy.TimeoutAsync<HttpResponseMessage>(timeout);
    }
}
