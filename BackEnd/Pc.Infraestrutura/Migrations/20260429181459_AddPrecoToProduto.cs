using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class AddPrecoToProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Preco",
                table: "Produtos",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Preco",
                table: "Produtos");
        }
    }
}
