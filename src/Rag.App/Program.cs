using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Rag.Core.Interfaces;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Interfaces.Services;
using Rag.Infrastructure.Data;
using Rag.Infrastructure.Llm;
using Rag.Infrastructure.Text;
using System;

var builder = WebApplication.CreateBuilder(args);

// Options
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));

// Npgsql DataSource
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
});

// HttpClient para Ollama
builder.Services.AddHttpClient<IOllamaClient, OllamaClient>((sp, client) =>
{
    var opt = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(opt.BaseUrl);
});

// DI
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IChunkRepository, ChunkRepository>();
builder.Services.AddScoped<IChunker, SimpleChunker>();
builder.Services.AddScoped<IRagService, RagService>();

// Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Minimal API: /api/ask
app.MapPost("/api/ask", async (Rag.Core.Domain.DTOs.AskRequest req, IRagService rag) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var res = await rag.AskAsync(req);
    res.TookMs = (int)sw.ElapsedMilliseconds;
    return Results.Ok(res);
});

app.Run();