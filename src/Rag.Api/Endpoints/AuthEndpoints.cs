using Rag.Core.Domain.DTOs.Auth.Request;
using Rag.Core.Domain.DTOs.Auth.Response;
using Rag.Core.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Rag.Api.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshCookieName = "X-Refresh-Token";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, IAuthService auth, HttpContext context) =>
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var result = await auth.RegisterAsync(request);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireRateLimiting("register")
        .AllowAnonymous();

        group.MapPost("/login", async (LoginRequest request, IAuthService auth, HttpContext context) =>
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var result = await auth.LoginAsync(request, ip);

            if (!result.Success || result.Data == null || string.IsNullOrEmpty(result.Data.RefreshToken))
                return Results.Unauthorized();

            context.Response.Cookies.Append(RefreshCookieName, result.Data.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = result.Data.RefreshTokenExpiresAtUtc
            });

            return Results.Ok(new AuthResponse(
                Success: true,
                Message: "Login realizado com sucesso.",
                Data: new AuthSuccessResponse(
                    result.Data.AccessToken,
                    result.Data.AccessTokenExpiresAtUtc,
                    string.Empty,
                    result.Data.RefreshTokenExpiresAtUtc
                )
            ));
        })
        .RequireRateLimiting("login")
        .AllowAnonymous();

        group.MapPost("/refresh", async (HttpContext context, IAuthService auth) =>
        {
            var refreshToken = context.Request.Cookies[RefreshCookieName];
            var ip = context.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(refreshToken))
            {
                context.Response.Cookies.Delete(RefreshCookieName);
                return Results.Unauthorized();
            }

            var result = await auth.RefreshAsync(refreshToken, ip);

            if (!result.Success || result.Data == null || string.IsNullOrEmpty(result.Data.RefreshToken))
            {
                context.Response.Cookies.Delete(RefreshCookieName);
                return Results.Unauthorized();
            }

            context.Response.Cookies.Append(RefreshCookieName, result.Data.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = result.Data.RefreshTokenExpiresAtUtc
            });

            return Results.Ok(new AuthResponse(
                Success: true,
                Message: "Token renovado.",
                Data: new AuthSuccessResponse(
                    result.Data.AccessToken,
                    result.Data.AccessTokenExpiresAtUtc,
                    string.Empty,
                    result.Data.RefreshTokenExpiresAtUtc
                )
            ));
        })
        .RequireRateLimiting("refresh")
        .AllowAnonymous();

        group.MapPost("/logout", async (HttpContext context, IAuthService auth) =>
        {
            var sub = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var jti = context.User.FindFirstValue(JwtRegisteredClaimNames.Jti);

            if (!Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            var result = await auth.LogoutAsync(userId, jti);
            context.Response.Cookies.Delete(RefreshCookieName);

            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireAuthorization();

        return app;
    }
}