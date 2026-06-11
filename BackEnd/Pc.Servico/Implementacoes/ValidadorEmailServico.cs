using Pc.Dominio.Validacoes;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class ValidadorEmailServico : IValidadorEmail
    {
        public async Task ValidarAsync(string email, CancellationToken cancellationToken = default)
        {
            if (!EmailValidator.IsValido(email))
                throw new EmailInvalidoException();

            if (!await EmailValidator.DominioAceitaEmailAsync(email, cancellationToken))
                throw new EmailInvalidoException();
        }
    }
}
