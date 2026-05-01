using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Rag.Api.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var tempoEndpoint = configuration["Observability:TempoEndpoint"];
        var tempoEnabled = !string.IsNullOrWhiteSpace(tempoEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("rag-app", serviceVersion: "1.0.0"))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter())
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation()
                 .AddHttpClientInstrumentation();

                if (tempoEnabled)
                {
                    t.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(tempoEndpoint!);
                        o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
                }
            });

        return services;
    }

    public static IEndpointRouteBuilder MapMetricsEndpoint(this IEndpointRouteBuilder endpoints, string path = "/metrics")
    {
        endpoints.MapPrometheusScrapingEndpoint(path);
        return endpoints;
    }
}