using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Rag.App.Auth;

public class CustomAuthStateProvider(
    IHttpContextAccessor httpContextAccessor,
    ILogger<CustomAuthStateProvider> logger) : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                logger.LogDebug("Auth state: autenticado como {Name}", user.Identity.Name);
                return Task.FromResult(new AuthenticationState(user));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter authentication state.");
        }

        return Task.FromResult(new AuthenticationState(_anonymous));
    }

    public void MarkUserAsLoggedOut()
    {
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_anonymous)));
    }
}