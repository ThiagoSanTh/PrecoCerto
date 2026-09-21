using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA
{
    public class MotorRegrasIA : IMotorRegrasIA
    {
        private readonly IReadOnlyList<IRegraIA> _regras;

        public MotorRegrasIA(IEnumerable<IRegraIA> regras)
        {
            _regras = regras.ToList();
        }

        public void Aplicar(ContextoIA contexto)
        {
            foreach (var regra in _regras)
            {
                if (!regra.Aplica(contexto))
                    continue;
                regra.Aplicar(contexto);
                if (!contexto.RegrasAplicadas.Contains(regra.Nome))
                    contexto.RegrasAplicadas.Add(regra.Nome);
            }

            // Sem localização: não pontuar distância
            if (contexto.Latitude is null || contexto.Longitude is null)
            {
                contexto.Pesos.Distancia = 0;
                if (!contexto.FallbacksUsados.Contains("sem_localizacao"))
                    contexto.FallbacksUsados.Add("sem_localizacao");
            }
        }
    }
}
