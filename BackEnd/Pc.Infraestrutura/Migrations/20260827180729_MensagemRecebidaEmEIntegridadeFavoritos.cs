using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class MensagemRecebidaEmEIntegridadeFavoritos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "Favoritos" a
                USING "Favoritos" b
                WHERE a."ProdutoId" IS NOT NULL
                  AND a."ProdutoId" = b."ProdutoId"
                  AND a."ClienteId" = b."ClienteId"
                  AND a."Id" > b."Id";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Favoritos_Lojas_LojaId",
                table: "Favoritos");

            migrationBuilder.DropForeignKey(
                name: "FK_Favoritos_Produtos_ProdutoId",
                table: "Favoritos");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Mensagens");

            migrationBuilder.Sql("""
                UPDATE "Mensagens"
                SET "EnviadaEm" = "DataCriacao"
                WHERE "EnviadaEm" IS NULL
                   OR "EnviadaEm" = '-infinity'::timestamptz;
                """);

            migrationBuilder.DropColumn(
                name: "DataCriacao",
                table: "Mensagens");

            migrationBuilder.RenameColumn(
                name: "DataAtualizacao",
                table: "Mensagens",
                newName: "RecebidaEm");

            migrationBuilder.Sql("""
                UPDATE "Mensagens"
                SET "RecebidaEm" = "EnviadaEm"
                WHERE "Lida" = TRUE AND "RecebidaEm" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_ClienteId_ProdutoId",
                table: "Favoritos",
                columns: new[] { "ClienteId", "ProdutoId" },
                unique: true,
                filter: "\"ProdutoId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Favoritos_Lojas_LojaId",
                table: "Favoritos",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Favoritos_Produtos_ProdutoId",
                table: "Favoritos",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Favoritos_Lojas_LojaId",
                table: "Favoritos");

            migrationBuilder.DropForeignKey(
                name: "FK_Favoritos_Produtos_ProdutoId",
                table: "Favoritos");

            migrationBuilder.DropIndex(
                name: "IX_Favoritos_ClienteId_ProdutoId",
                table: "Favoritos");

            migrationBuilder.RenameColumn(
                name: "RecebidaEm",
                table: "Mensagens",
                newName: "DataAtualizacao");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Mensagens",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataCriacao",
                table: "Mensagens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_Favoritos_Lojas_LojaId",
                table: "Favoritos",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Favoritos_Produtos_ProdutoId",
                table: "Favoritos",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id");
        }
    }
}
