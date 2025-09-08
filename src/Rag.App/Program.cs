using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rag.App.Components;
using Rag.Core.Interfaces;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Interfaces.Services;
using Rag.Infrastructure.Data;
using Rag.Infrastructure.Llm;
using Rag.Infrastructure.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddHttpClient<IOllamaClient, OllamaClient>((sp, client) =>
{
    var opt = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(opt.BaseUrl);
});

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IChunkRepository, ChunkRepository>();
builder.Services.AddScoped<IChunker, SimpleChunker>();
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapPost("/api/ask", async (Rag.Core.Domain.DTOs.AskRequest req, IRagService rag) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var res = await rag.AskAsync(req);
    res.TookMs = (int)sw.ElapsedMilliseconds;
    return Results.Ok(res);
});

// Ping simples
app.MapGet("/api/ping", () => Results.Ok(new { ok = true, time = DateTime.UtcNow }));

app.MapPost("/api/ask-demo", (Rag.Core.Domain.DTOs.AskRequest req) =>
{
    var answer = $"[demo] Você perguntou: \"{req.Question}\". K={req.K}, MaxCtx={req.MaxContextTokens}.";
    return Results.Ok(new Rag.Core.Domain.DTOs.AskResponse
    {
        Answer = answer,
        Sources = [],
        TookMs = 1
    });
});

app.Run();