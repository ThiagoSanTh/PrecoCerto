using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class FixLojaLojistaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lojistas_Lojas_LojaId",
                table: "Lojistas");

            migrationBuilder.DropIndex(
                name: "IX_Lojistas_LojaId",
                table: "Lojistas");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "Usuarios",
                newName: "NomeUsuario");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "Produtos",
                newName: "NomeProduto");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "Categorias",
                newName: "NomeCategoria");

            migrationBuilder.AddColumn<Guid>(
                name: "LojistaId",
                table: "Lojas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Lojas_LojistaId",
                table: "Lojas",
                column: "LojistaId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lojas_Lojistas_LojistaId",
                table: "Lojas",
                column: "LojistaId",
                principalTable: "Lojistas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lojas_Lojistas_LojistaId",
                table: "Lojas");

            migrationBuilder.DropIndex(
                name: "IX_Lojas_LojistaId",
                table: "Lojas");

            migrationBuilder.DropColumn(
                name: "LojistaId",
                table: "Lojas");

            migrationBuilder.RenameColumn(
                name: "NomeUsuario",
                table: "Usuarios",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "NomeProduto",
                table: "Produtos",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "NomeCategoria",
                table: "Categorias",
                newName: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Lojistas_LojaId",
                table: "Lojistas",
                column: "LojaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lojistas_Lojas_LojaId",
                table: "Lojistas",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
