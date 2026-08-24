namespace Pc.WebApi.Diagnostics;

internal static class BenchmarkMode
{
    public static bool Enabled { get; } =
        string.Equals(Environment.GetEnvironmentVariable("PRECOCERTO_BENCHMARK"), "1", StringComparison.OrdinalIgnoreCase);
}
