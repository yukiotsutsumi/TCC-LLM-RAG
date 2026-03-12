using Microsoft.AspNetCore.Authentication;
using Rag.Core.Domain.DTOs.Auth.Request;
using Rag.Core.Domain.DTOs.Auth.Response;
using Rag.Core.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Rag.App.Endpoints;

public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/account/login", async (
            HttpContext ctx,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            JsonSerializerOptions jsonOptions) =>
        {
            LoginRequest? request;
            try
            {
                request = await ctx.Request.ReadFromJsonAsync<LoginRequest>(jsonOptions);
            }
            catch
            {
                return Results.BadRequest("Payload inválido.");
            }

            if (request == null)
                return Results.BadRequest("Payload inválido.");

            var httpClient = httpClientFactory.CreateClient("RagApi");
            var apiResponse = await httpClient.PostAsJsonAsync("api/auth/login", request, jsonOptions);

            if (!apiResponse.IsSuccessStatusCode)
                return Results.Unauthorized();

            var result = await apiResponse.Content.ReadFromJsonAsync<AuthResponse>(jsonOptions);

            if (result?.Success != true || result.Data == null)
                return Results.Unauthorized();

            if (apiResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
                foreach (var cookie in cookies)
                    ctx.Response.Headers.Append("Set-Cookie", cookie);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.Data.AccessToken);
            var claims = jwt.Claims.ToList();
            claims.Add(new Claim("access_token", result.Data.AccessToken));

            var identity = new ClaimsIdentity(claims, "RagAuth");
            var principal = new ClaimsPrincipal(identity);

            await ctx.SignInAsync("RagAuth", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = jwt.ValidTo
            });

            return Results.Ok(result);
        })
        .AllowAnonymous();

        app.MapPost("/account/logout", async (HttpContext ctx) =>
        {
            Console.WriteLine(">>> LOGOUT ENDPOINT CHAMADO");
            Console.WriteLine($">>> Cookies recebidos: {string.Join(", ", ctx.Request.Cookies.Keys)}");

            ctx.Response.Cookies.Delete("X-Refresh-Token");
            await ctx.SignOutAsync("RagAuth");

            Console.WriteLine(">>> SignOut executado");
            return Results.Ok();
        })
        .AllowAnonymous();

        app.MapGet("/account/check-auth", (HttpContext ctx) =>
        {
            return ctx.User.Identity?.IsAuthenticated == true
                ? Results.Ok()
                : Results.Unauthorized();
        })
        .AllowAnonymous();

        return app;
    }
}