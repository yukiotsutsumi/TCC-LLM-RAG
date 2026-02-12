using Rag.Core.Domain.DTOs.Auth.Request;
using Rag.Core.Interfaces.Services;

namespace Rag.App.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(request);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request);
            return result.Success ? Results.Ok(result) : Results.Ok(result); // TODO: Retornamos 200 mesmo em erro para tratar no front, ou 401 se preferir
        });

        return app;
    }
}