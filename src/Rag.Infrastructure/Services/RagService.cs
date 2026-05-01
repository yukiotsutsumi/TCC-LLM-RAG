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

    // Distância cosine: 0 = idêntico, 2 = oposto
    // Chunks com score acima desse valor são ignorados
    private const double SimilarityThreshold = 0.40;

    // Máximo de trocas do histórico incluídas no prompt
    // (1 troca = 1 mensagem do usuário + 1 do assistente)
    private const int MaxHistoryTurns = 5;

    public async Task<AskResponse> AskAsync(AskRequest request)
    {
        var qEmb = await ollama.EmbedAsync(_opt.EmbeddingModel, request.Question);
        var top  = (await chunks.QueryKnnAsync(qEmb, request.K))
            .Where(t => t.Score <= SimilarityThreshold)
            .ToList();

        var prompt  = BuildPrompt(top, request.Question, request.History);
        var answer  = await ollama.GenerateAsync(_opt.GenerationModel, prompt);
        var sources = top.Count == 0 ? [] : BuildSources(top);

        return new AskResponse { Answer = answer, Sources = sources };
    }

    public async IAsyncEnumerable<StreamPart> AskStreamAsync(
        AskRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var qEmb   = await ollama.EmbedAsync(_opt.EmbeddingModel, request.Question, ct);
        var allTop = (await chunks.QueryKnnAsync(qEmb, request.K, ct: ct)).ToList();

        foreach (var t in allTop)
            Console.WriteLine($">>> Score: {t.Score:F4} | {t.DocumentTitle}");

        var top = allTop.Where(t => t.Score <= SimilarityThreshold).ToList();
        Console.WriteLine($">>> Threshold: {SimilarityThreshold} | Chunks usados: {top.Count}/{allTop.Count}");

        var prompt = BuildPrompt(top, request.Question, request.History);

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

        yield return StreamPart.CreateFinished(top.Count == 0 ? [] : BuildSources(top));
    }

    // ── Prompt ─────────────────────────────────────────────────

    private static string BuildPrompt(
        List<Rag.Core.Domain.Models.KnnResultDto> top,
        string question,
        List<HistoryMessage> history)
    {
        // Sem chunks relevantes — resposta controlada, sem espaço para alucinação
        if (top.Count == 0)
        {
            return $"""
            Responda APENAS a seguinte frase, em PT-BR, sem adicionar nada:
            "Não encontrei informações suficientes nos documentos disponíveis para responder a essa pergunta."
            
            Pergunta: {question}
            """;
        }

        // Com chunks — RAG normal
        var ctxBlock = $"""
        Contexto dos documentos:
        {string.Join("\n", top.Select(t =>
                $"- [{t.DocumentTitle ?? "Doc"}] {Trim(t.Chunk.Content, 600)}"))}
        """;

        var historyBlock = "";
        if (history.Count > 0)
        {
            var recent = history
                .TakeLast(MaxHistoryTurns * 2)
                .Select(m => m.Role == "user"
                    ? $"Usuário: {m.Content}"
                    : $"Assistente: {m.Content}");

            historyBlock = $"""

            Histórico da conversa:
            {string.Join("\n", recent)}
            """;
        }

        return $"""
        Você é um assistente técnico em Segurança da Informação, responda SEMPRE em PT-BR,
        independente do idioma da pergunta.
        Use apenas o contexto dos documentos abaixo. Se não houver informação suficiente,
        diga isso e não invente.
        Considere o histórico da conversa para entender referências como "isso", "ele", "aquilo".

        {ctxBlock}
        {historyBlock}

        Pergunta atual: {question}
        """;
    }

    // ── Helpers ────────────────────────────────────────────────

    private static List<SourceRef> BuildSources(
        IEnumerable<Rag.Core.Domain.Models.KnnResultDto> top) =>
        [.. top
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
            })];

    private static string Trim(string s, int max) =>
        s.Length > max ? s[..max] + "..." : s;
}
