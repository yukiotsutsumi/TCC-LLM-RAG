using Pgvector;

namespace Rag.Core.Interfaces
{
    public interface IOllamaClient
    {
        Task<Vector> EmbedAsync(string model, string text, CancellationToken ct = default);
        Task<string> GenerateAsync(string model, string prompt, CancellationToken ct = default);
        Task<string> GenerateStreamAggregatedAsync(string model, string prompt, CancellationToken ct = default);
    }
}
