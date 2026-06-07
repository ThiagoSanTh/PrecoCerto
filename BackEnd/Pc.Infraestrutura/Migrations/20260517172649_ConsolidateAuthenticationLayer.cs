using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateAuthenticationLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Usuarios_UsuarioId",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Lojistas_Usuarios_UsuarioId",
                table: "Lojistas");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Lojistas_UsuarioId",
                table: "Lojistas");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_UsuarioId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Lojistas");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Clientes");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Lojistas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NomeUsuario",
                table: "Lojistas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SenhaHash",
                table: "Lojistas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefone",
                table: "Lojistas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Lojistas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoLogin",
                table: "Lojistas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Clientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NomeUsuario",
                table: "Clientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SenhaHash",
                table: "Clientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefone",
                table: "Clientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Clientes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoLogin",
                table: "Clientes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeUsuario = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    Telefone = table.Column<string>(type: "text", nullable: true),
                    UltimoLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    NivelAcesso = table.Column<int>(type: "integer", nullable: false),
                    Permissoes = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Lojistas");

            migrationBuilder.DropColumn(
                name: "NomeUsuario",
                table: "Lojistas");

            migrationBuilder.DropColumn(
                name: "SenhaHash",
                table: "Lojistas");

            migrationBuilder.DropColumn(
                name: "Telefone",
                table: "Lojistas");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Lojistas");

            migrationBuilder.DropColumn(
                name: "UltimoLogin",
                table: "Lojistas");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "NomeUsuario",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "SenhaHash",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Telefone",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "UltimoLogin",
                table: "Clientes");

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Lojistas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Clientes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    NomeUsuario = table.Column<string>(type: "text", nullable: false),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    Telefone = table.Column<string>(type: "text", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    UltimoLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lojistas_UsuarioId",
                table: "Lojistas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_UsuarioId",
                table: "Clientes",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Usuarios_UsuarioId",
                table: "Clientes",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lojistas_Usuarios_UsuarioId",
                table: "Lojistas",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
