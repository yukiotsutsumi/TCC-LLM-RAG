namespace Rag.Core.Interfaces
{
    public interface IOllamaClient
    {
        Task<float[]> EmbedAsync(string model, string text);
        Task<string> GenerateAsync(string model, string prompt);
    }
}
