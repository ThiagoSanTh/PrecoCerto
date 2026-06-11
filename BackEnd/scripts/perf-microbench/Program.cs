using System.Diagnostics;
using System.Text.Json;
using Pc.Dominio.Validacoes;
using Pc.Servico.Implementacoes;

var logPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "debug-1a1352.log"));
var sessionId = "1a1352";

void Log(string message, object data, string hypothesisId = "MICRO")
{
    var line = JsonSerializer.Serialize(new
    {
        sessionId,
        runId = "perf-micro-v1",
        hypothesisId,
        location = "perf-microbench/Program.cs",
        message,
        data,
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    });
    File.AppendAllText(logPath, line + Environment.NewLine);
}

static (double avgUs, long totalMs, int iterations) Bench(string name, int iterations, Action action)
{
    // warmup
    for (var i = 0; i < 50; i++) action();

    var sw = Stopwatch.StartNew();
    for (var i = 0; i < iterations; i++) action();
    sw.Stop();
    var avgUs = sw.Elapsed.TotalMicroseconds / iterations;
    return (avgUs, sw.ElapsedMilliseconds, iterations);
}

var cpfBench = Bench("CpfValidator", 50_000, () => CpfValidator.IsValido("529.982.247-25"));
Log("microbench", new { name = "CpfValidator.IsValido", iterations = cpfBench.iterations, avgUs = Math.Round(cpfBench.avgUs, 2), totalMs = cpfBench.totalMs });

var emailBench = Bench("EmailValidator", 50_000, () => EmailValidator.IsValido("teste@exemplo.com"));
Log("microbench", new { name = "EmailValidator.IsValido", iterations = emailBench.iterations, avgUs = Math.Round(emailBench.avgUs, 2), totalMs = emailBench.totalMs });

var telBench = Bench("TelefoneValidator", 50_000, () => TelefoneValidator.IsValido("(11) 98765-4321"));
Log("microbench", new { name = "TelefoneValidator.IsValido", iterations = telBench.iterations, avgUs = Math.Round(telBench.avgUs, 2), totalMs = telBench.totalMs });

var hasher = new BcryptPasswordHasher();
var hashBench = Bench("BcryptHash", 5, () => hasher.Hash("senha123"));
Log("microbench", new { name = "BcryptPasswordHasher.Hash (workFactor 12)", iterations = hashBench.iterations, avgMs = Math.Round(hashBench.avgUs / 1000.0, 2), totalMs = hashBench.totalMs }, "BCRYPT");

var stored = hasher.Hash("senha123");
var verifyBench = Bench("BcryptVerify", 20, () => hasher.Verify("senha123", stored));
Log("microbench", new { name = "BcryptPasswordHasher.Verify", iterations = verifyBench.iterations, avgMs = Math.Round(verifyBench.avgUs / 1000.0, 2), totalMs = verifyBench.totalMs }, "BCRYPT");

Console.WriteLine(JsonSerializer.Serialize(new
{
    cpfAvgUs = Math.Round(cpfBench.avgUs, 2),
    emailAvgUs = Math.Round(emailBench.avgUs, 2),
    telAvgUs = Math.Round(telBench.avgUs, 2),
    bcryptHashAvgMs = Math.Round(hashBench.avgUs / 1000.0, 2),
    bcryptVerifyAvgMs = Math.Round(verifyBench.avgUs / 1000.0, 2)
}));
