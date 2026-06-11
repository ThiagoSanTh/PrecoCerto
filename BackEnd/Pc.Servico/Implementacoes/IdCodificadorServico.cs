using Microsoft.Extensions.Options;
using Pc.Servico.Interfaces;
using Sqids;

namespace Pc.Servico.Implementacoes
{
    public class IdEncodingSettings
    {
        public const string SectionName = "IdEncoding";
        public string Alphabet { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public int MinLength { get; set; } = 10;
    }

    public class IdCodificadorServico : IIdCodificador
    {
        private readonly SqidsEncoder<int> _encoder;

        public IdCodificadorServico(IOptions<IdEncodingSettings> options)
        {
            var settings = options.Value;
            _encoder = new SqidsEncoder<int>(new()
            {
                MinLength = settings.MinLength,
                Alphabet = settings.Alphabet
            });
        }

        public string Codificar(Guid id)
        {
            var nums = id.ToByteArray().Select(b => (int)b).ToList();
            return _encoder.Encode(nums);
        }

        public Guid? Decodificar(string codigo)
        {
            return TentarDecodificar(codigo, out var id) ? id : null;
        }

        public bool TentarDecodificar(string codigo, out Guid id)
        {
            id = Guid.Empty;
            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            if (Guid.TryParse(codigo, out id))
                return true;

            try
            {
                var nums = _encoder.Decode(codigo);
                if (nums.Count != 16)
                    return false;

                id = new Guid(nums.Select(n => (byte)n).ToArray());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
