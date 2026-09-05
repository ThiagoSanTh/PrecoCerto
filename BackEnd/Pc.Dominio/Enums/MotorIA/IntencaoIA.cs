namespace Pc.Dominio.Enums.MotorIA
{
    public enum IntencaoIA
    {
        BuscarProduto = 0,
        BuscarOferta = 1,
        BuscarLoja = 2,
        CompararPrecos = 3,
        BuscarPromocao = 4,
        BuscarProdutoMaisBarato = 5,
        BuscarLojaMaisProxima = 6,
        RecomendarProduto = 7,
        RecomendarLoja = 8,
        BuscarProdutosRelacionados = 9,
        ConsultarDisponibilidade = 10,
        ConsultarEntrega = 11,
        MontarCesta = 12,
        ForaDoDominio = 13,
        NaoEntendida = 14
    }

    public enum ObjetivoIA
    {
        Economizar = 0,
        Rapidez = 1,
        Proximidade = 2,
        Qualidade = 3,
        Promocao = 4,
        Conveniencia = 5,
        Disponibilidade = 6
    }

    public enum NivelPreferenciaIA
    {
        Neutro = 0,
        Baixa = 1,
        Media = 2,
        Alta = 3
    }
}
