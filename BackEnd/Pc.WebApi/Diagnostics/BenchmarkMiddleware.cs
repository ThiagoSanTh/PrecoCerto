namespace Pc.WebApi.Diagnostics;

internal sealed class BenchmarkMiddleware
{
    private readonly RequestDelegate _next;

    public BenchmarkMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        if (path.StartsWith("/api/health/bench", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var trace = new RequestTrace
        {
            Endpoint = NormalizePath(path)
        };
        BenchmarkRequestContext.Current.Value = trace;
        var started = TimeProvider.System.GetTimestamp();

        var failed = false;
        try
        {
            await _next(context);
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            var durationMs = TimeProvider.System.GetElapsedTime(started).TotalMilliseconds;
            var sqlMs = TimeSpan.FromTicks(Interlocked.Read(ref trace.SqlTicks)).TotalMilliseconds;
            var status = context.Response.StatusCode;
            if (failed && status < 400)
                status = 500;
            var bytes = context.Response.ContentLength ?? 0;
            BenchmarkStats.RecordRequest(
                trace.Endpoint,
                context.Request.Method,
                status,
                durationMs,
                sqlMs,
                trace.SqlCount,
                bytes);
            BenchmarkRequestContext.Current.Value = null;
        }
    }

    private static string NormalizePath(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (Guid.TryParse(parts[i], out _))
                parts[i] = "{id}";
            else if (parts[i].All(char.IsDigit) && parts[i].Length >= 11)
                parts[i] = "{cnpj}";
        }
        return "/" + string.Join('/', parts);
    }
}
