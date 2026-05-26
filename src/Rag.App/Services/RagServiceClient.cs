using Rag.Core.Domain.DTOs.Ask.Requests;
using Rag.Core.Domain.DTOs.ResponseAI;
using Rag.Core.Domain.Enums;
using Rag.Core.Interfaces.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using Rag.App.Auth;
using Microsoft.AspNetCore.Components;

namespace Rag.App.Services;

public class RagServiceClient(
    HttpClient httpClient,
    JsonSerializerOptions jsonOptions,
    IHttpContextAccessor httpContextAccessor,
    CustomAuthStateProvider authStateProvider,
    NavigationManager nav) : IRagService
{
    public async IAsyncEnumerable<StreamPart> AskStreamAsync(
        AskRequest request,
        DocumentAccessLevel accessLevel,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/ask/stream")
        {
            Content = JsonContent.Create(request, options: jsonOptions)
        };

        AddBearerToken(httpRequest);

        using var response = await httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        await HandleUnauthorizedAsync(response);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, bufferSize: 1);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
                yield break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            StreamPart? part;
            try
            {
                part = JsonSerializer.Deserialize<StreamPart>(line, jsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (part is not null)
                yield return part;
        }
    }

    private async Task HandleUnauthorizedAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            authStateProvider.MarkUserAsLoggedOut();
            await Task.Yield();
            nav.NavigateTo("/login", forceLoad: true);
        }
    }

    private void AddBearerToken(HttpRequestMessage request)
    {
        var token = httpContextAccessor.HttpContext?
            .User?.FindFirstValue("access_token");

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
}