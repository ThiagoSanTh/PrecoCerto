using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class OtimizacaoPerformanceIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Mensagens_ConversaId\";");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Produtos_LojaId\" ON \"Produtos\" (\"LojaId\");");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Ofertas_ProdutoId\" ON \"Ofertas\" (\"ProdutoId\");");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Mensagens_ConversaId_EnviadaEm\" ON \"Mensagens\" (\"ConversaId\", \"EnviadaEm\");");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Produtos_NomeProduto_trgm\" ON \"Produtos\" USING gin (\"NomeProduto\" gin_trgm_ops);");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Lojas_NomeFantasia_trgm\" ON \"Lojas\" USING gin (\"NomeFantasia\" gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Lojas_NomeFantasia_trgm\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Produtos_NomeProduto_trgm\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Mensagens_ConversaId_EnviadaEm\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Ofertas_ProdutoId\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Produtos_LojaId\";");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Mensagens_ConversaId\" ON \"Mensagens\" (\"ConversaId\");");
        }
    }
}
