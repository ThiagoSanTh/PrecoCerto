using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pc.Infraestrutura;
using Pc.Repositorio.Implementacoes;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes.Rag;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.RagReindexCli;

/// <summary>
/// Loop autônomo: reindexa só pendentes até erros=0, com backoff em rate-limit.
/// Uso: dotnet run --project BackEnd/Pc.RagReindexCli
/// </summary>
public static class Program
{
    private static string LogFile =>
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "rag-reindex-loop.log"));

    public static async Task<int> Main(string[] args)
    {
        var maxPasses = ArgInt(args, "--max-passes", 80);
        var baseDelayMs = ArgInt(args, "--delay-ms", 1500);
        var pauseBetweenPassesSec = ArgInt(args, "--pause-sec", 90);

        Log($"=== RAG reindex loop start maxPasses={maxPasses} delayMs={baseDelayMs} ===");
        Log($"Log file: {LogFile}");

        var config = BuildConfig();
        var services = new ServiceCollection();
        ConfigureServices(services, config, baseDelayMs);
        await using var sp = services.BuildServiceProvider();

        var ragOpts = sp.GetRequiredService<IOptions<RagSettings>>().Value;
        if (!ragOpts.EstaConfigurado)
        {
            Log("ABORT: Rag não configurado (ApiKey/Provider). Defina user-secrets Rag:ApiKey.");
            return 2;
        }

        Log($"Provider={ragOpts.Provider} Model={ragOpts.EmbeddingModel} Delay={ragOpts.DelayEntreEmbeddingsMs}ms");

        var prevErros = -1;
        var stagnant = 0;

        for (var pass = 1; pass <= maxPasses; pass++)
        {
            // Ajusta delay em runtime a cada pass estagnado
            ragOpts.DelayEntreEmbeddingsMs = Math.Min(5000, baseDelayMs + (stagnant * 500));

            Log($"--- Pass {pass}/{maxPasses} delay={ragOpts.DelayEntreEmbeddingsMs}ms ---");

            using var scope = sp.CreateScope();
            var indexador = scope.ServiceProvider.GetRequiredService<IRagIndexadorServico>();

            RagReindexResultado resultado;
            try
            {
                resultado = await indexador.ReindexarAsync(
                    apenasTipo: null,
                    somentePendentes: true,
                    cancellationToken: CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log($"PASS {pass} EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                stagnant++;
                var wait = TimeSpan.FromMinutes(Math.Min(15, 2 + stagnant * 2));
                Log($"Aguardando {wait.TotalMinutes:0.#} min após falha dura...");
                await Task.Delay(wait);
                continue;
            }

            Log(
                $"PASS {pass} docs={resultado.TotalDocumentos} emb={resultado.TotalEmbeddingsGerados} " +
                $"prod={resultado.ProdutosIndexados} loja={resultado.LojasIndexadas} " +
                $"oferta={resultado.OfertasIndexadas} av={resultado.AvaliacoesIndexadas} " +
                $"erros={resultado.Erros} tempoMs={resultado.TempoMs}");

            if (resultado.Erros == 0)
            {
                Log("SUCCESS: 0 erros. Índice pendente concluído.");
                return 0;
            }

            // Progresso = embeddings gerados nesta passada
            if (resultado.TotalEmbeddingsGerados == 0 && resultado.Erros >= prevErros && prevErros >= 0)
            {
                stagnant++;
                Log($"Sem progresso (stagnant={stagnant}). Provável quota Gemini — backoff longo.");
            }
            else if (resultado.Erros < prevErros || resultado.TotalEmbeddingsGerados > 0)
            {
                stagnant = Math.Max(0, stagnant - 1);
            }

            prevErros = resultado.Erros;

            // Pausa entre passes: cresce se estiver estagnado (até 20 min)
            var pauseSec = stagnant >= 2
                ? Math.Min(1200, pauseBetweenPassesSec * (1 + stagnant))
                : pauseBetweenPassesSec;
            Log($"Pausa {pauseSec}s antes da próxima passada ({resultado.Erros} erros restantes)...");
            await Task.Delay(TimeSpan.FromSeconds(pauseSec));
        }

        Log($"STOP: atingiu maxPasses={maxPasses} com erros restantes={prevErros}.");
        return 1;
    }

    private static IConfiguration BuildConfig()
    {
        var webApiDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Pc.WebApi"));
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(Path.Combine(webApiDir, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(webApiDir, "appsettings.Development.json"), optional: true)
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables();
        return builder.Build();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration config, int delayMs)
    {
        services.AddLogging(b =>
        {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Information);
            b.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            b.AddFilter("System.Net.Http", LogLevel.Warning);
        });

        services.Configure<RagSettings>(config.GetSection(RagSettings.SectionName));
        services.PostConfigure<RagSettings>(rag =>
        {
            if (string.IsNullOrWhiteSpace(rag.ApiKey))
            {
                rag.ApiKey = config["Rag:ApiKey"]
                    ?? Environment.GetEnvironmentVariable("Rag__ApiKey")
                    ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                    ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
                    ?? string.Empty;
            }

            if (rag.EhGemini)
            {
                if (string.IsNullOrWhiteSpace(rag.EmbeddingModel)
                    || rag.EmbeddingModel.Contains("text-embedding", StringComparison.OrdinalIgnoreCase))
                    rag.EmbeddingModel = "gemini-embedding-001";
                if (string.IsNullOrWhiteSpace(rag.ApiBaseUrl)
                    || rag.ApiBaseUrl.Contains("openai.com", StringComparison.OrdinalIgnoreCase))
                    rag.ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta";
            }

            rag.DelayEntreEmbeddingsMs = delayMs;
            rag.MaxRetries = Math.Max(rag.MaxRetries, 6);
            rag.RunInitialIndexOnStartup = false;
        });

        var cs = config.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection ausente (user-secrets).");

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(cs, o => o.UseVector()));

        services.AddHttpClient("GeminiEmbeddings", (sp, client) =>
        {
            var rag = sp.GetRequiredService<IOptions<RagSettings>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(rag.ApiBaseUrl)
                ? "https://generativelanguage.googleapis.com/v1beta/"
                : rag.ApiBaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(rag.TimeoutSegundos, 5, 180));
        });

        services.AddHttpClient("OpenAIEmbeddings", (sp, client) =>
        {
            var rag = sp.GetRequiredService<IOptions<RagSettings>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(rag.ApiBaseUrl) || rag.EhGemini
                ? "https://api.openai.com/v1/"
                : rag.ApiBaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(rag.TimeoutSegundos, 5, 180));
        });

        services.AddScoped<IEmbeddingService>(sp =>
        {
            var rag = sp.GetRequiredService<IOptions<RagSettings>>().Value;
            var http = sp.GetRequiredService<IHttpClientFactory>();
            var log = sp.GetRequiredService<ILoggerFactory>();
            if (rag.EhGemini)
                return new GeminiEmbeddingService(
                    http.CreateClient("GeminiEmbeddings"),
                    sp.GetRequiredService<IOptions<RagSettings>>(),
                    log.CreateLogger<GeminiEmbeddingService>());
            return new OpenAIEmbeddingService(
                http.CreateClient("OpenAIEmbeddings"),
                sp.GetRequiredService<IOptions<RagSettings>>(),
                log.CreateLogger<OpenAIEmbeddingService>());
        });

        services.AddSingleton<IRagIndexFila, RagIndexFila>();
        services.AddScoped<IDocumentoRagRepositorio, DocumentoRagRepositorio>();
        services.AddScoped<IRagDocumentBuilder, RagDocumentBuilder>();
        services.AddScoped<IRagIndexDlqServico, RagIndexDlqServico>();
        services.AddScoped<IRagIndexadorServico, RagIndexadorServico>();
    }

    private static int ArgInt(string[] args, string name, int fallback)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(args[i + 1], out var v))
                return v;
        }
        return fallback;
    }

    private static void Log(string msg)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}";
        Console.WriteLine(line);
        try { File.AppendAllText(LogFile, line + Environment.NewLine); } catch { /* ignore */ }
    }
}
