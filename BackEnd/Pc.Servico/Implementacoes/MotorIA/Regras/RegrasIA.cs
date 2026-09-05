using Pc.Dominio.Enums.MotorIA;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA.Regras
{
    public class RegraEconomizar : IRegraIA
    {
        public string Nome => "USUARIO_QUER_ECONOMIZAR";

        public bool Aplica(ContextoIA c) =>
            c.Objetivo == ObjetivoIA.Economizar
            || c.PreferenciaPreco == NivelPreferenciaIA.Alta
            || c.Intencao == IntencaoIA.BuscarProdutoMaisBarato;

        public void Aplicar(ContextoIA c)
        {
            c.Pesos.Preco = 40;
            c.Pesos.Promocao = 20;
            c.Pesos.Distancia = 15;
            c.Pesos.Entrega = 10;
            c.Pesos.Avaliacao = 10;
            c.Pesos.Disponibilidade = 5;
            c.Pesos.Conveniencia = 5;
        }
    }

    public class RegraUrgencia : IRegraIA
    {
        public string Nome => "URGENCIA";

        public bool Aplica(ContextoIA c) =>
            c.UrgenciaAlta || c.Objetivo == ObjetivoIA.Rapidez;

        public void Aplicar(ContextoIA c)
        {
            c.Pesos.Disponibilidade = Math.Max(c.Pesos.Disponibilidade, 25);
            c.Pesos.Distancia = Math.Max(c.Pesos.Distancia, 25);
            c.Pesos.Preco = Math.Min(c.Pesos.Preco, 20);
        }
    }

    public class RegraChuva : IRegraIA
    {
        public string Nome => "CHUVA";

        public bool Aplica(ContextoIA c) => c.ClimaDisponivel && c.Chuva == true;

        public void Aplicar(ContextoIA c)
        {
            c.Pesos.Preco = 30;
            c.Pesos.Promocao = 15;
            c.Pesos.Distancia = 20;
            c.Pesos.Entrega = 25;
            c.Pesos.Avaliacao = 5;
            c.Pesos.Disponibilidade = 5;
            c.Pesos.Conveniencia = 10;
        }
    }

    public class RegraEntrega : IRegraIA
    {
        public string Nome => "NECESSITA_ENTREGA";

        public bool Aplica(ContextoIA c) =>
            c.NecessitaEntrega || c.Intencao == IntencaoIA.ConsultarEntrega;

        public void Aplicar(ContextoIA c)
        {
            c.Pesos.Entrega = Math.Max(c.Pesos.Entrega, 30);
            c.Pesos.Distancia = Math.Max(c.Pesos.Distancia, 20);
            c.Pesos.Conveniencia = Math.Max(c.Pesos.Conveniencia, 15);
        }
    }

    public class RegraProximidade : IRegraIA
    {
        public string Nome => "PROXIMIDADE";

        public bool Aplica(ContextoIA c) =>
            c.PreferenciaDistancia == NivelPreferenciaIA.Alta
            || c.Objetivo == ObjetivoIA.Proximidade
            || c.Intencao == IntencaoIA.BuscarLojaMaisProxima;

        public void Aplicar(ContextoIA c)
        {
            c.Pesos.Distancia = Math.Max(c.Pesos.Distancia, 35);
            c.Pesos.Conveniencia = Math.Max(c.Pesos.Conveniencia, 15);
        }
    }

    public class RegraPromocao : IRegraIA
    {
        public string Nome => "BUSCA_PROMOCAO";

        public bool Aplica(ContextoIA c) =>
            c.Intencao == IntencaoIA.BuscarPromocao || c.Objetivo == ObjetivoIA.Promocao;

        public void Aplicar(ContextoIA c)
        {
            c.Pesos.Promocao = Math.Max(c.Pesos.Promocao, 35);
            c.Pesos.Preco = Math.Max(c.Pesos.Preco, 25);
        }
    }

    public class RegraQualidade : IRegraIA
    {
        public string Nome => "QUALIDADE";

        public bool Aplica(ContextoIA c) =>
            c.PreferenciaQualidade == NivelPreferenciaIA.Alta || c.Objetivo == ObjetivoIA.Qualidade;

        public void Aplicar(ContextoIA c)
        {
            c.Pesos.Avaliacao = Math.Max(c.Pesos.Avaliacao, 35);
            c.Pesos.Preco = Math.Min(c.Pesos.Preco, 20);
        }
    }
}
