using Microsoft.AspNetCore.Components.Authorization;
using Rag.App.Auth;
using Rag.App.Components;
using Rag.App.Endpoints;
using Rag.App.Services;
using Rag.Core.Interfaces.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication("RagAuth")
    .AddCookie("RagAuth", options =>
    {
        options.LoginPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Events.OnRedirectToLogin = context =>
        {
            var isApiRequest = context.Request.Path.StartsWithSegments("/api")
                || context.Request.Headers["Accept"].ToString().Contains("application/json")
                && !context.Request.Headers["Accept"].ToString().Contains("text/html");

            if (isApiRequest)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }

            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<CustomAuthStateProvider>());

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};
builder.Services.AddSingleton(jsonOptions);

builder.Services.AddHttpClient("RagApi", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiUrl"] ?? "https://localhost:65287");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
});

builder.Services.AddHttpClient("RagApp", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AppUrl"] ?? "https://localhost:7269");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
});

builder.Services.AddScoped<IAuthService>(sp =>
    new AuthServiceClient(
        sp.GetRequiredService<IHttpClientFactory>(),
        sp.GetRequiredService<JsonSerializerOptions>(),
        sp.GetRequiredService<IHttpContextAccessor>()
    ));

builder.Services.AddScoped<IRagService>(sp =>
    new RagServiceClient(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("RagApi"),
        sp.GetRequiredService<JsonSerializerOptions>()));

builder.Services.AddScoped<IIngestionService>(sp =>
    new IngestionServiceClient(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("RagApi"),
        sp.GetRequiredService<JsonSerializerOptions>()));

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapLoginEndpoint();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();