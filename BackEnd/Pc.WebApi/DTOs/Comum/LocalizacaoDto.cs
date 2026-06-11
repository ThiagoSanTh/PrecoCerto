using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Comum
{
    public class LocalizacaoDto
    {
        [Range(-90, 90, ErrorMessage = "Latitude deve estar entre -90 e 90.")]
        public decimal Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude deve estar entre -180 e 180.")]
        public decimal Longitude { get; set; }
    }
}
