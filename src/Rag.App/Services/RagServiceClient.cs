using Microsoft.AspNetCore.Http;
using Rag.Core.Domain.DTOs.Ask.Requests;
using Rag.Core.Domain.DTOs.Ask.Responses;
using Rag.Core.Domain.DTOs.ResponseAI;
using Rag.Core.Interfaces.Services;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;

namespace Rag.App.Services;

public class RagServiceClient(
    HttpClient httpClient,
    JsonSerializerOptions jsonOptions,
    IHttpContextAccessor httpContextAccessor) : IRagService
{
    public async IAsyncEnumerable<StreamPart> AskStreamAsync(
        AskRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/ask/stream")
        {
            Content = JsonContent.Create(request, options: jsonOptions)
        };
        AddBearerToken(httpRequest);

        using var response = await httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead, // não espera o body inteiro
            ct);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        // Buffer de 1 byte — desativa o buffer interno do StreamReader
        // Sem isso o StreamReader acumula chunks e entrega tudo de uma vez
        using var reader = new StreamReader(stream, bufferSize: 1);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            StreamPart? part = null;
            try
            {
                part = JsonSerializer.Deserialize<StreamPart>(line, jsonOptions);
            }
            catch (JsonException)
            {
                // linha malformada — ignora e continua
                continue;
            }

            if (part is not null)
                yield return part;
        }
    }

    public async Task<AskResponse> AskAsync(AskRequest request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/ask")
        {
            Content = JsonContent.Create(request, options: jsonOptions)
        };
        AddBearerToken(httpRequest);

        var response = await httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AskResponse>(jsonOptions)
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