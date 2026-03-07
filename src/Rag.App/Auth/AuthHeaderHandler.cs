using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Net.Http.Headers;

namespace Rag.App.Components.Auth
{
    public class AuthHeaderHandler(ProtectedLocalStorage localStorage) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await localStorage.GetAsync<string>("access_token");
            if (token.Success && !string.IsNullOrEmpty(token.Value))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
