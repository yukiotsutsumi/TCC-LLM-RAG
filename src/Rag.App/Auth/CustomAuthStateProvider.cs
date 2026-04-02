using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Rag.App.Auth;

public class CustomAuthStateProvider(
    IHttpContextAccessor httpContextAccessor,
    ILogger<CustomAuthStateProvider> logger) : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal? _cachedUser;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // HttpContext is only available during the initial HTTP request.
        // In Blazor Server interactive mode it becomes null after the circuit
        // is established, so we cache the principal on first successful read.
        try
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                _cachedUser = user;
                logger.LogDebug("Auth state: autenticado como {Name}", user.Identity.Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter authentication state.");
        }

        var principal = _cachedUser ?? _anonymous;
        return Task.FromResult(new AuthenticationState(principal));
    }

    public void MarkUserAsLoggedOut()
    {
        _cachedUser = null;
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_anonymous)));
    }
}
