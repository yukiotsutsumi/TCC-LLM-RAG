using Microsoft.Extensions.Options;
using Pgvector;
using Rag.Core.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rag.Infrastructure.Llm
{
    public class OllamaClient(HttpClient http, IOptions<OllamaOptions> opt) : IOllamaClient
    {
        private readonly OllamaOptions _opt = opt.Value;

    public async Task<Vector> EmbedAsync(string model, string text)  
    {  
        if (string.IsNullOrWhiteSpace(text))  
            throw new ArgumentException("Embedding input vazio.", nameof(text));  
    
        var body = new { model, prompt = text };  
        var res = await http.PostAsJsonAsync("/api/embeddings", body);  
        var content = await res.Content.ReadAsStringAsync();  
    
        if (!res.IsSuccessStatusCode)  
            throw new HttpRequestException($"Embeddings {res.StatusCode}: {content}");  
    
        var jsonOpt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };  
    
        // Tenta formato single: { "embedding": [...] }  
        try  
        {  
            var one = JsonSerializer.Deserialize<EmbedRes>(content, jsonOpt);  
            if (one?.Embedding is { Length: > 0 }) return new Vector(one.Embedding);  
            if (one?.Embedding is { Length: 0 })  
                throw new InvalidOperationException($"Embedding vazio — modelo='{model}', inputLen={text.Length}.");  
        }  
        catch { /* continua */ }  
    
        // Tenta formato batch: { "embeddings": [[...], ...] }  
        try  
        {  
            var multi = JsonSerializer.Deserialize<EmbedResV2>(content, jsonOpt);  
            if (multi?.Embeddings is { Length: > 0 } && multi.Embeddings[0] is { Length: > 0 })  
                return new Vector(multi.Embeddings[0]);  
        }  
        catch { /* continua */ }  
    
        throw new InvalidOperationException($"Resposta de embeddings inesperada do Ollama para modelo '{model}': {content}");  
    }  
    
    private record EmbedRes([property: JsonPropertyName("embedding")] float[] Embedding);  
    private record EmbedResV2([property: JsonPropertyName("embeddings")] float[][] Embeddings);

        public async Task<string> GenerateAsync(string model, string prompt)
        {
            var body = new { model, prompt, stream = false, options = new { temperature = _opt.Temperature, num_ctx = _opt.NumCtx } };
            var res = await http.PostAsJsonAsync("/api/generate", body);
            res.EnsureSuccessStatusCode();
            var obj = await res.Content.ReadFromJsonAsync<GenRes>();
            return obj!.Response;
        }
        
        private record GenRes(string Response);
    }
}
