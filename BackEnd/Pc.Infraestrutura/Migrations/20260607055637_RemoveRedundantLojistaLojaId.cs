using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantLojistaLojaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Propaga vínculos legados de Lojistas.LojaId para Lojas.LojistaId antes de remover a coluna redundante.
            migrationBuilder.Sql("""
                UPDATE "Lojas" lo
                SET "LojistaId" = lj."Id"
                FROM "Lojistas" lj
                WHERE lj."LojaId" = lo."Id"
                  AND lo."LojistaId" IS NULL
                  AND lj."LojaId" IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "Lojistas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LojaId",
                table: "Lojistas",
                type: "uuid",
                nullable: true);
        }
    }
}
