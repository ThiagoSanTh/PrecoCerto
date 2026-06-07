using Pc.Dominio.Entities.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pc.Dominio.Entities.Catalogo
{
    public class Endereco : BaseEntity
    {
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
