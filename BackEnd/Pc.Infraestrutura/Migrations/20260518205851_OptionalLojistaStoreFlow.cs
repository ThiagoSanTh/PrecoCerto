using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class OptionalLojistaStoreFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lojas_Lojistas_LojistaId",
                table: "Lojas");

            migrationBuilder.AlterColumn<Guid>(
                name: "LojaId",
                table: "Lojistas",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "LojistaId",
                table: "Lojas",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Lojas_Lojistas_LojistaId",
                table: "Lojas",
                column: "LojistaId",
                principalTable: "Lojistas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lojas_Lojistas_LojistaId",
                table: "Lojas");

            migrationBuilder.AlterColumn<Guid>(
                name: "LojaId",
                table: "Lojistas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LojistaId",
                table: "Lojas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lojas_Lojistas_LojistaId",
                table: "Lojas",
                column: "LojistaId",
                principalTable: "Lojistas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
