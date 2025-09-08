using Rag.Core.Domain.DTOs;
using Rag.Core.Interfaces;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Interfaces.Services;

namespace Rag.Infrastructure.Llm;

public class RagService(IOllamaClient ollama, IChunkRepository chunks, Microsoft.Extensions.Options.IOptions<OllamaOptions> opt) : IRagService
{
    private readonly OllamaOptions _opt = opt.Value;

    public async Task<AskResponse> AskAsync(AskRequest request)
    {
        var qEmb = await ollama.EmbedAsync(_opt.EmbeddingModel, request.Question);
        var top = await chunks.QueryKnnAsync(qEmb, request.K);

        var ctx = string.Join("\n", top.Select(t => $"- [{t.DocTitle ?? "Doc"}] {Trim(t.Chunk.Content, 600)}"));
        var prompt = $@"Você é um assistente técnico em Segurança da Informação, responda em PT-BR.
            Use apenas o contexto. Se não houver informação suficiente, diga isso e não invente.
            Cite as fontes entre colchetes no final.

            Contexto:
            {ctx}

            Pergunta:
            {request.Question}";

        var answer = await ollama.GenerateAsync(_opt.GenerationModel, prompt);
        var sources = top.Select(t => new SourceRef
        {
            ChunkId = t.Chunk.Id,
            Title = t.DocTitle ?? "Doc",
            Source = t.DocSource ?? t.Chunk.DocumentId.ToString(),
            Snippet = Trim(t.Chunk.Content, 300)
        }).ToList();

        return new AskResponse { Answer = answer, Sources = sources };
    }

    private static string Trim(string s, int max) => s.Length > max ? s[..max] + "..." : s;
}