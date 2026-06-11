using Pc.Dominio.Comum;
using Pc.WebApi.DTOs.Comum;

namespace Pc.WebApi.Helpers
{
    public static class PaginacaoHelper
    {
        public static PaginacaoParametros Normalizar(PaginacaoConsultaDto? consulta, int maxPageSize = 100)
        {
            var page = consulta?.Page ?? 1;
            var pageSize = consulta?.PageSize ?? 20;
            return PaginacaoParametros.De(page, pageSize, maxPageSize);
        }

        public static PaginacaoParametros Normalizar(int page, int pageSize, int maxPageSize = 100) =>
            PaginacaoParametros.De(page, pageSize, maxPageSize);

        public static PaginacaoRespostaDto<TDestino> ParaResposta<TOrigem, TDestino>(
            PaginacaoResultado<TOrigem> resultado,
            Func<TOrigem, TDestino> mapper)
        {
            return new PaginacaoRespostaDto<TDestino>
            {
                Items = resultado.Items.Select(mapper).ToList(),
                Page = resultado.Page,
                PageSize = resultado.PageSize,
                Total = resultado.Total,
                HasNext = resultado.HasNext
            };
        }

        public static PaginacaoRespostaDto<T> ParaResposta<T>(PaginacaoResultado<T> resultado) =>
            new()
            {
                Items = resultado.Items.ToList(),
                Page = resultado.Page,
                PageSize = resultado.PageSize,
                Total = resultado.Total,
                HasNext = resultado.HasNext
            };
    }
}
