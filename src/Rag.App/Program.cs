using Microsoft.EntityFrameworkCore;
using Rag.App.Components;
using Rag.App.Endpoints;
using Rag.App.Endpoints.HealthCheck;
using Rag.App.Extensions;
using Rag.App.Middleware;
using Rag.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.ApplyContainerOverrides(builder.Environment);
builder.Services.AddAppServices(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddAppHealthChecks();

builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRequestTimeouts();
builder.Services.AddAppRateLimiting();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseForwardedHeadersForProxy();
app.UseEnvironmentSpecificMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<JtiBlacklistMiddleware>();
app.UseRateLimiter();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseRequestTimeouts();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapMetricsEndpoint();
app.MapHealthEndpoints();
app.MapAskEndpoints();
app.MapAuthEndpoints();
app.MapIngestEndpoints();
app.Run();