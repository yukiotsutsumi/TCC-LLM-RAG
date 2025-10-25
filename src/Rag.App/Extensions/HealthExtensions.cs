using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rag.App.Endpoints.HealthCheck;

namespace Rag.App.Extensions;

public static class HealthExtensions
{
    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", failureStatus: HealthStatus.Unhealthy, tags: ["db", "postgres"])
            .AddCheck<OllamaHealthCheck>("ollama", failureStatus: HealthStatus.Unhealthy, tags: ["llm", "ollama"])
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["self"]);

        return services;
    }
}