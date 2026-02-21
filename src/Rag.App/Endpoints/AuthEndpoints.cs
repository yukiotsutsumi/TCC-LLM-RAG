using Microsoft.AspNetCore.Authorization;
using Rag.Core.Domain.DTOs.Auth.Request;
using Rag.Core.Interfaces.Services;
using System.Security.Claims;

namespace Rag.App.Endpoints
{
    public static class AuthEndpoints
    {
        private const string RefreshCookieName = "X-Refresh-Token";

        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth").WithTags("Auth").DisableAntiforgery();

            group.MapPost("/login", async (HttpContext context, LoginRequest request, IAuthService auth) =>
            {
                var result = await auth.LoginAsync(request);
                if (!result.Success) return Results.Unauthorized();

                SetRefreshTokenCookie(context, result.Data!.RefreshToken, result.Data.RefreshTokenExpiresAtUtc);

                return Results.Ok(new { result.Data.AccessToken, result.Data.AccessTokenExpiresAtUtc });
            });

            group.MapPost("/refresh", async (HttpContext context, IAuthService auth) =>
            {
                var refreshToken = context.Request.Cookies[RefreshCookieName];
                if (string.IsNullOrEmpty(refreshToken)) return Results.Unauthorized();

                var result = await auth.RefreshAsync(refreshToken);
                if (!result.Success) return Results.Unauthorized();

                SetRefreshTokenCookie(context, result.Data!.RefreshToken, result.Data.RefreshTokenExpiresAtUtc);

                return Results.Ok(new { result.Data.AccessToken, result.Data.AccessTokenExpiresAtUtc });
            });

            group.MapPost("/register", async (RegisterRequest request, IAuthService auth) =>
            {
                var result = await auth.RegisterAsync(request);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            });

            group.MapPost("/logout", async (HttpContext context, IAuthService auth) =>
            {
                var sub = context.User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
                if (!Guid.TryParse(sub, out var userId))
                    return Results.Unauthorized();

                var result = await auth.LogoutAsync(userId);

                context.Response.Cookies.Delete(RefreshCookieName);

                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            }).RequireAuthorization();

            return app;
        }

        private static void SetRefreshTokenCookie(HttpContext context, string token, DateTime expires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = expires
            };
            context.Response.Cookies.Append(RefreshCookieName, token, cookieOptions);
        }
    }
}