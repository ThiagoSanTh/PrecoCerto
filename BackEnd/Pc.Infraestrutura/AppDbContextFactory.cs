using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pc.Infraestrutura
{
    /// <summary>
    /// Fábrica usada apenas em tempo de design (dotnet ef migrations/database).
    /// Evita executar o Program da WebApi (e o MigrateAsync de startup) durante
    /// a geração de migrations. A connection string vem da variável de ambiente
    /// ConnectionStrings__DefaultConnection quando presente, ou de um placeholder
    /// (suficiente para gerar migrations, que não acessam o banco).
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=precocerto;Username=postgres;Password=postgres";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString, o => o.UseVector())
                .Options;

            return new AppDbContext(options);
        }
    }
}
