using Pgvector;
using System.Runtime.CompilerServices;

namespace Rag.Core.Interfaces
{
    public interface IOllamaClient
    {
        Task<Vector> EmbedAsync(string model, string text, CancellationToken ct = default);
        Task<string> GenerateAsync(string model, string prompt, CancellationToken ct = default);
        IAsyncEnumerable<string> GenerateStreamAsync(string model, string prompt, CancellationToken ct = default);
    }
}
