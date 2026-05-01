using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Rag.App.Auth;
using Rag.App.Components;
using Rag.App.Endpoints;
using Rag.App.Models;
using Rag.App.Services;
using Rag.Core.Interfaces.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    Console.Error.WriteLine($">>> CRASH: {e.ExceptionObject}");
    try { File.AppendAllText("crash.log", $"[{DateTime.Now}] {e.ExceptionObject}\n\n"); } catch { }
};

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    Console.Error.WriteLine($">>> UNOBSERVED TASK EXCEPTION: {e.Exception}");
    try { File.AppendAllText("crash.log", $"[{DateTime.Now}] UnobservedTask: {e.Exception}\n\n"); } catch { }
    e.SetObserved();
};

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
                || context.Request.Headers.Accept.ToString().Contains("application/json")
                && !context.Request.Headers.Accept.ToString().Contains("text/html");

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
builder.Services.AddMemoryCache();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = builder.Environment.IsDevelopment());
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
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:65287";
    client.BaseAddress = new Uri(baseUrl);

    client.Timeout = TimeSpan.FromMinutes(10);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
});

builder.Services.AddHttpClient("RagApp", client =>
{
    var baseUrl = builder.Configuration["App:BaseUrl"] ?? "https://localhost:7269";
    client.BaseAddress = new Uri(baseUrl);
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

builder.Services.AddScoped<IRagService>(sp => new RagServiceClient(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("RagApi"),
        sp.GetRequiredService<JsonSerializerOptions>(),
        sp.GetRequiredService<IHttpContextAccessor>()
    ));

builder.Services.AddScoped<IIngestionService>(sp =>
    new IngestionServiceClient(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("RagApi"),
        sp.GetRequiredService<JsonSerializerOptions>(),
        sp.GetRequiredService<IHttpContextAccessor>()
    ));

builder.Services.AddScoped<IngestionServiceClient>(sp =>
    new IngestionServiceClient(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("RagApi"),
        sp.GetRequiredService<JsonSerializerOptions>(),
        sp.GetRequiredService<IHttpContextAccessor>()
    ));

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapLoginEndpoint();

app.MapGet("/chat/export", (string token, IMemoryCache cache, HttpContext ctx) =>
{
    Console.WriteLine($">>> Export request: token={token}");
    
    if (ctx.User.Identity?.IsAuthenticated != true)
    {
        Console.WriteLine(">>> Export: não autenticado");
        return Results.Unauthorized();
    }

    if (!cache.TryGetValue<ChatExportEntry>($"chatexport:{token}", out var entry) || entry is null)
    {
        Console.WriteLine($">>> Export: token não encontrado no cache");
        return Results.NotFound();
    }

    cache.Remove($"chatexport:{token}");
    Console.WriteLine($">>> Export: sucesso, arquivo={entry.Filename}");
    
    return Results.File(
        Encoding.UTF8.GetBytes(entry.Content),
        "application/octet-stream",
        entry.Filename);
})
.DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();