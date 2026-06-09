using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pc.Infraestrutura.Migrations
{
    /// <summary>
    /// Unifica as entidades Cliente e Lojista em uma única tabela Usuarios.
    /// Preserva os dados existentes: renomeia Clientes -> Usuarios e migra as
    /// linhas de Lojistas para Usuarios (Papel = Lojista, preservando o Id para
    /// manter o vínculo com Lojas.UsuarioId).
    /// </summary>
    public partial class UnificarUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Remove as FKs que apontavam para Clientes / Lojistas.
            migrationBuilder.DropForeignKey(name: "FK_Avaliacoes_Clientes_ClienteId", table: "Avaliacoes");
            migrationBuilder.DropForeignKey(name: "FK_Carrinhos_Clientes_ClienteId", table: "Carrinhos");
            migrationBuilder.DropForeignKey(name: "FK_Favoritos_Clientes_ClienteId", table: "Favoritos");
            migrationBuilder.DropForeignKey(name: "FK_HistoricosPesquisa_Clientes_ClienteId", table: "HistoricosPesquisa");
            migrationBuilder.DropForeignKey(name: "FK_PreferenciasClientes_Clientes_ClienteId", table: "PreferenciasClientes");
            migrationBuilder.DropForeignKey(name: "FK_Lojas_Lojistas_LojistaId", table: "Lojas");

            // 2) Renomeia a tabela Clientes -> Usuarios (preserva dados e PK).
            migrationBuilder.RenameTable(name: "Clientes", newName: "Usuarios");
            migrationBuilder.Sql(@"ALTER TABLE ""Usuarios"" RENAME CONSTRAINT ""PK_Clientes"" TO ""PK_Usuarios"";");

            // 3) Renomeia a FK de Loja: LojistaId -> UsuarioId.
            migrationBuilder.RenameColumn(name: "LojistaId", table: "Lojas", newName: "UsuarioId");
            migrationBuilder.RenameIndex(name: "IX_Lojas_LojistaId", table: "Lojas", newName: "IX_Lojas_UsuarioId");

            // 4) Novas colunas em Usuarios.
            migrationBuilder.AddColumn<int>(
                name: "Papel", table: "Usuarios", type: "integer", nullable: false, defaultValue: 1);
            migrationBuilder.AddColumn<string>(
                name: "Cargo", table: "Usuarios", type: "text", nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "LojaVinculadaId", table: "Usuarios", type: "uuid", nullable: true);

            // Remove o default técnico usado apenas para preencher linhas existentes.
            migrationBuilder.Sql(@"ALTER TABLE ""Usuarios"" ALTER COLUMN ""Papel"" DROP DEFAULT;");

            // 5) Migra os lojistas existentes para Usuarios (Papel = 2 = Lojista),
            //    preservando o Id para manter o vínculo com Lojas.UsuarioId.
            migrationBuilder.Sql(@"
                INSERT INTO ""Usuarios""
                    (""Id"", ""NomeUsuario"", ""Email"", ""SenhaHash"", ""Telefone"", ""UltimoLogin"",
                     ""Tipo"", ""EmailConfirmado"", ""TokenConfirmacao"", ""LatitudeAtual"", ""LongitudeAtual"",
                     ""DataCriacao"", ""DataAtualizacao"", ""Ativo"", ""Papel"", ""Cargo"", ""LojaVinculadaId"")
                SELECT
                    ""Id"", ""NomeUsuario"", ""Email"", ""SenhaHash"", ""Telefone"", ""UltimoLogin"",
                    2, ""EmailConfirmado"", ""TokenConfirmacao"", NULL, NULL,
                    ""DataCriacao"", ""DataAtualizacao"", ""Ativo"", 2, ""Cargo"", NULL
                FROM ""Lojistas"";");

            // 6) Remove a tabela Lojistas (dados já migrados).
            migrationBuilder.DropTable(name: "Lojistas");

            // 7) Índice e FKs apontando para Usuarios.
            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_LojaVinculadaId", table: "Usuarios", column: "LojaVinculadaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Lojas_LojaVinculadaId", table: "Usuarios", column: "LojaVinculadaId",
                principalTable: "Lojas", principalColumn: "Id", onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Avaliacoes_Usuarios_ClienteId", table: "Avaliacoes", column: "ClienteId",
                principalTable: "Usuarios", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Carrinhos_Usuarios_ClienteId", table: "Carrinhos", column: "ClienteId",
                principalTable: "Usuarios", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Favoritos_Usuarios_ClienteId", table: "Favoritos", column: "ClienteId",
                principalTable: "Usuarios", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricosPesquisa_Usuarios_ClienteId", table: "HistoricosPesquisa", column: "ClienteId",
                principalTable: "Usuarios", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lojas_Usuarios_UsuarioId", table: "Lojas", column: "UsuarioId",
                principalTable: "Usuarios", principalColumn: "Id", onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PreferenciasClientes_Usuarios_ClienteId", table: "PreferenciasClientes", column: "ClienteId",
                principalTable: "Usuarios", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverte: separa novamente Lojistas de Usuarios e renomeia Usuarios -> Clientes.
            migrationBuilder.DropForeignKey(name: "FK_Usuarios_Lojas_LojaVinculadaId", table: "Usuarios");
            migrationBuilder.DropForeignKey(name: "FK_Avaliacoes_Usuarios_ClienteId", table: "Avaliacoes");
            migrationBuilder.DropForeignKey(name: "FK_Carrinhos_Usuarios_ClienteId", table: "Carrinhos");
            migrationBuilder.DropForeignKey(name: "FK_Favoritos_Usuarios_ClienteId", table: "Favoritos");
            migrationBuilder.DropForeignKey(name: "FK_HistoricosPesquisa_Usuarios_ClienteId", table: "HistoricosPesquisa");
            migrationBuilder.DropForeignKey(name: "FK_Lojas_Usuarios_UsuarioId", table: "Lojas");
            migrationBuilder.DropForeignKey(name: "FK_PreferenciasClientes_Usuarios_ClienteId", table: "PreferenciasClientes");

            migrationBuilder.DropIndex(name: "IX_Usuarios_LojaVinculadaId", table: "Usuarios");

            // Recria a tabela Lojistas.
            migrationBuilder.CreateTable(
                name: "Lojistas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Cargo = table.Column<string>(type: "text", nullable: true),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    EmailConfirmado = table.Column<bool>(type: "boolean", nullable: false),
                    NomeUsuario = table.Column<string>(type: "text", nullable: false),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    Telefone = table.Column<string>(type: "text", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    TokenConfirmacao = table.Column<string>(type: "text", nullable: true),
                    UltimoLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lojistas", x => x.Id);
                });

            // Move de volta os usuários com papel Lojista/Vendedor para Lojistas.
            migrationBuilder.Sql(@"
                INSERT INTO ""Lojistas""
                    (""Id"", ""Ativo"", ""Cargo"", ""DataAtualizacao"", ""DataCriacao"", ""Email"",
                     ""EmailConfirmado"", ""NomeUsuario"", ""SenhaHash"", ""Telefone"", ""Tipo"",
                     ""TokenConfirmacao"", ""UltimoLogin"")
                SELECT
                    ""Id"", ""Ativo"", ""Cargo"", ""DataAtualizacao"", ""DataCriacao"", ""Email"",
                    ""EmailConfirmado"", ""NomeUsuario"", ""SenhaHash"", ""Telefone"", 2,
                    ""TokenConfirmacao"", ""UltimoLogin""
                FROM ""Usuarios"" WHERE ""Papel"" = 2;");

            migrationBuilder.Sql(@"DELETE FROM ""Usuarios"" WHERE ""Papel"" = 2;");

            // Remove colunas novas.
            migrationBuilder.DropColumn(name: "Papel", table: "Usuarios");
            migrationBuilder.DropColumn(name: "Cargo", table: "Usuarios");
            migrationBuilder.DropColumn(name: "LojaVinculadaId", table: "Usuarios");

            // Renomeia Usuarios -> Clientes.
            migrationBuilder.Sql(@"ALTER TABLE ""Usuarios"" RENAME CONSTRAINT ""PK_Usuarios"" TO ""PK_Clientes"";");
            migrationBuilder.RenameTable(name: "Usuarios", newName: "Clientes");

            migrationBuilder.RenameColumn(name: "UsuarioId", table: "Lojas", newName: "LojistaId");
            migrationBuilder.RenameIndex(name: "IX_Lojas_UsuarioId", table: "Lojas", newName: "IX_Lojas_LojistaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Avaliacoes_Clientes_ClienteId", table: "Avaliacoes", column: "ClienteId",
                principalTable: "Clientes", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Carrinhos_Clientes_ClienteId", table: "Carrinhos", column: "ClienteId",
                principalTable: "Clientes", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Favoritos_Clientes_ClienteId", table: "Favoritos", column: "ClienteId",
                principalTable: "Clientes", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricosPesquisa_Clientes_ClienteId", table: "HistoricosPesquisa", column: "ClienteId",
                principalTable: "Clientes", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lojas_Lojistas_LojistaId", table: "Lojas", column: "LojistaId",
                principalTable: "Lojistas", principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PreferenciasClientes_Clientes_ClienteId", table: "PreferenciasClientes", column: "ClienteId",
                principalTable: "Clientes", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }
    }
}
