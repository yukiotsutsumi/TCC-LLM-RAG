using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;

namespace Rag.App.Auth;

public class RevalidatingAuthStateProvider(
    ILoggerFactory loggerFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(30);

    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        var user = authenticationState.User;

        if (user.Identity?.IsAuthenticated != true)
            return Task.FromResult(false);

        var expClaim = user.FindFirstValue("exp");
        if (expClaim != null && long.TryParse(expClaim, out var exp))
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);
            if (expiry < DateTimeOffset.UtcNow)
                return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}