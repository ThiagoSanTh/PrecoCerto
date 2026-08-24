using System.Diagnostics;

namespace Pc.WebApi.Diagnostics;

internal sealed class ProcessSamplerHostedService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var process = Process.GetCurrentProcess();
        var lastCpu = process.TotalProcessorTime;
        var lastAt = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            process.Refresh();
            var now = DateTime.UtcNow;
            var cpu = process.TotalProcessorTime;
            var elapsed = (now - lastAt).TotalMilliseconds;
            var cpuMs = (cpu - lastCpu).TotalMilliseconds;
            var cpuPct = elapsed <= 0 ? 0 : cpuMs / (elapsed * Environment.ProcessorCount) * 100.0;

            ThreadPool.GetAvailableThreads(out var workerAvail, out _);
            ThreadPool.GetMaxThreads(out var workerMax, out _);

            BenchmarkStats.RecordProcess(new ProcessSample
            {
                At = DateTimeOffset.UtcNow,
                CpuPercent = Math.Round(cpuPct, 2),
                WorkingSetBytes = process.WorkingSet64,
                GcHeapBytes = GC.GetTotalMemory(false),
                ThreadCount = process.Threads.Count,
                WorkerAvailable = workerAvail,
                WorkerMax = workerMax
            });

            lastCpu = cpu;
            lastAt = now;
        }
    }
}
