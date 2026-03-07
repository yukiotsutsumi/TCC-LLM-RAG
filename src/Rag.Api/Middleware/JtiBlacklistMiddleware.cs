using Microsoft.AspNetCore.Http;
using Rag.Core.Interfaces.Repositories;
using System.IdentityModel.Tokens.Jwt;

namespace Rag.Api.Middleware;

public class JtiBlacklistMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IRevokedTokenRepository revokedTokenRepo)
    {

        var path = context.Request.Path.Value;

        if (path is null || !path.StartsWith("/api"))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(jti) && await revokedTokenRepo.IsRevokedAsync(jti))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Token revogado.");
                return;
            }
        }

        await next(context);
    }
}