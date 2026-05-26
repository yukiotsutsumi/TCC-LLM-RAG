using Microsoft.AspNetCore.Components;
using Rag.App.Auth;
using Rag.Core.Domain.DTOs.Documents.Response;
using Rag.Core.Domain.DTOs.Ingest.Requests;
using Rag.Core.Domain.DTOs.Ingest.Responses;
using Rag.Core.Interfaces.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Rag.App.Services;

public class IngestionServiceClient(
    HttpClient httpClient,
    JsonSerializerOptions jsonOptions,
    IHttpContextAccessor httpContextAccessor,
    CustomAuthStateProvider authStateProvider,
    NavigationManager nav) : IIngestionService
{
    public async Task<IngestTextResponse> IngestTextAsync(
        IngestTextRequest request,
        CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/ingest-text")
        {
            Content = JsonContent.Create(request, options: jsonOptions)
        };

        AddBearerToken(req);

        var response = await httpClient.SendAsync(req, ct);
        await HandleUnauthorizedAsync(response);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IngestTextResponse>(jsonOptions, ct)
               ?? throw new InvalidOperationException("Resposta inválida da API.");
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "api/documents");
        AddBearerToken(req);

        var response = await httpClient.SendAsync(req, ct);
        await HandleUnauthorizedAsync(response);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<DocumentDto>>(jsonOptions, ct) ?? [];
    }

    public async Task<DocumentDto?> GetDocumentAsync(Guid id, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"api/documents/{id}");
        AddBearerToken(req);

        var response = await httpClient.SendAsync(req, ct);
        await HandleUnauthorizedAsync(response);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DocumentDto>(jsonOptions, ct);
    }

    public async Task<bool> DeleteDocumentAsync(Guid id, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/documents/{id}");
        AddBearerToken(req);

        var response = await httpClient.SendAsync(req, ct);
        await HandleUnauthorizedAsync(response);

        return response.IsSuccessStatusCode;
    }

    public async Task<DocumentStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "api/documents/stats");
        AddBearerToken(req);

        var response = await httpClient.SendAsync(req, ct);
        await HandleUnauthorizedAsync(response);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DocumentStatsDto>(jsonOptions, ct)
               ?? new DocumentStatsDto(0, 0);
    }

    public async Task<IngestTextResponse> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string? title,
        string? source,
        CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            ext == ".pdf" ? "application/pdf" : "text/plain");

        content.Add(streamContent, "file", fileName);
        if (!string.IsNullOrEmpty(title)) content.Add(new StringContent(title), "title");
        if (!string.IsNullOrEmpty(source)) content.Add(new StringContent(source), "source");

        var req = new HttpRequestMessage(HttpMethod.Post, "api/upload") { Content = content };
        AddBearerToken(req);

        var response = await httpClient.SendAsync(req, ct);
        await HandleUnauthorizedAsync(response);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IngestTextResponse>(jsonOptions, ct)
               ?? throw new InvalidOperationException("Resposta inválida da API.");
    }

    public async Task<bool> UpdateDocumentAccessLevelAsync(Guid id, Rag.Core.Domain.Enums.DocumentAccessLevel level, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, $"api/documents/{id}/access-level?level={level}");
        AddBearerToken(req);

        var response = await httpClient.SendAsync(req, ct);
        await HandleUnauthorizedAsync(response);

        return response.IsSuccessStatusCode;
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
        var token = httpContextAccessor.HttpContext?.User?.FindFirstValue("access_token");
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}