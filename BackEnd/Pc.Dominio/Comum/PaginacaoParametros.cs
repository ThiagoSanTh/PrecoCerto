namespace Pc.Dominio.Comum
{
    public sealed class PaginacaoInvalidaException : ArgumentOutOfRangeException
    {
        public PaginacaoInvalidaException()
            : base("page", "Página fora do intervalo que a consulta aceita.")
        {
        }
    }

    public readonly struct PaginacaoParametros
    {
        public int Page { get; }
        public int PageSize { get; }
        public int Skip { get; }

        public PaginacaoParametros(int page, int pageSize, int maxPageSize = 100)
        {
            Page = page < 1 ? 1 : page;
            PageSize = pageSize < 1 ? 20 : Math.Min(pageSize, maxPageSize);
            var skip = (long)(Page - 1) * PageSize;
            if (skip > int.MaxValue)
                throw new PaginacaoInvalidaException();
            Skip = (int)skip;
        }

        public static PaginacaoParametros De(int page = 1, int pageSize = 20, int maxPageSize = 100) =>
            new(page, pageSize, maxPageSize);
    }
}
