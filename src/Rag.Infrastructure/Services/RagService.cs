using Rag.Core.Domain.DTOs.Ask.Requests;
using Rag.Core.Domain.DTOs.Ask.Responses;
using Rag.Core.Domain.DTOs.ResponseAI;
using Rag.Core.Interfaces;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Interfaces.Services;
using Rag.Infrastructure.Llm;
using System.Runtime.CompilerServices;

namespace Rag.Infrastructure.Services;

public class RagService(
    IOllamaClient ollama,
    IChunkRepository chunks,
    Microsoft.Extensions.Options.IOptions<OllamaOptions> opt) : IRagService
{
    private readonly OllamaOptions _opt = opt.Value;

    // Distância cosine — quanto MENOR, mais similar
    // 0.0 = idêntico, 1.0 = sem relação, 2.0 = oposto
    // Threshold de 0.5 significa: só usa chunks com pelo menos 50% de similaridade
    private const double SimilarityThreshold = 0.5;

    public async Task<AskResponse> AskAsync(AskRequest request)
    {
        var qEmb = await ollama.EmbedAsync(_opt.EmbeddingModel, request.Question);
        var top  = (await chunks.QueryKnnAsync(qEmb, request.K))
            .Where(t => t.Score <= SimilarityThreshold)  // filtra por relevância
            .ToList();

        var prompt = top.Count == 0
            ? BuildFallbackPrompt(request.Question)
            : BuildRagPrompt(top, request.Question);

        var answer  = await ollama.GenerateAsync(_opt.GenerationModel, prompt);
        var sources = top.Count == 0 ? [] : BuildSources(top);

        return new AskResponse { Answer = answer, Sources = sources };
    }

    public async IAsyncEnumerable<StreamPart> AskStreamAsync(
        AskRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var qEmb = await ollama.EmbedAsync(_opt.EmbeddingModel, request.Question, ct);
        var top  = (await chunks.QueryKnnAsync(qEmb, request.K, ct: ct))
            .Where(t => t.Score <= SimilarityThreshold)  // filtra por relevância
            .ToList();

        var prompt = top.Count == 0
            ? BuildFallbackPrompt(request.Question)
            : BuildRagPrompt(top, request.Question);

        await using var enumerator = ollama
            .GenerateStreamAsync(_opt.GenerationModel, prompt, ct)
            .GetAsyncEnumerator(ct);

        while (true)
        {
            bool moved;
            try   { moved = await enumerator.MoveNextAsync(); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            { yield break; }

            if (!moved) break;

            var delta = enumerator.Current;
            if (!string.IsNullOrEmpty(delta))
                yield return StreamPart.CreateDelta(delta);
        }

        var sources = top.Count == 0 ? [] : BuildSources(top);
        yield return StreamPart.CreateFinished(sources);
    }

    // ── Prompts ────────────────────────────────────────────────

    // Usado quando nenhum chunk passa no threshold — responde sem RAG
    private static string BuildFallbackPrompt(string question) => $@"
        Você é um assistente técnico. Responda em PT-BR de forma direta e honesta.
        Se não souber a resposta, diga simplesmente que não tem essa informação.
        Não invente. Não mencione documentos ou contexto.

        Pergunta: {question}";

    // Usado quando há chunks relevantes — RAG normal
    private static string BuildRagPrompt(
        IEnumerable<Rag.Core.Domain.Models.KnnResultDto> top,
        string question)
    {
        var ctx = string.Join("\n", top.Select(t =>
            $"- [{t.DocumentTitle ?? "Doc"}] {Trim(t.Chunk.Content, 600)}"));

        return $@"
        Você é um assistente técnico em Segurança da Informação, responda em PT-BR.
        Use apenas o contexto abaixo. Se não houver informação suficiente, diga isso e não invente.
        Cite as fontes entre colchetes no final.

        Contexto:
        {ctx}

        Pergunta: {question}";
    }

    // ── Helpers ────────────────────────────────────────────────

    // Uma fonte por documento — pega o chunk mais relevante (menor Score) de cada doc
    private static List<SourceRef> BuildSources(
        IEnumerable<Rag.Core.Domain.Models.KnnResultDto> top) =>
        top
            .GroupBy(t => t.Chunk.DocumentId)
            .Select(g =>
            {
                var best = g.OrderBy(t => t.Score).First();
                return new SourceRef
                {
                    ChunkId = best.Chunk.Id,
                    Title   = best.DocumentTitle ?? "Doc",
                    Source  = best.DocumentSource ?? best.Chunk.DocumentId.ToString(),
                    Snippet = Trim(best.Chunk.Content, 300)
                };
            })
            .ToList();

    private static string Trim(string s, int max) =>
        s.Length > max ? s[..max] + "..." : s;
}
