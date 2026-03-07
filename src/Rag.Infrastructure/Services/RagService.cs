using Rag.Core.Domain.DTOs.Ask.Requests;
using Rag.Core.Domain.DTOs.Ask.Responses;
using Rag.Core.Domain.DTOs.ResponseAI;
using Rag.Core.Interfaces;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Interfaces.Services;
using Rag.Infrastructure.Llm;
using System.Runtime.CompilerServices;

namespace Rag.Infrastructure.Services;

public class RagService(IOllamaClient ollama, IChunkRepository chunks, Microsoft.Extensions.Options.IOptions<OllamaOptions> opt) : IRagService
{
    private readonly OllamaOptions _opt = opt.Value;

    public async Task<AskResponse> AskAsync(AskRequest request)
    {
        // 1) Embed da pergunta
        var qEmb = await ollama.EmbedAsync(_opt.EmbeddingModel, request.Question);

        // 2) Busca vetorial
        var top = (await chunks.QueryKnnAsync(qEmb, request.K)).ToList();

        // 3) Monta contexto
        string ctx;
        if (top.Count == 0)
        {
            ctx = "(sem resultados relacionados)";
        }
        else
        {
            ctx = string.Join("\n", top.Select(t =>
                $"- [{t.DocumentTitle ?? "Doc"}] {Trim(t.Chunk.Content, 600)}"));
        }

        // 4) Prompt
        var prompt = $@"
            Você é um assistente técnico em Segurança da Informação, responda em PT-BR.
            Use apenas o contexto. Se não houver informação suficiente, diga isso e não invente.
            Cite as fontes entre colchetes no final.

            Contexto:
            {ctx}

            Pergunta:
            {request.Question}";

        // 5) Geração
        var answer = await ollama.GenerateAsync(_opt.GenerationModel, prompt);

        // 6) Fontes
        var sources = top.Select(t => new SourceRef
        {
            ChunkId = t.Chunk.Id,
            Title = t.DocumentTitle ?? "Doc",
            Source = t.DocumentSource ?? t.Chunk.DocumentId.ToString(),
            Snippet = Trim(t.Chunk.Content, 300)
        }).ToList();

        return new AskResponse { Answer = answer, Sources = sources };
    }

    public async IAsyncEnumerable<StreamPart> AskStreamAsync(AskRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 1) Embed
        var qEmb = await ollama.EmbedAsync(_opt.EmbeddingModel, request.Question, ct);

        // 2) Busca vetorial
        var top = (await chunks.QueryKnnAsync(qEmb, request.K, ct: ct)).ToList();

        // 3) Monta contexto
        string ctx = top.Count == 0
            ? "(sem resultados relacionados)"
            : string.Join("\n", top.Select(t => $"- [{t.DocumentTitle ?? "Doc"}] {Trim(t.Chunk.Content, 600)}"));

        // 4) Prompt
        var prompt = $@"
        Você é um assistente técnico em Segurança da Informação, responda em PT-BR.
        Use apenas o contexto. Se não houver informação suficiente, diga isso e não invente.
        Cite as fontes entre colchetes no final.

        Contexto:
        {ctx}

        Pergunta:
        {request.Question}";

        // 5) Obter enumerador do stream do LLM
        await using var enumerator = ollama.GenerateStreamAsync(_opt.GenerationModel, prompt, ct).GetAsyncEnumerator(ct);

        while (true)
        {
            bool moved;
            try
            {
                moved = await enumerator.MoveNextAsync();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // cancelado pelo chamador: encerra a iteração silenciosamente
                yield break;
            }

            if (!moved)
                break;

            var delta = enumerator.Current;
            if (!string.IsNullOrEmpty(delta))
            {
                yield return StreamPart.CreateDelta(delta);
            }
        }

        // 6) Ao terminar a geração, monta e envia as fontes como evento final
        var sources = top.Select(t => new SourceRef
        {
            ChunkId = t.Chunk.Id,
            Title = t.DocumentTitle ?? "Doc",
            Source = t.DocumentSource ?? t.Chunk.DocumentId.ToString(),
            Snippet = Trim(t.Chunk.Content, 300)
        }).ToList();

        yield return StreamPart.CreateFinished(sources);
    }

    private static string Trim(string s, int max) => s.Length > max ? s[..max] + "..." : s;
}