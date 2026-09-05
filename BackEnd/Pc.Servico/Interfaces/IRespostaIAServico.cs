using Pc.Servico.Modelos.IA;

namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Gera texto humano a partir de dados estruturados do Preço Certo.
    /// Fase 1: implementação determinística. Fase 3: pode ser substituída por LLM.
    /// </summary>
    public interface IRespostaIAServico
    {
        string Gerar(IAContextoResposta contexto);
    }
}
