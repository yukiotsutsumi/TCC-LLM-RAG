using Rag.Core.Domain.DTOs.Ingest.Requests;
using Rag.Core.Domain.DTOs.Ingest.Responses;

namespace Rag.Core.Interfaces.Services;

// Responsabilidade única: processar e indexar texto/documentos
// Gerenciamento de documentos (listar, deletar, stats) é feito
// pelos endpoints diretamente via IDocumentRepository
public interface IIngestionService
{
    Task<IngestTextResponse> IngestTextAsync(IngestTextRequest request, CancellationToken ct = default);
}
