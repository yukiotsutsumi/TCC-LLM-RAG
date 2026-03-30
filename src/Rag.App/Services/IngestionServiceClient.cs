using Rag.Core.Domain.DTOs.Ingest.Requests;
using Rag.Core.Domain.DTOs.Ingest.Responses;
using Rag.Core.Interfaces.Services;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Rag.App.Services;

public class IngestionServiceClient(
    HttpClient httpClient,
    JsonSerializerOptions jsonOptions,
    IHttpContextAccessor httpContextAccessor) : IIngestionService
{
    public async Task<IngestTextResponse> IngestTextAsync(IngestTextRequest request, CancellationToken ct = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/ingest-text")
        {
            Content = JsonContent.Create(request, options: jsonOptions)
        };

        AddBearerToken(httpRequest);

        var response = await httpClient.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IngestTextResponse>(jsonOptions, ct)
               ?? throw new InvalidOperationException("Resposta inválida da API.");
    }

    private void AddBearerToken(HttpRequestMessage request)
    {
        var token = httpContextAccessor.HttpContext?
            .User?.FindFirstValue("access_token");

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }
}