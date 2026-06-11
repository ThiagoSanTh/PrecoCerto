using Pc.Dominio.Entities.Usuarios;

namespace Pc.Servico.Modelos
{
    public enum LoginErroCodigo
    {
        None,
        EmailNaoEncontrado,
        SenhaIncorreta
    }

    public class LoginValidacaoResult
    {
        public LoginErroCodigo Erro { get; init; }
        public Usuario? Usuario { get; init; }

        public bool Sucesso => Erro == LoginErroCodigo.None && Usuario != null;

        public static LoginValidacaoResult Ok(Usuario usuario) =>
            new() { Erro = LoginErroCodigo.None, Usuario = usuario };

        public static LoginValidacaoResult EmailNaoEncontrado() =>
            new() { Erro = LoginErroCodigo.EmailNaoEncontrado };

        public static LoginValidacaoResult SenhaIncorreta() =>
            new() { Erro = LoginErroCodigo.SenhaIncorreta };
    }
}
