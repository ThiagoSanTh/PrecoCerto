namespace Pc.Dominio.Comum
{
    public class PaginacaoResultado<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int Total { get; init; }
        public bool HasNext => Page * PageSize < Total;
    }
}
