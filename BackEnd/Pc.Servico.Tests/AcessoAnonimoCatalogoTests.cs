using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pc.WebApi.Configuration;
using Moq;
using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos;
using Xunit;

namespace Pc.Servico.Tests;

/// <summary>
/// Contrato da issue #67: mapa e lista respondem sem token; compra, IA, chat,
/// favoritos, histórico, perfil, escrita do lojista e BI continuam 401.
/// </summary>
public class AcessoAnonimoCatalogoTests : IClassFixture<CatalogoApiFactory>
{
    private const string EmailDono = "dono.secreto@loja.example";
    private const string TelefoneDono = "21988887777";

    private static readonly Guid LojaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProdutoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OfertaId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid AvaliacaoId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid ClienteId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private readonly HttpClient _client;
    private readonly string _tokenCliente;

    public AcessoAnonimoCatalogoTests(CatalogoApiFactory factory)
    {
        _client = factory.CreateClient();
        var jwt = factory.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
        _tokenCliente = EmitirToken(jwt.Secret, jwt.Issuer, jwt.Audience);
    }

    public static IEnumerable<object[]> RotasPublicas()
    {
        var loja = LojaId.ToString();
        var produto = ProdutoId.ToString();
        var oferta = OfertaId.ToString();
        yield return new object[] { "GET", "/api/Feed?page=1&pageSize=20", null!, null! };
        yield return new object[] { "GET", "/api/Lojas?page=1&pageSize=20", null!, EmailDono };
        yield return new object[] { "GET", $"/api/Lojas/mapa?latitude=-22.9&longitude=-43.2&raioKm=15", null!, EmailDono };
        yield return new object[] { "GET", $"/api/Lojas/{loja}", null!, TelefoneDono };
        yield return new object[] { "POST", "/api/Lojas/buscar?page=1&pageSize=20", "\"pao\"", EmailDono };
        yield return new object[] { "GET", $"/api/Produtos?page=1&pageSize=20&lojaId={loja}", null!, null! };
        yield return new object[] { "GET", $"/api/Produtos/{produto}", null!, null! };
        yield return new object[] { "POST", "/api/Produtos/Buscar", "{\"nome\":\"pao\",\"page\":1,\"pageSize\":20}", null! };
        yield return new object[] { "GET", "/api/Ofertas?page=1&pageSize=20", null!, null! };
        yield return new object[] { "GET", $"/api/Ofertas/{oferta}", null!, null! };
        yield return new object[] { "GET", $"/api/Ofertas/produto/{produto}", null!, null! };
        yield return new object[] { "GET", $"/api/Avaliacoes/loja/{loja}", null!, ClienteId.ToString() };
        yield return new object[] { "GET", $"/api/Avaliacoes/loja/{loja}/media", null!, null! };
        yield return new object[] { "GET", "/api/Weather?latitude=-22.9&longitude=-43.2", null!, null! };
        yield return new object[] { "GET", "/api/HistoricoPesquisa/sugestoes?termo=pao", null!, ClienteId.ToString() };
    }

    public static IEnumerable<object[]> RotasProtegidas()
    {
        var id = LojaId.ToString();
        var cliente = ClienteId.ToString();
        var produto = ProdutoId.ToString();
        yield return new object[] { "POST", "/api/IA/analisar", "{\"mensagem\":\"leite\"}" };
        yield return new object[] { "POST", "/api/IA/chat", "{\"mensagem\":\"leite\"}" };
        yield return new object[] { "POST", "/api/Lojas", "{}" };
        yield return new object[] { "PUT", $"/api/Lojas/{id}", "{}" };
        yield return new object[] { "DELETE", $"/api/Lojas/{id}", null! };
        yield return new object[] { "POST", "/api/Produtos", "{}" };
        yield return new object[] { "PUT", $"/api/Produtos/{produto}", "{}" };
        yield return new object[] { "DELETE", $"/api/Produtos/{produto}?lojaId={id}", null! };
        yield return new object[] { "POST", "/api/Ofertas", "{}" };
        yield return new object[] { "PUT", $"/api/Ofertas/{id}", "{}" };
        yield return new object[] { "DELETE", $"/api/Ofertas/{id}", null! };
        yield return new object[] { "GET", $"/api/Favoritos/cliente/{cliente}", null! };
        yield return new object[] { "GET", $"/api/Favoritos/verificar/{cliente}?produtoId={produto}", null! };
        yield return new object[] { "POST", "/api/Favoritos", "{}" };
        yield return new object[] { "DELETE", $"/api/Favoritos/cliente/{cliente}/produto/{produto}", null! };
        yield return new object[] { "GET", "/api/Conversas", null! };
        yield return new object[] { "GET", "/api/Conversas/nao-lidas", null! };
        yield return new object[] { "POST", "/api/Conversas/abrir?lojaId=" + id, null! };
        yield return new object[] { "GET", "/api/Conversas/abc/mensagens", null! };
        yield return new object[] { "POST", "/api/Conversas/abc/mensagens", "{\"texto\":\"oi\"}" };
        yield return new object[] { "POST", "/api/HistoricoPesquisa", "{}" };
        yield return new object[] { "GET", $"/api/HistoricoPesquisa/cliente/{cliente}", null! };
        yield return new object[] { "GET", $"/api/HistoricoPesquisa/cliente/{cliente}/ultimos", null! };
        yield return new object[] { "GET", $"/api/HistoricoPesquisa/loja/{id}/relatorio", null! };
        yield return new object[] { "GET", $"/api/Clientes/{cliente}", null! };
        yield return new object[] { "PUT", $"/api/Clientes/{cliente}", "{}" };
        yield return new object[] { "GET", $"/api/Lojistas/{cliente}", null! };
        yield return new object[] { "GET", $"/api/Lojistas/loja/{id}/vendedores", null! };
        yield return new object[] { "POST", $"/api/Lojistas/loja/{id}/vendedores", "{}" };
        yield return new object[] { "GET", $"/api/PreferenciasCliente/cliente/{cliente}", null! };
        yield return new object[] { "POST", "/api/Avaliacoes", "{}" };
        yield return new object[] { "GET", $"/api/Avaliacoes/{AvaliacaoId}", null! };
        yield return new object[] { "GET", $"/api/Avaliacoes/cliente/{cliente}", null! };
        yield return new object[] { "POST", "/api/Rag/search", "{}" };
        yield return new object[] { "GET", "/api/Rag/dlq", null! };
    }

    [Theory]
    [MemberData(nameof(RotasPublicas))]
    public async Task Rota_publica_responde_200_sem_token(string metodo, string rota, string? corpo, string? dadoProibido)
    {
        var resposta = await Enviar(metodo, rota, corpo);
        var texto = await resposta.Content.ReadAsStringAsync();
        Assert.True(
            resposta.StatusCode == HttpStatusCode.OK,
            $"{metodo} {rota} devolveu {(int)resposta.StatusCode}: {texto}");
        if (dadoProibido is not null)
            Assert.DoesNotContain(dadoProibido, texto, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(RotasProtegidas))]
    public async Task Rota_protegida_responde_401_sem_token(string metodo, string rota, string? corpo)
    {
        var resposta = await Enviar(metodo, rota, corpo);
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Loja_com_token_continua_devolvendo_email_e_telefone()
    {
        var resposta = await Enviar("GET", $"/api/Lojas/{LojaId}", token: _tokenCliente);
        var texto = await resposta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains(EmailDono, texto, StringComparison.Ordinal);
        Assert.Contains(TelefoneDono, texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Avaliacao_com_token_continua_devolvendo_clienteId()
    {
        var resposta = await Enviar("GET", $"/api/Avaliacoes/loja/{LojaId}", token: _tokenCliente);
        var texto = await resposta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains(ClienteId.ToString(), texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preflight_cors_do_app_local_continua_liberado()
    {
        var pedido = new HttpRequestMessage(HttpMethod.Options, "/api/Feed");
        pedido.Headers.Add("Origin", "http://localhost:8081");
        pedido.Headers.Add("Access-Control-Request-Method", "GET");
        var resposta = await _client.SendAsync(pedido);
        Assert.True(
            resposta.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK,
            $"preflight devolveu {(int)resposta.StatusCode}");
        Assert.True(resposta.Headers.TryGetValues("Access-Control-Allow-Origin", out var origens));
        Assert.Contains("http://localhost:8081", origens!);
    }

    private async Task<HttpResponseMessage> Enviar(string metodo, string rota, string? corpo = null, string? token = null)
    {
        var pedido = new HttpRequestMessage(new HttpMethod(metodo), rota);
        if (corpo is not null)
            pedido.Content = new StringContent(corpo, Encoding.UTF8, "application/json");
        if (token is not null)
            pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(pedido);
    }

    private static string EmitirToken(string secret, string issuer, string audience)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(issuer) ? "PrecoCerto" : issuer,
            audience: string.IsNullOrWhiteSpace(audience) ? "PrecoCertoApp" : audience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, ClienteId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, ClienteId.ToString()),
                new Claim(ClaimTypes.Role, "Cliente"),
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class CatalogoApiFactory : WebApplicationFactory<Program>
{
    public const string JwtSecret = "TEST-ONLY-PrecoCerto-Jwt-Secret-32chars!";

    private static readonly Guid LojaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProdutoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OfertaId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid AvaliacaoId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid ClienteId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

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
                ["Jwt:ExpirationHours"] = "24",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();

            var loja = LojaExemplo();
            var produto = ProdutoExemplo(loja);
            var oferta = OfertaExemplo(produto, loja);
            var avaliacao = new Avaliacao
            {
                Id = AvaliacaoId,
                ClienteId = ClienteId,
                LojaId = LojaId,
                Loja = loja,
                Nota = 5,
                Comentario = "Bom",
                DataAvaliacao = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            };

            services.RemoveAll<ILojaServico>();
            var lojas = new Mock<ILojaServico>();
            lojas.Setup(s => s.ListarPaginadoAsync(It.IsAny<PaginacaoParametros>()))
                .ReturnsAsync(Pagina(loja));
            lojas.Setup(s => s.BuscarPorNomePaginadoAsync(It.IsAny<string>(), It.IsAny<PaginacaoParametros>()))
                .ReturnsAsync(Pagina(loja));
            lojas.Setup(s => s.ListarPorProximidadeAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Loja> { loja });
            lojas.Setup(s => s.ObterPorIdAsync(LojaId)).ReturnsAsync(loja);
            services.AddSingleton(lojas.Object);

            services.RemoveAll<IProdutoServico>();
            var produtos = new Mock<IProdutoServico>();
            produtos.Setup(s => s.ListarProdutosPaginadoAsync(It.IsAny<PaginacaoParametros>(), It.IsAny<Guid?>(), It.IsAny<CategoriaProduto?>()))
                .ReturnsAsync(Pagina(produto));
            produtos.Setup(s => s.BuscarPorNomePaginadoAsync(It.IsAny<string>(), It.IsAny<PaginacaoParametros>(), It.IsAny<Guid?>()))
                .ReturnsAsync(Pagina(produto));
            produtos.Setup(s => s.ObterPorIdAsync(ProdutoId)).ReturnsAsync(produto);
            services.AddSingleton(produtos.Object);

            services.RemoveAll<IOfertaServico>();
            var ofertas = new Mock<IOfertaServico>();
            ofertas.Setup(s => s.ListarPaginadoAsync(It.IsAny<PaginacaoParametros>()))
                .ReturnsAsync(Pagina(oferta));
            ofertas.Setup(s => s.ObterPorIdAsync(OfertaId)).ReturnsAsync(oferta);
            ofertas.Setup(s => s.ObterPorProdutoAsync(ProdutoId)).ReturnsAsync(new List<Oferta> { oferta });
            services.AddSingleton(ofertas.Object);

            services.RemoveAll<IFeedServico>();
            var feed = new Mock<IFeedServico>();
            feed.Setup(s => s.ListarAsync(It.IsAny<PaginacaoParametros>(), It.IsAny<string?>(), It.IsAny<CategoriaProduto?>(), It.IsAny<Guid?>()))
                .ReturnsAsync(Pagina(new FeedItem
                {
                    ProdutoId = ProdutoId,
                    Nome = "Pao",
                    LojaId = LojaId,
                    LojaNome = "Padaria",
                    PrecoBase = 5m,
                    PrecoExibicao = 4m,
                    EmPromocao = true,
                    Categoria = CategoriaProduto.Outros
                }));
            services.AddSingleton(feed.Object);

            services.RemoveAll<IAvaliacaoServico>();
            var avaliacoes = new Mock<IAvaliacaoServico>();
            avaliacoes.Setup(s => s.ListarPorLojaAsync(LojaId)).ReturnsAsync(new List<Avaliacao> { avaliacao });
            avaliacoes.Setup(s => s.ListarPorClienteAsync(ClienteId)).ReturnsAsync(new List<Avaliacao> { avaliacao });
            avaliacoes.Setup(s => s.ObterPorIdAsync(AvaliacaoId)).ReturnsAsync(avaliacao);
            avaliacoes.Setup(s => s.ObterResumoAvaliacaoAsync(LojaId)).ReturnsAsync((4.5, 2));
            services.AddSingleton(avaliacoes.Object);

            services.RemoveAll<IHistoricoPesquisaServico>();
            var historico = new Mock<IHistoricoPesquisaServico>();
            historico.Setup(s => s.ObterSugestoesAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<HistoricoPesquisa>
                {
                    new()
                    {
                        Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                        ClienteId = ClienteId,
                        TermoPesquisa = "pao",
                        DataPesquisa = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
                    }
                });
            services.AddSingleton(historico.Object);

            services.RemoveAll<IClimaServico>();
            var clima = new Mock<IClimaServico>();
            clima.Setup(s => s.ObterPorCoordenadasAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClimaResposta
                {
                    Atual = new ClimaAtual { Temperatura = 26m, Descricao = "Ensolarado" }
                });
            services.AddSingleton(clima.Object);
        });
    }

    private static Loja LojaExemplo() => new()
    {
        Id = LojaId,
        NomeFantasia = "Padaria",
        RazaoSocial = "Padaria Ltda",
        Cnpj = "11222333000181",
        Email = "dono.secreto@loja.example",
        Telefone = "21988887777",
        UsuarioId = ClienteId,
        Endereco = new Endereco
        {
            Logradouro = "Rua A",
            Cidade = "Rio",
            Latitude = -22.9m,
            Longitude = -43.2m
        }
    };

    private static Produto ProdutoExemplo(Loja loja) => new()
    {
        Id = ProdutoId,
        NomeProduto = "Pao",
        Marca = "Casa",
        Preco = 5m,
        LojaId = LojaId,
        Loja = loja,
        Categoria = CategoriaProduto.Outros
    };

    private static Oferta OfertaExemplo(Produto produto, Loja loja) => new()
    {
        Id = OfertaId,
        ProdutoId = ProdutoId,
        Produto = produto,
        LojaId = LojaId,
        Loja = loja,
        Preco = 4m,
        Disponivel = true,
        EmPromocao = true
    };

    private static PaginacaoResultado<T> Pagina<T>(T item) => new()
    {
        Items = new[] { item },
        Page = 1,
        PageSize = 20,
        Total = 1
    };
}
