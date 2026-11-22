using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pc.WebApi.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Entities.Rag;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Infraestrutura;
using Xunit;

namespace Pc.Servico.Tests;

/// <summary>
/// Contrato da issue #54: estado do chat por usuário, tem-novas sem desde
/// e expiração do heartbeat calculada na leitura.
/// </summary>
public class ChatEstadoUsuarioTests : IClassFixture<ChatEstadoApiFactory>
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 22, 15, 0, 0, TimeSpan.Zero);

    private readonly ChatEstadoApiFactory _factory;
    private readonly HttpClient _client;

    public ChatEstadoUsuarioTests(ChatEstadoApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.Relogio.Definir(T0);
    }

    [Fact]
    public async Task Abrir_e_fechar_sem_corpo_sao_idempotentes()
    {
        var usuario = await CriarClienteAsync();
        var token = Token(usuario);

        var abrir1 = await PostSemCorpo("/api/Conversas/chat/abrir", token);
        var abrir2 = await PostSemCorpo("/api/Conversas/chat/abrir", token);
        var aberto = await Estado(token);

        _factory.Relogio.Definir(T0.AddMinutes(3));
        var fechar1 = await PostSemCorpo("/api/Conversas/chat/fechar", token);
        var fechar2 = await PostSemCorpo("/api/Conversas/chat/fechar", token);
        var fechado = await Estado(token);

        Assert.Equal(HttpStatusCode.NoContent, abrir1.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, abrir2.StatusCode);
        Assert.Equal(string.Empty, await abrir1.Content.ReadAsStringAsync());
        Assert.True(aberto.Aberto);
        Assert.Null(aberto.FechadoEm);

        Assert.Equal(HttpStatusCode.NoContent, fechar1.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, fechar2.StatusCode);
        Assert.False(fechado.Aberto);
        Assert.Equal(T0.AddMinutes(3), fechado.FechadoEm);
        Assert.EndsWith("Z", fechado.FechadoEmTexto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Abrir_nao_altera_o_chat_de_outro_usuario()
    {
        var ana = await CriarClienteAsync();
        var bia = await CriarClienteAsync();

        var resposta = await PostSemCorpo("/api/Conversas/chat/abrir", Token(ana));
        var estadoBia = await Estado(Token(bia));

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.False(estadoBia.Aberto);
        Assert.Null(estadoBia.FechadoEm);
    }

    [Fact]
    public async Task Tem_novas_sem_desde_ignora_mensagem_anterior_ao_fechamento()
    {
        var usuario = await CriarClienteAsync();
        await PostSemCorpo("/api/Conversas/chat/fechar", Token(usuario));
        await CriarMensagemAsync(usuario, T0.AddHours(-1).UtcDateTime);

        Assert.False(await TemNovas(Token(usuario)));
    }

    [Fact]
    public async Task Tem_novas_sem_desde_acusa_mensagem_posterior_ao_fechamento()
    {
        var usuario = await CriarClienteAsync();
        await PostSemCorpo("/api/Conversas/chat/fechar", Token(usuario));
        await CriarMensagemAsync(usuario, T0.AddMinutes(1).UtcDateTime);

        Assert.True(await TemNovas(Token(usuario)));
    }

    [Fact]
    public async Task Tem_novas_sem_desde_com_fechamento_nulo_mantem_o_comportamento_atual()
    {
        var usuario = await CriarClienteAsync();
        await CriarMensagemAsync(usuario, T0.AddDays(-2).UtcDateTime);

        var estado = await Estado(Token(usuario));

        Assert.False(estado.Aberto);
        Assert.Null(estado.FechadoEm);
        Assert.True(await TemNovas(Token(usuario)));
    }

    [Fact]
    public async Task Tem_novas_com_desde_ignora_o_fechamento_gravado()
    {
        var usuario = await CriarClienteAsync();
        await PostSemCorpo("/api/Conversas/chat/fechar", Token(usuario));
        await CriarMensagemAsync(usuario, T0.AddMinutes(1).UtcDateTime);

        var desdeDepois = T0.AddMinutes(2).UtcDateTime.ToString("o", CultureInfo.InvariantCulture);

        Assert.False(await TemNovas(Token(usuario), desdeDepois));
        Assert.True(await TemNovas(Token(usuario)));
    }

    [Fact]
    public async Task Chat_aberto_ha_mais_de_cinco_minutos_conta_como_fechado()
    {
        var usuario = await CriarClienteAsync();
        var token = Token(usuario);

        _factory.Relogio.Definir(T0.AddHours(-1));
        Assert.Equal(HttpStatusCode.NoContent, (await PostSemCorpo("/api/Conversas/chat/fechar", token)).StatusCode);

        _factory.Relogio.Definir(T0);
        Assert.Equal(HttpStatusCode.NoContent, (await PostSemCorpo("/api/Conversas/chat/abrir", token)).StatusCode);
        await CriarMensagemAsync(usuario, T0.AddMinutes(-30).UtcDateTime);

        _factory.Relogio.Avancar(TimeSpan.FromMinutes(6));

        var estado = await Estado(token);
        Assert.False(estado.Aberto);
        Assert.Equal(T0, estado.FechadoEm);
        Assert.False(await TemNovas(token));

        await CriarMensagemAsync(usuario, T0.AddMinutes(1).UtcDateTime);
        Assert.True(await TemNovas(token));
    }

    [Fact]
    public async Task Segundo_abrir_renova_o_heartbeat()
    {
        var usuario = await CriarClienteAsync();
        var token = Token(usuario);

        Assert.Equal(HttpStatusCode.NoContent, (await PostSemCorpo("/api/Conversas/chat/abrir", token)).StatusCode);
        _factory.Relogio.Avancar(TimeSpan.FromMinutes(4));
        Assert.Equal(HttpStatusCode.NoContent, (await PostSemCorpo("/api/Conversas/chat/abrir", token)).StatusCode);
        _factory.Relogio.Avancar(TimeSpan.FromMinutes(4));

        var aindaAberto = await Estado(token);
        Assert.True(aindaAberto.Aberto);

        _factory.Relogio.Avancar(TimeSpan.FromMinutes(2));
        var expirado = await Estado(token);
        Assert.False(expirado.Aberto);
        Assert.Equal(T0.AddMinutes(4), expirado.FechadoEm);
    }

    [Theory]
    [InlineData("POST", "/api/Conversas/chat/abrir")]
    [InlineData("POST", "/api/Conversas/chat/fechar")]
    [InlineData("GET", "/api/Conversas/chat/estado")]
    [InlineData("GET", "/api/Conversas/tem-novas")]
    public async Task Anonimo_recebe_401(string metodo, string rota)
    {
        var pedido = new HttpRequestMessage(new HttpMethod(metodo), rota);
        var resposta = await _client.SendAsync(pedido);
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    private async Task<HttpResponseMessage> PostSemCorpo(string rota, string token)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Post, rota);
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Null(pedido.Content);
        return await _client.SendAsync(pedido);
    }

    private async Task<bool> TemNovas(string token, string? desde = null)
    {
        var rota = desde is null
            ? "/api/Conversas/tem-novas"
            : "/api/Conversas/tem-novas?desde=" + Uri.EscapeDataString(desde);
        var pedido = new HttpRequestMessage(HttpMethod.Get, rota);
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resposta = await _client.SendAsync(pedido);
        var texto = await resposta.Content.ReadAsStringAsync();
        Assert.True(resposta.StatusCode == HttpStatusCode.OK, texto);
        using var doc = JsonDocument.Parse(texto);
        return doc.RootElement.GetProperty("temNovas").GetBoolean();
    }

    private async Task<EstadoChatDto> Estado(string token)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Get, "/api/Conversas/chat/estado");
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resposta = await _client.SendAsync(pedido);
        var texto = await resposta.Content.ReadAsStringAsync();
        Assert.True(resposta.StatusCode == HttpStatusCode.OK, texto);
        using var doc = JsonDocument.Parse(texto);
        var fechado = doc.RootElement.GetProperty("chatFechadoEm");
        string? cru = null;
        DateTimeOffset? instante = null;
        if (fechado.ValueKind != JsonValueKind.Null)
        {
            cru = fechado.GetString();
            instante = DateTimeOffset.Parse(cru!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        return new EstadoChatDto(doc.RootElement.GetProperty("chatAberto").GetBoolean(), instante, cru);
    }

    private async Task<Guid> CriarClienteAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var usuario = new Usuario
        {
            NomeUsuario = "Ana",
            Email = $"ana-{Guid.NewGuid():N}@test",
            SenhaHash = "x",
            Papel = PapelUsuario.Cliente,
            Tipo = TipoUsuario.Cliente
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario.Id;
    }

    private async Task CriarMensagemAsync(Guid clienteId, DateTime enviadaEm)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var remetenteId = await RemetenteDaConversaAsync(db, clienteId);
        db.Mensagens.Add(new Mensagem
        {
            ConversaId = _factory.Conversas[clienteId],
            RemetenteId = remetenteId,
            RemetentePapel = PapelUsuario.Lojista,
            Texto = "oi",
            EnviadaEm = DateTime.SpecifyKind(enviadaEm, DateTimeKind.Utc),
            Lida = false
        });
        await db.SaveChangesAsync();
    }

    private async Task<Guid> RemetenteDaConversaAsync(AppDbContext db, Guid clienteId)
    {
        if (_factory.Remetentes.TryGetValue(clienteId, out var remetente))
            return remetente;

        var lojista = new Usuario
        {
            NomeUsuario = "Loja",
            Email = $"loja-{Guid.NewGuid():N}@test",
            SenhaHash = "x",
            Papel = PapelUsuario.Lojista,
            Tipo = TipoUsuario.Lojista
        };
        var loja = new Loja
        {
            NomeFantasia = "Loja",
            Usuario = lojista,
            Endereco = new Endereco { Cidade = "Rio" }
        };
        var conversa = new Conversa { ClienteId = clienteId, Loja = loja };
        db.Conversas.Add(conversa);
        await db.SaveChangesAsync();
        _factory.Conversas[clienteId] = conversa.Id;
        _factory.Remetentes[clienteId] = lojista.Id;
        return lojista.Id;
    }

    private string Token(Guid usuarioId)
    {
        var jwt = _factory.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));
        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(jwt.Issuer) ? "PrecoCerto" : jwt.Issuer,
            audience: string.IsNullOrWhiteSpace(jwt.Audience) ? "PrecoCertoApp" : jwt.Audience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim(ClaimTypes.Role, "Cliente")
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private readonly record struct EstadoChatDto(bool Aberto, DateTimeOffset? FechadoEm, string? FechadoEmTexto);
}

public sealed class RelogioMutavel : TimeProvider
{
    private DateTimeOffset _utc;

    public RelogioMutavel(DateTimeOffset utc) => _utc = utc;

    public void Definir(DateTimeOffset utc) => _utc = utc;

    public void Avancar(TimeSpan tempo) => _utc = _utc.Add(tempo);

    public override DateTimeOffset GetUtcNow() => _utc;
}

public sealed class ChatEstadoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSecret = "TEST-ONLY-PrecoCerto-Jwt-Secret-32chars!";

    private readonly SqliteConnection _conexao = new("Data Source=:memory:");

    public RelogioMutavel Relogio { get; } = new(new DateTimeOffset(2026, 11, 22, 15, 0, 0, TimeSpan.Zero));

    public Dictionary<Guid, Guid> Conversas { get; } = new();

    public Dictionary<Guid, Guid> Remetentes { get; } = new();

    public ChatEstadoApiFactory() => _conexao.Open();

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        _conexao.Dispose();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Port=5432;Database=none;Username=none;Password=none",
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:Issuer"] = "PrecoCerto",
                ["Jwt:Audience"] = "PrecoCertoApp",
                ["Jwt:ExpirationHours"] = "24"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            TrocarBanco(services, _conexao);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Relogio);
        });
    }

    private static void TrocarBanco(IServiceCollection services, SqliteConnection conexao)
    {
        var remover = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                || d.ServiceType == typeof(DbContextOptions)
                || d.ServiceType == typeof(AppDbContext)
                || (d.ServiceType.IsGenericType
                    && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)
                    && d.ServiceType.GenericTypeArguments.Length == 1
                    && d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext)))
            .ToList();

        foreach (var descriptor in remover)
            services.Remove(descriptor);

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conexao).Options;
        services.AddScoped<AppDbContext>(_ => new ContextoChatSqlite(options));
    }

    private sealed class ContextoChatSqlite(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<DocumentoRag>();
        }
    }
}
