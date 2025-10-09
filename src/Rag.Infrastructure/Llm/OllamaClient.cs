using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;
using Rag.Core.Interfaces;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rag.Infrastructure.Llm
{
    public class OllamaClient(HttpClient http, IOptions<OllamaOptions> opt, ILogger<OllamaClient> logger) : IOllamaClient
    {
        private readonly OllamaOptions _opt = opt.Value;
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<Vector> EmbedAsync(string model, string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Embedding input vazio.", nameof(text));

            var payload = new { model, prompt = text };
            var content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var res = await http.PostAsync("/api/embeddings", content, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            sw.Stop();

            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("Embeddings falhou: {Status} em {Elapsed} ms. Corpo: {Body}", (int)res.StatusCode, sw.ElapsedMilliseconds, Trunc(body, 500));
                throw new HttpRequestException($"Embeddings {res.StatusCode}: {body}");
            }

            // Tenta formato single: { "embedding": [...] }
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("embedding", out var embProp) && embProp.ValueKind == JsonValueKind.Array)
                {
                    var arr = embProp.EnumerateArray().Select(x => x.GetSingle()).ToArray();
                    if (arr.Length == 0)
                        throw new InvalidOperationException($"Embedding vazio — modelo='{model}', inputLen={text.Length}.");
                    logger.LogDebug("Embedding OK em {Elapsed} ms, dims={Dims}", sw.ElapsedMilliseconds, arr.Length);
                    return new Vector(arr);
                }

                if (doc.RootElement.TryGetProperty("embeddings", out var embsProp) && embsProp.ValueKind == JsonValueKind.Array)
                {
                    var first = embsProp.EnumerateArray().FirstOrDefault();
                    if (first.ValueKind == JsonValueKind.Array)
                    {
                        var arr = first.EnumerateArray().Select(x => x.GetSingle()).ToArray();
                        if (arr.Length > 0)
                        {
                            logger.LogDebug("Embedding (batch) OK em {Elapsed} ms, dims={Dims}", sw.ElapsedMilliseconds, arr.Length);
                            return new Vector(arr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao desserializar embeddings em {Elapsed} ms. Body: {Body}", sw.ElapsedMilliseconds, Trunc(body, 500));
                throw;
            }

            throw new InvalidOperationException($"Resposta de embeddings inesperada do Ollama para modelo '{model}': {Trunc(body, 500)}");
        }

        public async Task<string> GenerateAsync(string model, string prompt, CancellationToken ct = default)
        {
            var payload = new
            {
                model,
                prompt,
                stream = false,
                options = new
                {
                    temperature = _opt.Temperature,
                    num_ctx = _opt.NumCtx
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var res = await http.PostAsync("/api/generate", content, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            sw.Stop();

            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("Generate falhou: {Status} em {Elapsed} ms. Body: {Body}", (int)res.StatusCode, sw.ElapsedMilliseconds, Trunc(body, 800));
                res.EnsureSuccessStatusCode();
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("response", out var respProp))
                {
                    var txt = respProp.GetString() ?? string.Empty;
                    logger.LogInformation("Generate OK em {Elapsed} ms. Tokens aprox promptLen={PromptLen} respLen={RespLen}",
                        sw.ElapsedMilliseconds, prompt.Length, txt.Length);
                    return txt;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao desserializar generate em {Elapsed} ms. Body: {Body}", sw.ElapsedMilliseconds, Trunc(body, 800));
                throw;
            }

            throw new InvalidOperationException("Resposta de geração sem campo 'response'.");
        }

        // Versão alternativa com streaming (caso queira testar latência percebida)
        public async Task<string> GenerateStreamAggregatedAsync(string model, string prompt, CancellationToken ct = default)
        {
            var payload = new
            {
                model,
                prompt,
                stream = true,
                options = new
                {
                    temperature = _opt.Temperature,
                    num_ctx = _opt.NumCtx
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            using var res = await http.PostAsync("/api/generate", content, ct);
            res.EnsureSuccessStatusCode();

            using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);
            var sb = new StringBuilder();

            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("response", out var resp))
                    {
                        sb.Append(resp.GetString());
                    }
                }
                catch
                {
                    // linhas podem conter mensagens de controle; ignore parse inválido
                }
            }

            sw.Stop();
            var final = sb.ToString();
            logger.LogInformation("Generate (stream agregado) OK em {Elapsed} ms. RespLen={Len}", sw.ElapsedMilliseconds, final.Length);
            return final;
        }

        private static string Trunc(string s, int max) => string.IsNullOrEmpty(s) ? s : (s.Length > max ? s[..max] + "..." : s);
    }
}