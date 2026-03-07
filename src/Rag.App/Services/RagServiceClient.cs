using Rag.Core.Domain.DTOs.Ask.Requests;
using Rag.Core.Domain.DTOs.Ask.Responses;
using Rag.Core.Domain.DTOs.ResponseAI;
using Rag.Core.Interfaces.Services;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Rag.App.Services;

public class RagServiceClient(HttpClient httpClient, JsonSerializerOptions jsonOptions) : IRagService
{
    public async IAsyncEnumerable<StreamPart> AskStreamAsync(
        AskRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/ask/stream")
        {
            Content = JsonContent.Create(request, options: jsonOptions)
        };

        using var response = await httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var part = JsonSerializer.Deserialize<StreamPart>(line, jsonOptions);
            if (part is not null)
                yield return part;
        }
    }

    public async Task<AskResponse> AskAsync(AskRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/ask", request, jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AskResponse>(jsonOptions)
               ?? throw new InvalidOperationException("Resposta inválida da API.");
    }
}