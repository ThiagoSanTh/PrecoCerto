using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmado",
                table: "Lojistas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TokenConfirmacao",
                table: "Lojistas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmado",
                table: "Clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TokenConfirmacao",
                table: "Clientes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmado",
                table: "Lojistas");

            migrationBuilder.DropColumn(
                name: "TokenConfirmacao",
                table: "Lojistas");

            migrationBuilder.DropColumn(
                name: "EmailConfirmado",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TokenConfirmacao",
                table: "Clientes");
        }
    }
}
