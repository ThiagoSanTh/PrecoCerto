using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace Pc.WebApi.Diagnostics;

internal sealed class EndpointAccumulator
{
    public long Count;
    public long BytesOut;
    public long Errors4xx;
    public long Errors5xx;
    public long Status429;
    public long Status500;
    public long Status502;
    public long Status503;
    public long SqlQueries;
    public readonly ConcurrentQueue<double> DurationsMs = new();
    public readonly ConcurrentQueue<double> SqlMs = new();
    public readonly ConcurrentQueue<double> AppMs = new();
}

internal sealed class ProcessSample
{
    public DateTimeOffset At { get; init; }
    public double CpuPercent { get; init; }
    public long WorkingSetBytes { get; init; }
    public long GcHeapBytes { get; init; }
    public int ThreadCount { get; init; }
    public int WorkerAvailable { get; init; }
    public int WorkerMax { get; init; }
}

internal static class BenchmarkStats
{
    private const int MaxSamplesPerEndpoint = 40_000;
    private const int MaxSqlSamples = 250;

    private static readonly ConcurrentDictionary<string, EndpointAccumulator> Endpoints = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentQueue<SqlSample> RecentSql = new();
    private static readonly ConcurrentQueue<ProcessSample> ProcessSamples = new();
    private static readonly Process Process = Process.GetCurrentProcess();

    public static void RecordRequest(
        string endpoint,
        string method,
        int status,
        double durationMs,
        double sqlMs,
        int sqlCount,
        long bytesOut)
    {
        var key = $"{method} {endpoint}";
        var acc = Endpoints.GetOrAdd(key, _ => new EndpointAccumulator());
        Interlocked.Increment(ref acc.Count);
        Interlocked.Add(ref acc.BytesOut, bytesOut);
        Interlocked.Add(ref acc.SqlQueries, sqlCount);

        if (status is >= 400 and < 500) Interlocked.Increment(ref acc.Errors4xx);
        if (status >= 500) Interlocked.Increment(ref acc.Errors5xx);
        if (status == 429) Interlocked.Increment(ref acc.Status429);
        if (status == 500) Interlocked.Increment(ref acc.Status500);
        if (status == 502) Interlocked.Increment(ref acc.Status502);
        if (status == 503) Interlocked.Increment(ref acc.Status503);

        EnqueueCapped(acc.DurationsMs, durationMs);
        EnqueueCapped(acc.SqlMs, sqlMs);
        EnqueueCapped(acc.AppMs, Math.Max(0, durationMs - sqlMs));
    }

    public static void RecordSql(SqlSample sample)
    {
        RecentSql.Enqueue(sample);
        while (RecentSql.Count > MaxSqlSamples && RecentSql.TryDequeue(out _)) { }
    }

    public static void RecordProcess(ProcessSample sample)
    {
        ProcessSamples.Enqueue(sample);
        while (ProcessSamples.Count > 600 && ProcessSamples.TryDequeue(out _)) { }
    }

    public static void Reset()
    {
        Endpoints.Clear();
        while (RecentSql.TryDequeue(out _)) { }
        while (ProcessSamples.TryDequeue(out _)) { }
        GC.Collect(2, GCCollectionMode.Optimized, blocking: false);
    }

    public static object Snapshot()
    {
        Process.Refresh();
        ThreadPool.GetAvailableThreads(out var workerAvail, out var ioAvail);
        ThreadPool.GetMaxThreads(out var workerMax, out var ioMax);

        var endpoints = Endpoints
            .Select(kv =>
            {
                var a = kv.Value;
                var durations = a.DurationsMs.ToArray();
                var sql = a.SqlMs.ToArray();
                var app = a.AppMs.ToArray();
                var count = Interlocked.Read(ref a.Count);
                var err4 = Interlocked.Read(ref a.Errors4xx);
                var err5 = Interlocked.Read(ref a.Errors5xx);
                return new
                {
                    endpoint = kv.Key,
                    count,
                    bytesOut = Interlocked.Read(ref a.BytesOut),
                    avgBytes = count == 0 ? 0 : Interlocked.Read(ref a.BytesOut) / count,
                    sqlQueries = Interlocked.Read(ref a.SqlQueries),
                    avgSqlQueries = count == 0 ? 0 : (double)Interlocked.Read(ref a.SqlQueries) / count,
                    meanMs = Mean(durations),
                    p50Ms = Percentile(durations, 50),
                    p95Ms = Percentile(durations, 95),
                    p99Ms = Percentile(durations, 99),
                    minMs = durations.Length == 0 ? 0 : durations.Min(),
                    maxMs = durations.Length == 0 ? 0 : durations.Max(),
                    sqlMeanMs = Mean(sql),
                    sqlP95Ms = Percentile(sql, 95),
                    appMeanMs = Mean(app),
                    appP95Ms = Percentile(app, 95),
                    errors4xx = err4,
                    errors5xx = err5,
                    status429 = Interlocked.Read(ref a.Status429),
                    status500 = Interlocked.Read(ref a.Status500),
                    status502 = Interlocked.Read(ref a.Status502),
                    status503 = Interlocked.Read(ref a.Status503),
                    errorRate = count == 0 ? 0 : (double)(err4 + err5) / count
                };
            })
            .OrderByDescending(x => x.p95Ms)
            .ToList();

        var processList = ProcessSamples.ToArray();
        var sqlList = RecentSql
            .OrderByDescending(s => s.DurationMs)
            .Take(40)
            .Select(s => new { s.Endpoint, s.DurationMs, command = Truncate(s.Command, 600) })
            .ToList();

        return new
        {
            capturedAt = DateTimeOffset.UtcNow,
            machine = new
            {
                processorCount = Environment.ProcessorCount,
                workingSetBytes = Process.WorkingSet64,
                gcHeapBytes = GC.GetTotalMemory(false),
                threadCount = Process.Threads.Count,
                workerAvail,
                workerMax,
                ioAvail,
                ioMax
            },
            processSamples = processList.Select(s => new
            {
                at = s.At,
                s.CpuPercent,
                s.WorkingSetBytes,
                s.GcHeapBytes,
                s.ThreadCount,
                s.WorkerAvailable,
                s.WorkerMax
            }),
            processSummary = processList.Length == 0 ? null : new
            {
                cpuMean = processList.Average(s => s.CpuPercent),
                cpuMax = processList.Max(s => s.CpuPercent),
                ramMeanMb = processList.Average(s => s.WorkingSetBytes / 1024.0 / 1024.0),
                ramMaxMb = processList.Max(s => s.WorkingSetBytes / 1024.0 / 1024.0)
            },
            endpoints,
            slowestSql = sqlList
        };
    }

    private static void EnqueueCapped(ConcurrentQueue<double> queue, double value)
    {
        queue.Enqueue(value);
        while (queue.Count > MaxSamplesPerEndpoint && queue.TryDequeue(out _)) { }
    }

    private static double Mean(double[] values) =>
        values.Length == 0 ? 0 : values.Average();

    private static double Percentile(double[] values, int p)
    {
        if (values.Length == 0) return 0;
        var sorted = values.OrderBy(v => v).ToArray();
        var idx = (int)Math.Ceiling(p / 100.0 * sorted.Length) - 1;
        idx = Math.Clamp(idx, 0, sorted.Length - 1);
        return sorted[idx];
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
