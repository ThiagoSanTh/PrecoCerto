using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class AddSessoesChatIA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessoesChat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    UltimaInteracaoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessoesChat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessoesChat_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MensagensChat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessaoChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    Papel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagensChat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagensChat_SessoesChat_SessaoChatId",
                        column: x => x.SessaoChatId,
                        principalTable: "SessoesChat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MensagensChat_SessaoChatId_DataCriacao",
                table: "MensagensChat",
                columns: new[] { "SessaoChatId", "DataCriacao" });

            migrationBuilder.CreateIndex(
                name: "IX_SessoesChat_UsuarioId_UltimaInteracaoEm",
                table: "SessoesChat",
                columns: new[] { "UsuarioId", "UltimaInteracaoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MensagensChat");

            migrationBuilder.DropTable(
                name: "SessoesChat");
        }
    }
}
