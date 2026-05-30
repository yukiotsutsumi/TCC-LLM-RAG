using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Rag.App.Extensions;

public static class EnvironmentExtensions
{
    public static bool RunningInContainer()
    {
        try { return File.Exists("/.dockerenv"); } catch { return false; }
    }

    public static void ApplyContainerOverrides(this IConfigurationBuilder builder)
    {
    }

    public static void ApplyContainerOverrides(this IConfiguration configuration, IHostEnvironment env)
    {
        if (!env.IsDevelopment() && RunningInContainer())
        {
            var cfg = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .Build();

            var conn = cfg.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");

            var ollamaBase = cfg.GetRequiredSection("Ollama").GetValue<string>("BaseUrl")
                ?? throw new InvalidOperationException("Ollama:BaseUrl não configurado.");

            conn = conn.Replace("Host=localhost", "Host=postgres")
                       .Replace("Host=127.0.0.1", "Host=postgres");

            ollamaBase = ollamaBase
                .Replace("http://localhost:11434", "http://ollama:11434")
                .Replace("http://127.0.0.1:11434", "http://ollama:11434");
            _ = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = conn,
                ["Ollama:BaseUrl"] = ollamaBase
            };

        }
    }

    public static void ApplyContainerOverrides(this ConfigurationManager configuration, IHostEnvironment env)
    {
        if (!env.IsDevelopment() && RunningInContainer())
        {
            var conn = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");

            var ollamaBase = configuration.GetSection("Ollama").GetValue<string>("BaseUrl")
                ?? throw new InvalidOperationException("Ollama:BaseUrl não configurado.");

            conn = conn.Replace("Host=localhost", "Host=postgres")
                       .Replace("Host=127.0.0.1", "Host=postgres");

            ollamaBase = ollamaBase
                .Replace("http://localhost:11434", "http://ollama:11434")
                .Replace("http://127.0.0.1:11434", "http://ollama:11434");

            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = conn,
                ["Ollama:BaseUrl"] = ollamaBase
            });
        }
    }
}