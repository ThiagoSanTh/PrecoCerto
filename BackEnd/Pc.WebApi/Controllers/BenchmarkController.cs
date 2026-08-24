using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Pc.Infraestrutura;
using Pc.WebApi.Diagnostics;

namespace Pc.WebApi.Controllers;

[ApiController]
[Route("api/health/bench")]
[AllowAnonymous]
[DisableRateLimiting]
public class BenchmarkController : ControllerBase
{
    private readonly AppDbContext _db;

    public BenchmarkController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Snapshot()
    {
        if (!BenchmarkMode.Enabled)
            return NotFound();
        return Ok(BenchmarkStats.Snapshot());
    }

    [HttpPost("reset")]
    public IActionResult Reset()
    {
        if (!BenchmarkMode.Enabled)
            return NotFound();
        BenchmarkStats.Reset();
        return Ok(new { reset = true });
    }

    [HttpGet("db")]
    public async Task<IActionResult> Database(CancellationToken cancellationToken)
    {
        if (!BenchmarkMode.Enabled)
            return NotFound();

        try
        {
            var tables = await QueryRows(
                """
                SELECT relname AS name, n_live_tup AS rows
                FROM pg_stat_user_tables
                ORDER BY n_live_tup DESC
                """,
                cancellationToken);

            var indexes = await QueryRows(
                """
                SELECT tablename, indexname, indexdef
                FROM pg_indexes
                WHERE schemaname = 'public'
                ORDER BY tablename, indexname
                """,
                cancellationToken);

            var activity = await QueryRows(
                """
                SELECT state, COUNT(*)::int AS count
                FROM pg_stat_activity
                WHERE datname = current_database()
                GROUP BY state
                """,
                cancellationToken);

            var settings = await QueryRows(
                """
                SELECT name, setting
                FROM pg_settings
                WHERE name IN (
                    'max_connections',
                    'shared_buffers',
                    'work_mem',
                    'effective_cache_size',
                    'random_page_cost'
                )
                """,
                cancellationToken);

            object? pgStatStatements = null;
            try
            {
                pgStatStatements = await QueryRows(
                    """
                    SELECT LEFT(query, 400) AS query,
                           calls,
                           ROUND(total_exec_time::numeric, 2) AS total_ms,
                           ROUND(mean_exec_time::numeric, 2) AS mean_ms,
                           rows
                    FROM pg_stat_statements
                    WHERE query NOT ILIKE '%pg_stat_statements%'
                    ORDER BY mean_exec_time DESC
                    LIMIT 15
                    """,
                    cancellationToken);
            }
            catch
            {
                pgStatStatements = "extensão pg_stat_statements indisponível";
            }

            return Ok(new { tables, indexes, activity, settings, pgStatStatements });
        }
        catch (Exception)
        {
            return StatusCode(503, new { error = "Falha ao inspecionar PostgreSQL." });
        }
    }

    [HttpGet("explain")]
    public async Task<IActionResult> Explain([FromQuery] string name, CancellationToken cancellationToken)
    {
        if (!BenchmarkMode.Enabled)
            return NotFound();

        var sql = name switch
        {
            "feed" =>
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT p."Id"
                FROM "Produtos" p
                LEFT JOIN "Lojas" l ON p."LojaId" = l."Id"
                ORDER BY p."NomeProduto"
                OFFSET 0 LIMIT 20
                """,
            "feed_count" =>
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT COUNT(*)
                FROM "Produtos" p
                LEFT JOIN "Lojas" l ON p."LojaId" = l."Id"
                """,
            "feed_search" =>
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT p."Id"
                FROM "Produtos" p
                WHERE p."NomeProduto" ILIKE '%arroz%'
                   OR (p."Marca" IS NOT NULL AND p."Marca" ILIKE '%arroz%')
                   OR (p."Descricao" IS NOT NULL AND p."Descricao" ILIKE '%arroz%')
                ORDER BY p."NomeProduto"
                OFFSET 0 LIMIT 20
                """,
            "lojas" =>
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT l."Id"
                FROM "Lojas" l
                LEFT JOIN "Enderecos" e ON l."EnderecoId" = e."Id"
                ORDER BY l."NomeFantasia"
                OFFSET 0 LIMIT 20
                """,
            "mapa" =>
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT l."Id", e."Latitude", e."Longitude"
                FROM "Lojas" l
                INNER JOIN "Enderecos" e ON l."EnderecoId" = e."Id"
                WHERE e."Latitude" IS NOT NULL AND e."Longitude" IS NOT NULL
                """,
            "ofertas_best" =>
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT o."Id", o."ProdutoId", o."Preco"
                FROM "Ofertas" o
                LEFT JOIN "Lojas" l ON o."LojaId" = l."Id"
                WHERE o."Disponivel" = TRUE
                """,
            "avaliacoes_loja" =>
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT a."Id"
                FROM "Avaliacoes" a
                LEFT JOIN "Usuarios" u ON a."ClienteId" = u."Id"
                ORDER BY a."DataAvaliacao" DESC
                LIMIT 200
                """,
            "email_login" =>
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT u."Id"
                FROM "Usuarios" u
                WHERE LOWER(u."Email") = LOWER('bench@example.com') AND u."Ativo" = TRUE
                """,
            _ => null
        };

        if (sql is null)
            return BadRequest(new { error = "name inválido" });

        try
        {
            var plan = new List<string>();
            var conn = _db.Database.GetDbConnection();
            var opened = conn.State != System.Data.ConnectionState.Open;
            if (opened)
                await conn.OpenAsync(cancellationToken);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 30;
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                plan.Add(reader.GetString(0));

            return Ok(new { name, plan = string.Join('\n', plan) });
        }
        catch (Exception)
        {
            return StatusCode(503, new { error = "EXPLAIN falhou." });
        }
    }

    private async Task<List<Dictionary<string, object?>>> QueryRows(string sql, CancellationToken cancellationToken)
    {
        var rows = new List<Dictionary<string, object?>>();
        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }
}
