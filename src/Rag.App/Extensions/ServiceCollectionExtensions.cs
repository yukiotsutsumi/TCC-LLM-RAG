using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rag.App.Endpoints.HealthCheck;
using Rag.Core.Interfaces;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Interfaces.Services;
using Rag.Infrastructure.Data;
using Rag.Infrastructure.Llm;
using Rag.Infrastructure.Text;

namespace Rag.App.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opt =>
        {
            var cs = configuration.GetConnectionString("Postgres");
            opt.UseNpgsql(cs, o => o.UseVector());
        });

        services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));

        services.AddHttpClient<IOllamaClient, OllamaClient>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(opt.BaseUrl ?? "http://localhost:11434");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        services.AddHttpClient(nameof(OllamaHealthCheck));

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();
        services.AddScoped<IChunker, SimpleChunker>();
        services.AddScoped<IRagService, RagService>();
        services.AddScoped<IIngestionService, IngestionService>();

        services.AddScoped(sp =>
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
        });

        return services;
    }
}