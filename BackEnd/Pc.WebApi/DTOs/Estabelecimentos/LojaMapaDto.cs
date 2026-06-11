namespace Pc.WebApi.DTOs.Estabelecimentos
{
    public class LojaMapaDto
    {
        public Guid Id { get; set; }
        public string CodigoPublico { get; set; } = string.Empty;
        public string NomeFantasia { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
