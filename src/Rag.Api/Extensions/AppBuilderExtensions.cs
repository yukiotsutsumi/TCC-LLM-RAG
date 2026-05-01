using Microsoft.AspNetCore.HttpOverrides;
using static Rag.App.Extensions.EnvironmentExtensions;

namespace Rag.Api.Extensions;

public static class AppBuilderExtensions
{
    public static IApplicationBuilder UseForwardedHeadersForProxy(this IApplicationBuilder app)
    {
        var fwd = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
        };
        fwd.KnownIPNetworks.Clear();
        fwd.KnownProxies.Clear();
        app.UseForwardedHeaders(fwd);
        return app;
    }

    public static IApplicationBuilder UseEnvironmentSpecificMiddleware(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();

        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
            if (!RunningInContainer())
            {
                app.UseHttpsRedirection();
            }
        }

        return app;
    }
}