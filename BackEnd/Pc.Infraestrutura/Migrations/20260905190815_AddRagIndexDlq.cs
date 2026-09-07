using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class AddRagIndexDlq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RagIndexDlq",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    EntidadeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Acao = table.Column<int>(type: "integer", nullable: false),
                    Tentativas = table.Column<int>(type: "integer", nullable: false),
                    UltimoErro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReprocessadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagIndexDlq", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RagIndexDlq_CriadoEmUtc",
                table: "RagIndexDlq",
                column: "CriadoEmUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RagIndexDlq_Status",
                table: "RagIndexDlq",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RagIndexDlq_Tipo_EntidadeId",
                table: "RagIndexDlq",
                columns: new[] { "Tipo", "EntidadeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RagIndexDlq");
        }
    }
}
