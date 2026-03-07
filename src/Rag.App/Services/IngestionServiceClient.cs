using Rag.Core.Domain.DTOs.Ingest.Requests;
using Rag.Core.Domain.DTOs.Ingest.Responses;
using Rag.Core.Interfaces.Services;
using System.Text.Json;

namespace Rag.App.Services;

public class IngestionServiceClient(HttpClient httpClient, JsonSerializerOptions jsonOptions) : IIngestionService
{
    public async Task<IngestTextResponse> IngestTextAsync(IngestTextRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/ingest", request, jsonOptions, cancellationToken: ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IngestTextResponse>(jsonOptions, ct)
               ?? throw new InvalidOperationException("Resposta inválida da API.");
    }
}