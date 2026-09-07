using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Pc.Infraestrutura
{
    /// <summary>
    /// Fábrica de design-time (dotnet ef migrations / database update).
    /// Ordem da connection string:
    /// 1) env ConnectionStrings__DefaultConnection
    /// 2) user-secrets do Pc.WebApi (Development)
    /// 3) placeholder local (só para gerar migration sem tocar no banco)
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        private const string WebApiUserSecretsId = "preco-certo-webapi-dev";

        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? TentarUserSecretsWebApi()
                ?? "Host=localhost;Port=5432;Database=precocerto;Username=postgres;Password=postgres";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString, o => o.UseVector())
                .Options;

            return new AppDbContext(options);
        }

        private static string? TentarUserSecretsWebApi()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var secretsPath = Path.Combine(appData, "Microsoft", "UserSecrets", WebApiUserSecretsId, "secrets.json");
                if (!File.Exists(secretsPath))
                    return null;

                var config = new ConfigurationBuilder()
                    .AddJsonFile(secretsPath, optional: true, reloadOnChange: false)
                    .Build();

                var cs = config.GetConnectionString("DefaultConnection");
                return string.IsNullOrWhiteSpace(cs) ? null : cs;
            }
            catch
            {
                return null;
            }
        }
    }
}
