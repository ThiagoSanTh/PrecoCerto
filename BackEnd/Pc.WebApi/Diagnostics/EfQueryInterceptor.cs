using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Pc.WebApi.Diagnostics;

internal sealed class EfQueryInterceptor : DbCommandInterceptor
{
    private static readonly Regex Sensitive = new(
        @"'(?:password|senha|token|secret|cookie|authorization)[^']*'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Record(command, eventData);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData);
        return new ValueTask<DbDataReader>(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Record(command, eventData);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData);
        return new ValueTask<int>(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Record(command, eventData);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData);
        return new ValueTask<object?>(result);
    }

    private static void Record(DbCommand command, CommandExecutedEventData eventData)
    {
        var trace = BenchmarkRequestContext.Current.Value;
        var ms = eventData.Duration.TotalMilliseconds;
        var ticks = eventData.Duration.Ticks;
        var endpoint = trace?.Endpoint ?? "(no-http)";

        if (trace is not null)
        {
            Interlocked.Add(ref trace.SqlTicks, ticks);
            Interlocked.Increment(ref trace.SqlCount);
        }

        var sample = new SqlSample
        {
            Endpoint = endpoint,
            DurationMs = ms,
            Command = Sanitize(command.CommandText)
        };
        trace?.Samples.Add(sample);
        BenchmarkStats.RecordSql(sample);
    }

    private static string Sanitize(string sql)
    {
        if (string.IsNullOrEmpty(sql)) return string.Empty;
        var collapsed = Regex.Replace(sql, @"\s+", " ").Trim();
        collapsed = Sensitive.Replace(collapsed, "'***'");
        return collapsed.Length <= 800 ? collapsed : collapsed[..800] + "…";
    }
}
