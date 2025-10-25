using Rag.App.Components;
using Rag.App.Endpoints;
using Rag.App.Endpoints.HealthCheck;
using Rag.App.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.ApplyContainerOverrides(builder.Environment);
builder.Services.AddAppServices(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddAppHealthChecks();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRequestTimeouts();

var app = builder.Build();

app.UseForwardedHeadersForProxy();
app.UseEnvironmentSpecificMiddleware();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseRequestTimeouts();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapMetricsEndpoint();
app.MapHealthEndpoints();
app.MapAskEndpoints();
app.MapIngestEndpoints();
app.Run();