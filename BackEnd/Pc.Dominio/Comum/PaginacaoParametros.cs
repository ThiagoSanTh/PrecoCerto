namespace Pc.Dominio.Comum
{
    public readonly struct PaginacaoParametros
    {
        public int Page { get; }
        public int PageSize { get; }
        public int Skip { get; }

        public PaginacaoParametros(int page, int pageSize, int maxPageSize = 100)
        {
            Page = page < 1 ? 1 : page;
            PageSize = pageSize < 1 ? 20 : Math.Min(pageSize, maxPageSize);
            Skip = (Page - 1) * PageSize;
        }

        public static PaginacaoParametros De(int page = 1, int pageSize = 20, int maxPageSize = 100) =>
            new(page, pageSize, maxPageSize);
    }
}
