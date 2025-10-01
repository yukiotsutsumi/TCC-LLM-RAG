using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rag.App.Components;
using Rag.App.Endpoints;
using Rag.Core.Interfaces;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Interfaces.Services;
using Rag.Infrastructure.Data;
using Rag.Infrastructure.Llm;
using Rag.Infrastructure.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRequestTimeouts();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"), o => o.UseVector()));

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));

builder.Services.AddHttpClient<IOllamaClient, OllamaClient>((sp, client) =>
{
    var opt = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(opt.BaseUrl ?? "http://localhost:11434");
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IChunkRepository, ChunkRepository>();
builder.Services.AddScoped<IChunker, SimpleChunker>();
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<IIngestionService, IngestionService>();

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
app.UseRequestTimeouts();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapHealthEndpoints();
app.MapAskEndpoints();
app.MapIngestEndpoints();

app.Run();