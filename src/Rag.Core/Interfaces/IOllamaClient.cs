using Pgvector;

namespace Rag.Core.Interfaces
{
    public interface IOllamaClient
    {
        Task<Vector> EmbedAsync(string model, string text);
        Task<string> GenerateAsync(string model, string prompt);
    }
}
