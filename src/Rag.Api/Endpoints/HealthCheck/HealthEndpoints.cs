using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Rag.Api.Endpoints.HealthCheck
{
    public static class HealthEndpoints
    {
        public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = reg => true,
                ResponseWriter = WriteResponse
            }).RequireAuthorization();

            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = reg => reg.Tags.Contains("self"),
                ResponseWriter = WriteResponse
            }).RequireAuthorization();

            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = reg => true,
                ResponseWriter = WriteResponse
            }).RequireAuthorization();

            return endpoints;
        }

        private static Task WriteResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";
            var payload = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    error = e.Value.Exception?.Message,
                    duration_ms = e.Value.Duration.TotalMilliseconds
                }),
                total_duration_ms = report.TotalDuration.TotalMilliseconds
            };
            return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}