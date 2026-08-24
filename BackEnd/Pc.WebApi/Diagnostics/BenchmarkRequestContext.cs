using System.Collections.Concurrent;

namespace Pc.WebApi.Diagnostics;

internal sealed class SqlSample
{
    public string Command { get; init; } = string.Empty;
    public double DurationMs { get; init; }
    public string Endpoint { get; init; } = string.Empty;
}

internal sealed class RequestTrace
{
    public string Endpoint { get; set; } = string.Empty;
    public long SqlTicks;
    public int SqlCount;
    public ConcurrentBag<SqlSample> Samples { get; } = new();
}

internal static class BenchmarkRequestContext
{
    public static readonly AsyncLocal<RequestTrace?> Current = new();
}
