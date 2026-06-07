using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pc.Infraestrutura;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260519120000_AddLojaIdToProduto")]
    public partial class AddLojaIdToProduto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LojaId",
                table: "Produtos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_LojaId",
                table: "Produtos",
                column: "LojaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_Lojas_LojaId",
                table: "Produtos",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_Lojas_LojaId",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_LojaId",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "Produtos");
        }
    }
}
