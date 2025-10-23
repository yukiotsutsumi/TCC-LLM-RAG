using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Rag.App.Components;
using Rag.App.Endpoints;
using Rag.App.Endpoints.HealthCheck;
using Rag.App.Health;
using Rag.Core.Interfaces;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Interfaces.Services;
using Rag.Infrastructure.Data;
using Rag.Infrastructure.Llm;
using Rag.Infrastructure.Text;


var builder = WebApplication.CreateBuilder(args);
static bool RunningInContainer()
{
    try { return File.Exists("/.dockerenv"); } catch { return false; }
}

if (!builder.Environment.IsDevelopment() && RunningInContainer())
{
    var cfg = new ConfigurationBuilder().AddConfiguration(builder.Configuration).Build();

    var conn = cfg.GetConnectionString("Postgres")
               ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=ragdb";
    conn = conn.Replace("Host=localhost", "Host=postgres")
               .Replace("Host=127.0.0.1", "Host=postgres");

    var ollamaBase = (cfg["Ollama:BaseUrl"] ?? "http://localhost:11434")
        .Replace("http://localhost:11434", "http://ollama:11434")
        .Replace("http://127.0.0.1:11434", "http://ollama:11434");

    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:Postgres"] = conn,
        ["Ollama:BaseUrl"] = ollamaBase
    });
}

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("rag-app", serviceVersion: "1.0.0"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri("http://tempo:4317");
            o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        }));

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRequestTimeouts();

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Postgres");
    opt.UseNpgsql(cs, o => o.UseVector());
});
builder.Services.AddDbContextFactory<AppDbContext>();

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));

builder.Services.AddHttpClient<IOllamaClient, OllamaClient>((sp, client) =>
{
    var opt = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(opt.BaseUrl ?? "http://localhost:11434");
    client.Timeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddHttpClient(nameof(OllamaHealthCheck));
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IChunkRepository, ChunkRepository>();
builder.Services.AddScoped<IChunker, SimpleChunker>();
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<IIngestionService, IngestionService>();

builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres", failureStatus: HealthStatus.Unhealthy, tags: new[] { "db", "postgres" })
    .AddCheck<OllamaHealthCheck>("ollama", failureStatus: HealthStatus.Unhealthy, tags: new[] { "llm", "ollama" })
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "self" });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseRequestTimeouts();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapPrometheusScrapingEndpoint("/metrics");
app.MapHealthEndpoints();
app.MapAskEndpoints();
app.MapIngestEndpoints();

app.Run();