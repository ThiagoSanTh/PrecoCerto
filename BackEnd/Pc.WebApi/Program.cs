using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Pc.Infraestrutura;
using Pc.Repositorio.Implementacoes;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes;
using Pc.Servico.Implementacoes.IA;
using Pc.Servico.Implementacoes.MotorIA;
using Pc.Servico.Implementacoes.MotorIA.Regras;
using Pc.Servico.Implementacoes.Rag;
using Pc.Servico.Interfaces;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.Rag;
using Pc.WebApi.Authorization;
using Pc.WebApi.Configuration;
using Pc.WebApi.Diagnostics;
using Pc.WebApi.Hubs;
using Pc.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 📝 Serilog: logging estruturado em console e arquivo (rotação diária).
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();

    if (context.HostingEnvironment.IsDevelopment())
    {
        configuration.WriteTo.File(
            "logs/precocerto-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7);
    }
});

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://0.0.0.0:5132");
}
else
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    if (builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection vazia. Em BackEnd/Pc.WebApi rode: " +
            "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" " +
            "\"Host=db.SEU_PROJECT.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=SUA_SENHA;SSL Mode=Require\" " +
            "e também: dotnet user-secrets set \"Jwt:Secret\" \"chave-com-pelo-menos-32-caracteres\"");
    }

    Console.Error.WriteLine("WARN ConnectionStrings__DefaultConnection ausente. Healthcheck sobe; o restante da API falha até configurar o banco.");
    connectionString = "Host=127.0.0.1;Port=5432;Database=none;Username=none;Password=none";
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Preço Certo API",
        Version = "v1",
        Description = "API do Preço Certo: catálogo de produtos, lojas, ofertas, usuários e interações (favoritos, histórico, avaliações)."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[]
    {
        "http://localhost:8081",
        "http://localhost:19006",
        "http://localhost:3000",
        "http://127.0.0.1:8081",
        "http://127.0.0.1:19006",
        "http://127.0.0.1:3000",
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppPolicy", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;

                if (builder.Environment.IsDevelopment())
                {
                    if (uri.Host is "localhost" or "127.0.0.1") return true;
                    if (uri.Host.StartsWith("192.168.") || uri.Host.StartsWith("10.")) return true;
                    if (uri.Host.StartsWith("172.")) return true;
                }

                if (uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)) return true;

                return corsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
            });
    });
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? new JwtSettings();

if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
{
    if (builder.Environment.IsDevelopment())
        jwtSettings.Secret = "DEV-ONLY-PrecoCerto-Jwt-Secret-32chars!";
    else
        jwtSettings.Secret = "RAILWAY-PLACEHOLDER-JWT-SECRET-32CHARS";
    Console.Error.WriteLine("WARN Jwt:Secret ausente ou curto. Defina Jwt__Secret no Railway.");
}

builder.Services.PostConfigure<JwtSettings>(o =>
{
    o.Secret = jwtSettings.Secret;
    o.Issuer = string.IsNullOrWhiteSpace(jwtSettings.Issuer) ? "PrecoCerto" : jwtSettings.Issuer;
    o.Audience = string.IsNullOrWhiteSpace(jwtSettings.Audience) ? "PrecoCertoApp" : jwtSettings.Audience;
    o.ExpirationHours = jwtSettings.ExpirationHours > 0 ? jwtSettings.ExpirationHours : 24;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.CapacidadeCliente, policy =>
        policy.RequireRole("Cliente", "Lojista", "Vendedor"));
    options.AddPolicy(AuthPolicies.CapacidadeClienteOuAdmin, policy =>
        policy.RequireRole("Cliente", "Lojista", "Vendedor", "Admin"));
});
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Respeita X-Forwarded-Proto/For atras de proxy (Render/Railway) para que
// a deteccao de HTTPS funcione sem causar loops de redirecionamento.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 10;
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("catalogo", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 120;
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 300;
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("ia", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 30;
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("rag", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 20;
        limiter.QueueLimit = 0;
    });
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }
        if (path.Contains("/Auth/login", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/Clientes/login", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit = 0
            });
        }

        if (path.StartsWith("/api/IA", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"ia:{ip}", _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 30,
                QueueLimit = 0
            });
        }

        if (path.StartsWith("/api/Rag", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"rag:{ip}", _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 20,
                QueueLimit = 0
            });
        }

        if (path.StartsWith("/api/Feed", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/Produtos", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/Lojas", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/Ofertas", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/Weather", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 120,
                QueueLimit = 0
            });
        }

        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 300,
            QueueLimit = 0
        });
    });
});

builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// ✉️ E-mail (boas-vindas, recuperação de senha)
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.Configure<ConsultaCnpjSettings>(builder.Configuration.GetSection(ConsultaCnpjSettings.SectionName));
builder.Services.Configure<ClimaSettings>(builder.Configuration.GetSection(ClimaSettings.SectionName));
builder.Services.Configure<IdEncodingSettings>(builder.Configuration.GetSection(IdEncodingSettings.SectionName));
builder.Services.Configure<RagSettings>(builder.Configuration.GetSection(RagSettings.SectionName));
builder.Services.PostConfigure<RagSettings>(rag =>
{
    if (string.IsNullOrWhiteSpace(rag.ApiKey))
    {
        rag.ApiKey = builder.Configuration["Rag:ApiKey"]
            ?? Environment.GetEnvironmentVariable("Rag__ApiKey")
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? string.Empty;
    }

    // Defaults por provider (sem sobrescrever valores explícitos não-padrão).
    if (rag.EhGemini)
    {
        if (string.IsNullOrWhiteSpace(rag.EmbeddingModel)
            || rag.EmbeddingModel.Contains("text-embedding-3", StringComparison.OrdinalIgnoreCase)
            || rag.EmbeddingModel.Equals("text-embedding-004", StringComparison.OrdinalIgnoreCase))
            rag.EmbeddingModel = "gemini-embedding-001";

        if (string.IsNullOrWhiteSpace(rag.ApiBaseUrl)
            || rag.ApiBaseUrl.Contains("openai.com", StringComparison.OrdinalIgnoreCase))
            rag.ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta";
    }
    else if (rag.EhOpenAi)
    {
        if (string.IsNullOrWhiteSpace(rag.EmbeddingModel)
            || rag.EmbeddingModel.Contains("gemini", StringComparison.OrdinalIgnoreCase))
            rag.EmbeddingModel = "text-embedding-3-small";

        if (string.IsNullOrWhiteSpace(rag.ApiBaseUrl)
            || rag.ApiBaseUrl.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase))
            rag.ApiBaseUrl = "https://api.openai.com/v1";
    }
});

builder.Services.AddHttpClient("OpenAIEmbeddings", (sp, client) =>
{
    var rag = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RagSettings>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(rag.ApiBaseUrl) || rag.EhGemini
        ? "https://api.openai.com/v1/"
        : rag.ApiBaseUrl.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(rag.TimeoutSegundos, 5, 120));
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

builder.Services.AddHttpClient("GeminiEmbeddings", (sp, client) =>
{
    var rag = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RagSettings>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(rag.ApiBaseUrl) || rag.EhOpenAi
        ? "https://generativelanguage.googleapis.com/v1beta/"
        : rag.ApiBaseUrl.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(rag.TimeoutSegundos, 5, 120));
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

builder.Services.AddScoped<IEmbeddingService>(sp =>
{
    var rag = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RagSettings>>().Value;
    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    if (rag.EhGemini)
    {
        return new GeminiEmbeddingService(
            httpFactory.CreateClient("GeminiEmbeddings"),
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RagSettings>>(),
            loggerFactory.CreateLogger<GeminiEmbeddingService>());
    }

    return new OpenAIEmbeddingService(
        httpFactory.CreateClient("OpenAIEmbeddings"),
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RagSettings>>(),
        loggerFactory.CreateLogger<OpenAIEmbeddingService>());
});

builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddSingleton<IIdCodificador, IdCodificadorServico>();
builder.Services.AddHttpClient<IConsultaCnpjServico, ConsultaCnpjServico>();
builder.Services.AddHttpClient<IClimaProvedor, OpenMeteoClimaProvedor>((sp, client) =>
{
    var clima = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ClimaSettings>>().Value;
    var timeout = Math.Clamp(clima.TimeoutSegundos, 3, 15);
    client.Timeout = TimeSpan.FromSeconds(timeout);
    client.DefaultRequestHeaders.TryAddWithoutValidation(
        "User-Agent",
        "PrecoCerto/1.0 (https://github.com/ThiagoSanTh/PrecoCerto)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddScoped<IClimaServico, ClimaServico>();

// Repositórios — Catálogo e Estabelecimentos
builder.Services.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();
builder.Services.AddScoped<ILojaRepositorio, LojaRepositorio>();
builder.Services.AddScoped<IOfertaRepositorio, OfertaRepositorio>();

// Repositórios — Usuários
builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IAdminRepositorio, AdminRepositorio>();

// Repositórios — Interações
builder.Services.AddScoped<IFavoritoRepositorio, FavoritoRepositorio>();
builder.Services.AddScoped<IHistoricoPesquisaRepositorio, HistoricoPesquisaRepositorio>();
builder.Services.AddScoped<IAvaliacaoRepositorio, AvaliacaoRepositorio>();
builder.Services.AddScoped<IPreferenciaClienteRepositorio, PreferenciaClienteRepositorio>();
builder.Services.AddScoped<IConversaRepositorio, ConversaRepositorio>();
builder.Services.AddScoped<IDocumentoRagRepositorio, DocumentoRagRepositorio>();

// Serviços — Catálogo e Estabelecimentos
builder.Services.AddScoped<IProdutoServico, ProdutoServico>();
builder.Services.AddScoped<IFeedServico, FeedServico>();
builder.Services.AddScoped<ILojaServico, LojaServico>();
builder.Services.AddScoped<IOfertaServico, OfertaServico>();

// Serviços — Usuários
builder.Services.AddScoped<IValidadorEmail, ValidadorEmailServico>();
builder.Services.AddScoped<IClienteServico, ClienteServico>();
builder.Services.AddScoped<IAdminServico, AdminServico>();

// Serviços — Interações
builder.Services.AddScoped<IFavoritoServico, FavoritoServico>();
builder.Services.AddScoped<IHistoricoPesquisaServico, HistoricoPesquisaServico>();
builder.Services.AddScoped<IAvaliacaoServico, AvaliacaoServico>();
builder.Services.AddScoped<IPreferenciaClienteServico, PreferenciaClienteServico>();
builder.Services.AddScoped<IConversaServico, ConversaServico>();
builder.Services.AddScoped<ChatNotificacaoHelper>();
builder.Services.AddScoped<IRespostaIAServico, RespostaDeterministicaServico>();
builder.Services.AddScoped<IIAServico, IAServico>();

// MotorIA v2
builder.Services.AddScoped<IClassificadorIntencaoIA, ClassificadorIntencaoIA>();
builder.Services.AddScoped<IExtratorEntidadesIA, ExtratorEntidadesIA>();
builder.Services.AddScoped<IInterpretadorIA, InterpretadorIA>();
builder.Services.AddScoped<IRegraIA, RegraEconomizar>();
builder.Services.AddScoped<IRegraIA, RegraUrgencia>();
builder.Services.AddScoped<IRegraIA, RegraChuva>();
builder.Services.AddScoped<IRegraIA, RegraEntrega>();
builder.Services.AddScoped<IRegraIA, RegraProximidade>();
builder.Services.AddScoped<IRegraIA, RegraPromocao>();
builder.Services.AddScoped<IRegraIA, RegraQualidade>();
builder.Services.AddScoped<IMotorRegrasIA, MotorRegrasIA>();
builder.Services.AddScoped<IMotorPontuacaoIA, MotorPontuacaoIA>();
builder.Services.AddScoped<IMotorRecomendacaoIA, MotorRecomendacaoIA>();
builder.Services.AddScoped<IGeradorRespostaIA, GeradorRespostaIA>();
builder.Services.AddScoped<IRagConhecimentoIA, RagConhecimentoIA>();
builder.Services.AddScoped<IMotorIA, MotorIA>();

// RAG
builder.Services.AddSingleton<IRagIndexFila, RagIndexFila>();
builder.Services.AddScoped<IRagDocumentBuilder, RagDocumentBuilder>();
builder.Services.AddScoped<IRagIndexadorServico, RagIndexadorServico>();
builder.Services.AddScoped<IRagCascadeIndexador, RagCascadeIndexador>();
builder.Services.AddScoped<IRagIndexDlqServico, RagIndexDlqServico>();
builder.Services.AddScoped<IRagServico, RagServico>();
builder.Services.AddHostedService<RagIndexWorker>();
builder.Services.AddHostedService<RagInitialIndexHostedService>();
builder.Services.AddHostedService<MigracaoStartupHostedService>();
builder.Services.AddHostedService<AdminSeedHostedService>();

if (BenchmarkMode.Enabled)
{
    builder.Services.AddSingleton<EfQueryInterceptor>();
    builder.Services.AddHostedService<ProcessSamplerHostedService>();
    Console.Error.WriteLine("WARN PRECOCERTO_BENCHMARK=1: métricas ativas e rate limiter desligado.");
}

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(
        connectionString,
        npgsql =>
        {
            npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            npgsql.UseVector();
        });
    if (BenchmarkMode.Enabled)
        options.AddInterceptors(sp.GetRequiredService<EfQueryInterceptor>());
});

var app = builder.Build();

app.UseForwardedHeaders();

// CORS antes de redirecionamentos — preflight OPTIONS não pode receber 301/302.
app.UseCors("AppPolicy");

app.UseResponseCompression();
if (BenchmarkMode.Enabled)
    app.UseMiddleware<BenchmarkMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!BenchmarkMode.Enabled)
    app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous()
    .DisableRateLimiting();
app.MapControllers();
app.MapHub<Pc.WebApi.Hubs.ChatHub>("/hubs/chat");

app.Run();
