using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Rag.Infrastructure.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? "Development";

            var infraDir = Directory.GetCurrentDirectory();
            var solutionRoot = Path.GetFullPath(Path.Combine(infraDir, ".."));
            var appProjectDir = Path.Combine(solutionRoot, "Rag.App");

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(appProjectDir)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();

            var config = configBuilder.Build();

            var conn = config.GetConnectionString("Postgres")
                      ?? "Host=localhost;Port=5432;Database=ragdb;Username=postgres;Password=postgres";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(conn, o => o.UseVector());

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}