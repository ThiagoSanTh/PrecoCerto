using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Interfaces.MotorIA
{
    public interface IValidadorRespostaIA
    {
        bool Validar(string resposta, ContextoIA contexto);
    }
}
