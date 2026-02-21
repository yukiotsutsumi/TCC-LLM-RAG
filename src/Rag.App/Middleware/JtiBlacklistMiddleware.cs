using Rag.Core.Interfaces.Repositories;
using System.IdentityModel.Tokens.Jwt;

namespace Rag.App.Middleware;

public class JtiBlacklistMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IRevokedTokenRepository revokedTokenRepo)
    {
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