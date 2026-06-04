using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pc.Infraestrutura;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260604120000_AddImagemUrlToProduto")]
    public partial class AddImagemUrlToProduto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagemUrl",
                table: "Produtos",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagemUrl",
                table: "Produtos");
        }
    }
}
