namespace Pc.WebApi.DTOs.Comum
{
    public class PaginacaoRespostaDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public bool HasNext { get; set; }
    }
}
