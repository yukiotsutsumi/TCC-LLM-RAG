using Microsoft.Extensions.Options;
using Rag.Core.Interfaces;
using System.Net.Http.Json;

namespace Rag.Infrastructure.Llm
{
    public class OllamaClient(HttpClient http, IOptions<OllamaOptions> opt) : IOllamaClient
    {
        private readonly OllamaOptions _opt = opt.Value;

        public async Task<float[]> EmbedAsync(string model, string text)
        {
            var res = await http.PostAsJsonAsync("/api/embeddings", new { model, prompt = text });
            res.EnsureSuccessStatusCode();
            var obj = await res.Content.ReadFromJsonAsync<EmbedRes>();
            return obj!.Embedding;
        }

        public async Task<string> GenerateAsync(string model, string prompt)
        {
            var body = new { model, prompt, stream = false, options = new { temperature = _opt.Temperature, num_ctx = _opt.NumCtx } };
            var res = await http.PostAsJsonAsync("/api/generate", body);
            res.EnsureSuccessStatusCode();
            var obj = await res.Content.ReadFromJsonAsync<GenRes>();
            return obj!.Response;
        }
        
        private record EmbedRes(float[] Embedding);
        private record GenRes(string Response);
    }
}
