using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Rag.Api.Endpoints.HealthCheck;

public sealed class OllamaHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
            var client = httpClientFactory.CreateClient(nameof(OllamaHealthCheck));
            client.Timeout = TimeSpan.FromSeconds(2);

            using var resp = await client.GetAsync(baseUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return resp.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Status {(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message, ex);
        }
    }
}