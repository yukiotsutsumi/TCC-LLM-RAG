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
        try
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                _cachedUser = user;
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
