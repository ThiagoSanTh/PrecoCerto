using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Pc.Dominio.Comum;
using Xunit;

namespace Pc.Servico.Tests;

/// <summary>
/// B1: o teto de catálogo e o de login são por IP, não um balde só para o processo.
/// </summary>
public class LimitePorIpTests : IClassFixture<CatalogoApiFactory>
{
    private readonly CatalogoApiFactory _factory;

    public LimitePorIpTests(CatalogoApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Feed_de_outro_ip_nao_herda_o_429()
    {
        var ipA = IPAddress.Parse("198.51.100.10");
        var ipB = IPAddress.Parse("203.0.113.7");

        for (var i = 0; i < 120; i++)
            await Enviar(_factory, "GET", "/api/Feed", ipA);

        var estouro = await Enviar(_factory, "GET", "/api/Feed", ipA);
        var outro = await Enviar(_factory, "GET", "/api/Feed", ipB);

        Assert.Equal(HttpStatusCode.TooManyRequests, estouro);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, outro);
    }

    [Fact]
    public async Task Login_de_outro_ip_nao_herda_o_429()
    {
        var ipA = IPAddress.Parse("198.51.100.11");
        var ipB = IPAddress.Parse("203.0.113.8");

        HttpStatusCode ultimoA = 0;
        for (var i = 0; i < 11; i++)
            ultimoA = await Enviar(_factory, "POST", "/api/Auth/login", ipA, "{}");

        var outro = await Enviar(_factory, "POST", "/api/Auth/login", ipB, "{}");

        Assert.Equal(HttpStatusCode.TooManyRequests, ultimoA);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, outro);
    }

    [Fact]
    public async Task Limite_estourado_devolve_retry_after_e_mensagem()
    {
        using var client = _factory.CreateClient();
        HttpResponseMessage? ultima = null;
        for (var i = 0; i < 11; i++)
        {
            ultima = await client.PostAsync(
                "/api/Auth/login",
                new StringContent("{}", Encoding.UTF8, "application/json"));
        }

        Assert.NotNull(ultima);
        Assert.Equal(HttpStatusCode.TooManyRequests, ultima!.StatusCode);
        Assert.True(ultima.Headers.RetryAfter?.Delta is { } espera && espera > TimeSpan.Zero);
        var corpo = await ultima.Content.ReadAsStringAsync();
        Assert.Contains("Muitas requisições", corpo, StringComparison.Ordinal);
    }

    internal static async Task<HttpStatusCode> Enviar(
        CatalogoApiFactory factory,
        string metodo,
        string rota,
        IPAddress ip,
        string? corpo = null,
        string? xff = null)
    {
        var ctx = await factory.Server.SendAsync(c =>
        {
            c.Request.Method = metodo;
            var q = rota.IndexOf('?');
            c.Request.Path = q < 0 ? rota : rota[..q];
            if (q >= 0)
                c.Request.QueryString = new QueryString(rota[q..]);
            c.Connection.RemoteIpAddress = ip;
            if (xff is not null)
                c.Request.Headers["X-Forwarded-For"] = xff;
            if (corpo is not null)
            {
                var bytes = Encoding.UTF8.GetBytes(corpo);
                c.Request.ContentType = "application/json";
                c.Request.Body = new MemoryStream(bytes);
                c.Request.ContentLength = bytes.Length;
            }
        });
        return (HttpStatusCode)ctx.Response.StatusCode;
    }
}

/// <summary>
/// B2: login, catálogo e o restante da API não compartilham o mesmo contador.
/// </summary>
public class LimitePorFaixaTests : IClassFixture<CatalogoApiFactory>
{
    private readonly CatalogoApiFactory _factory;

    public LimitePorFaixaTests(CatalogoApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Feed_nao_reduz_o_teto_de_300_do_restante_da_api()
    {
        var ip = IPAddress.Parse("198.51.100.20");
        await LimitePorIpTests.Enviar(_factory, "GET", "/api/Feed", ip);

        var estouros = 0;
        for (var i = 0; i < 125; i++)
        {
            var status = await LimitePorIpTests.Enviar(
                _factory, "GET", "/api/Clientes/eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee", ip);
            if (status == HttpStatusCode.TooManyRequests)
                estouros++;
        }

        Assert.Equal(0, estouros);
    }

    [Fact]
    public async Task Um_login_nao_deixa_o_feed_no_teto_de_10()
    {
        var ip = IPAddress.Parse("198.51.100.21");
        await LimitePorIpTests.Enviar(_factory, "POST", "/api/Auth/login", ip, "{}");

        var estouros = 0;
        for (var i = 0; i < 15; i++)
        {
            var status = await LimitePorIpTests.Enviar(_factory, "GET", "/api/Feed", ip);
            if (status == HttpStatusCode.TooManyRequests)
                estouros++;
        }

        Assert.Equal(0, estouros);
    }
}

/// <summary>
/// B2b: X-Forwarded-For forjado não abre um balde novo. O proxy confiável, sim.
/// </summary>
public class XForwardedForTests : IClassFixture<CatalogoApiFactory>
{
    private readonly CatalogoApiFactory _factory;

    public XForwardedForTests(CatalogoApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Xff_forjado_em_conexao_publica_nao_burla_o_limite()
    {
        var socket = IPAddress.Parse("203.0.113.50");
        var ok = 0;
        var limitado = 0;
        for (var i = 1; i <= 310; i++)
        {
            var status = await LimitePorIpTests.Enviar(
                _factory,
                "GET",
                "/api/Clientes/eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
                socket,
                xff: $"198.51.{i / 256}.{i % 256}");
            if (status == HttpStatusCode.TooManyRequests)
                limitado++;
            else
                ok++;
        }

        Assert.True(limitado > 0, "as 310 chamadas passaram; o XFF forjado virou um IP novo");
        Assert.InRange(ok, 1, 300);
    }

    [Fact]
    public async Task Proxy_privado_usa_so_o_ultimo_hop_do_xff()
    {
        var proxy = IPAddress.Parse("10.1.0.5");
        HttpStatusCode ultimo = 0;
        for (var i = 0; i < 121; i++)
        {
            ultimo = await LimitePorIpTests.Enviar(
                _factory,
                "GET",
                "/api/Feed",
                proxy,
                xff: $"1.2.3.{i % 50}, 203.0.113.77");
        }

        var outroCliente = await LimitePorIpTests.Enviar(
            _factory,
            "GET",
            "/api/Feed",
            proxy,
            xff: "1.2.3.9, 203.0.113.78");

        Assert.Equal(HttpStatusCode.TooManyRequests, ultimo);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, outroCliente);
    }

    [Fact]
    public async Task Ip_publico_em_100_0_0_0_8_nao_forja_xff()
    {
        var socket = IPAddress.Parse("100.1.2.3");
        for (var i = 0; i < 120; i++)
        {
            await LimitePorIpTests.Enviar(
                _factory, "GET", "/api/Feed", socket, xff: "203.0.113.77");
        }

        var outroHop = await LimitePorIpTests.Enviar(
            _factory, "GET", "/api/Feed", socket, xff: "203.0.113.78");

        Assert.Equal(HttpStatusCode.TooManyRequests, outroHop);
    }

    [Fact]
    public async Task Cgnat_100_64_confia_no_ultimo_hop()
    {
        var proxy = IPAddress.Parse("100.64.0.2");
        HttpStatusCode ultimo = 0;
        for (var i = 0; i < 121; i++)
        {
            ultimo = await LimitePorIpTests.Enviar(
                _factory, "GET", "/api/Feed", proxy, xff: "203.0.113.77");
        }

        var outroCliente = await LimitePorIpTests.Enviar(
            _factory, "GET", "/api/Feed", proxy, xff: "203.0.113.79");

        Assert.Equal(HttpStatusCode.TooManyRequests, ultimo);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, outroCliente);
    }
}

public class EntradaPublicaTests : IClassFixture<CatalogoApiFactory>
{
    private readonly HttpClient _client;

    public EntradaPublicaTests(CatalogoApiFactory factory) => _client = factory.CreateClient();

    [Theory]
    [InlineData("/api/Feed?page=107374184&pageSize=20")]
    [InlineData("/api/Lojas?page=107374184&pageSize=20")]
    [InlineData("/api/Produtos?page=107374184&pageSize=20")]
    [InlineData("/api/Ofertas?page=107374184&pageSize=20")]
    public async Task Pagina_que_estoura_offset_nao_devolve_500(string rota)
    {
        var resposta = await _client.GetAsync(rota);
        Assert.NotEqual(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Busca_de_loja_com_pagina_enorme_nao_devolve_500()
    {
        var resposta = await _client.PostAsync(
            "/api/Lojas/buscar?page=107374184&pageSize=20",
            new StringContent("\"pao\"", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public void Parametros_de_pagina_enorme_nao_geram_skip_negativo()
    {
        var erro = Assert.Throws<PaginacaoInvalidaException>(() => PaginacaoParametros.De(107374184, 20));
        Assert.NotNull(erro);
    }

    [Theory]
    [InlineData("/api/Avaliacoes/loja/00000000-0000-0000-0000-000000000000")]
    [InlineData("/api/Avaliacoes/loja/00000000-0000-0000-0000-000000000000/media")]
    public async Task Avaliacao_de_loja_com_guid_vazio_nao_devolve_500(string rota)
    {
        var resposta = await _client.GetAsync(rota);
        Assert.NotEqual(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Sugestao_com_nul_nao_devolve_500()
    {
        var resposta = await _client.GetAsync("/api/HistoricoPesquisa/sugestoes?termo=%00");
        Assert.NotEqual(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Theory]
    [InlineData("/api/HistoricoPesquisa/sugestoes")]
    [InlineData("/api/HistoricoPesquisa/sugestoes?termo=")]
    [InlineData("/api/HistoricoPesquisa/sugestoes?termo=%20")]
    [InlineData("/api/HistoricoPesquisa/sugestoes?termo=%09")]
    [InlineData("/api/HistoricoPesquisa/sugestoes?termo=%0A")]
    [InlineData("/api/HistoricoPesquisa/sugestoes?termo=%0D%0A")]
    public async Task Sugestao_sem_termo_devolve_lista_vazia(string rota)
    {
        var resposta = await _client.GetAsync(rota);
        var texto = (await resposta.Content.ReadAsStringAsync()).Trim();
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("[]", texto);
    }

    [Fact]
    public async Task LojaId_invalido_vem_em_portugues()
    {
        var resposta = await _client.GetAsync("/api/Feed?lojaId=abc");
        var texto = await resposta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.DoesNotContain("is not valid", texto, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("não é válido", texto, StringComparison.Ordinal);
    }
}
