using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Rag.Core.Domain.DTOs.Auth.Request;
using Rag.Core.Domain.DTOs.Auth.Response;
using Rag.Core.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Rag.App.Services;

public class AuthServiceClient(
    IHttpClientFactory httpClientFactory,
    JsonSerializerOptions jsonOptions,
    IHttpContextAccessor httpContextAccessor) : IAuthService
{
    private HttpClient ApiClient => httpClientFactory.CreateClient("RagApi");
    private HttpClient AppClient => httpClientFactory.CreateClient("RagApp");

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await ApiClient.PostAsJsonAsync("api/auth/register", request, jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthResponse>(jsonOptions)
               ?? throw new InvalidOperationException("Resposta inválida da API.");
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        var response = await AppClient.PostAsJsonAsync("/account/login", request, jsonOptions);

        if (!response.IsSuccessStatusCode)
            return new AuthResponse(false, "Usuário ou senha inválidos.", null);

        return await response.Content.ReadFromJsonAsync<AuthResponse>(jsonOptions)
               ?? new AuthResponse(false, "Falha ao deserializar resposta", null);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh");
        ForwardCookiesFromBrowser(req);
        req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        var response = await ApiClient.SendAsync(req);

        if (!response.IsSuccessStatusCode)
            return new AuthResponse(false, "Refresh falhou.", null);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(jsonOptions);
        if (result?.Success != true || result.Data == null)
            return new AuthResponse(false, "Refresh inválido.", null);

        ForwardCookiesToBrowser(response);
        await SignInWithAccessTokenAsync(result.Data.AccessToken);

        return result;
    }

    public async Task<AuthResponse> LogoutAsync(Guid userId, string? jti = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
        ForwardCookiesFromBrowser(req);

        var accessToken = GetCurrentAccessToken();
        if (!string.IsNullOrEmpty(accessToken))
            req.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        await ApiClient.SendAsync(req);

        await AppClient.PostAsync("/account/logout", null);

        return new AuthResponse(true, "Logout realizado.");
    }

    private async Task SignInWithAccessTokenAsync(string accessToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null || string.IsNullOrEmpty(accessToken)) return;

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(accessToken)) return;

        var jwt = handler.ReadJwtToken(accessToken);
        var claims = jwt.Claims.ToList();
        claims.Add(new Claim("access_token", accessToken));

        var identity = new ClaimsIdentity(claims, "RagAuth");
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync("RagAuth", principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = jwt.ValidTo
        });
    }

    private string? GetCurrentAccessToken() =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue("access_token");

    private void ForwardCookiesToBrowser(HttpResponseMessage apiResponse)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) return;
        if (!apiResponse.Headers.TryGetValues("Set-Cookie", out var cookies)) return;
        foreach (var cookie in cookies)
            httpContext.Response.Headers.Append("Set-Cookie", cookie);
    }

    private void ForwardCookiesFromBrowser(HttpRequestMessage request)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) return;
        var cookieHeader = httpContext.Request.Headers["Cookie"].ToString();
        if (!string.IsNullOrEmpty(cookieHeader))
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
    }
}