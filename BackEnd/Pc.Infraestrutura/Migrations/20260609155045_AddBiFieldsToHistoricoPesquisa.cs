using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class AddBiFieldsToHistoricoPesquisa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LojaId",
                table: "HistoricosPesquisa",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProdutoId",
                table: "HistoricosPesquisa",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosPesquisa_LojaId",
                table: "HistoricosPesquisa",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosPesquisa_ProdutoId",
                table: "HistoricosPesquisa",
                column: "ProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricosPesquisa_Lojas_LojaId",
                table: "HistoricosPesquisa",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricosPesquisa_Produtos_ProdutoId",
                table: "HistoricosPesquisa",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistoricosPesquisa_Lojas_LojaId",
                table: "HistoricosPesquisa");

            migrationBuilder.DropForeignKey(
                name: "FK_HistoricosPesquisa_Produtos_ProdutoId",
                table: "HistoricosPesquisa");

            migrationBuilder.DropIndex(
                name: "IX_HistoricosPesquisa_LojaId",
                table: "HistoricosPesquisa");

            migrationBuilder.DropIndex(
                name: "IX_HistoricosPesquisa_ProdutoId",
                table: "HistoricosPesquisa");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "HistoricosPesquisa");

            migrationBuilder.DropColumn(
                name: "ProdutoId",
                table: "HistoricosPesquisa");
        }
    }
}
