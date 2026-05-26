using Microsoft.EntityFrameworkCore;
using Rag.Api.Endpoints;
using Rag.Api.Endpoints.HealthCheck;
using Rag.Api.Extensions;
using Rag.Api.Middleware;
using Rag.App.Extensions;
using Rag.Infrastructure.Data;
using System.Text.Json.Serialization;

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    Console.Error.WriteLine($">>> CRASH: {e.ExceptionObject}");
};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppCors(builder.Configuration);
builder.Configuration.ApplyContainerOverrides(builder.Environment);
builder.Services.AddAppServices(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddAppHealthChecks();
builder.Services.AddIdentityServices(builder.Configuration);
// Policy for admin-only endpoints
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
});
builder.Services.AddAppRateLimiting();
builder.Services.AddRequestTimeouts();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
     db.Database.Migrate();
}

try
{
    await app.Services.SeedIdentityAsync();
}
catch (Exception ex)
{
    Console.WriteLine(">>> ERRO NO SEED IDENTITY");
    Console.WriteLine(ex.ToString());
    throw;
}

app.UseForwardedHeadersForProxy();
app.UseEnvironmentSpecificMiddleware();
app.UseAppCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseMiddleware<JtiBlacklistMiddleware>();

app.MapMetricsEndpoint();
app.MapHealthEndpoints();
app.MapAskEndpoints();
app.MapAuthEndpoints();
app.MapIngestEndpoints();
app.MapDocumentEndpoints(); 
app.UseRequestTimeouts();


app.Run();