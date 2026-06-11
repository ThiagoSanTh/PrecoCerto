using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Dominio.Validacoes;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos;

namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Serviço do usuário (entidade unificada Usuario): autenticação, perfil,
    /// localização e papéis (Cliente/Lojista/Vendedor).
    /// </summary>
    public class ClienteServico : IClienteServico
    {
        private readonly IClienteRepositorio _clienteRepositorio;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IValidadorEmail _validadorEmail;

        public ClienteServico(
            IClienteRepositorio clienteRepositorio,
            IPasswordHasher passwordHasher,
            IValidadorEmail validadorEmail)
        {
            _clienteRepositorio = clienteRepositorio;
            _passwordHasher = passwordHasher;
            _validadorEmail = validadorEmail;
        }

        public async Task<Usuario> RegistrarAsync(Usuario cliente)
        {
            if (string.IsNullOrWhiteSpace(cliente.Email))
                throw new Exception("Email é obrigatório.");

            await _validadorEmail.ValidarAsync(cliente.Email);

            cliente.Email = EmailValidator.Normalizar(cliente.Email);

            if (string.IsNullOrWhiteSpace(cliente.NomeUsuario))
                throw new Exception("Nome de usuário é obrigatório.");

            if (string.IsNullOrWhiteSpace(cliente.SenhaHash) || cliente.SenhaHash.Length < 6)
                throw new Exception("Senha deve ter pelo menos 6 caracteres.");

            var existente = await _clienteRepositorio.ObterPorEmailCadastroAsync(cliente.Email);
            if (existente != null)
                throw new EmailJaRegistradoException();

            cliente.Ativo = true;
            cliente.DataCriacao = DateTime.UtcNow;
            cliente.Tipo = TipoUsuario.Cliente;
            cliente.Papel = PapelUsuario.Cliente;
            cliente.SenhaHash = _passwordHasher.Hash(cliente.SenhaHash);
            cliente.EmailConfirmado = true;
            cliente.TokenConfirmacao = null;

            return await _clienteRepositorio.AdicionarAsync(cliente);
        }

        public async Task<Usuario?> ValidarLoginAsync(string email, string senha)
        {
            var result = await ValidarLoginDetalhadoAsync(email, senha);
            return result.Usuario;
        }

        public async Task<LoginValidacaoResult> ValidarLoginDetalhadoAsync(string email, string senha)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
                return LoginValidacaoResult.EmailNaoEncontrado();

            var cliente = await _clienteRepositorio.ObterPorEmailAsync(EmailValidator.Normalizar(email));

            if (cliente == null)
                return LoginValidacaoResult.EmailNaoEncontrado();

            if (!await VerificarSenhaAsync(cliente, senha))
                return LoginValidacaoResult.SenhaIncorreta();

            try
            {
                var ultimoLogin = DateTime.UtcNow;
                await _clienteRepositorio.AtualizarUltimoLoginAsync(cliente.Id, ultimoLogin);
                cliente.UltimoLogin = ultimoLogin;
            }
            catch
            {
                // Não impede login se atualização de UltimoLogin falhar
            }

            return LoginValidacaoResult.Ok(cliente);
        }

        public async Task<Usuario?> ObterPorIdAsync(Guid id)
        {
            return await _clienteRepositorio.ObterPorIdAsync(id);
        }

        public async Task<Usuario?> ObterComLojaAsync(Guid id)
        {
            return await _clienteRepositorio.ObterPorIdComLojaAsync(id);
        }

        public async Task<Usuario?> ObterPorEmailAsync(string email)
        {
            return await _clienteRepositorio.ObterPorEmailAsync(email);
        }

        public async Task<List<Usuario>> ListarAtivosAsync()
        {
            return await _clienteRepositorio.ListarAtivosAsync();
        }

        public async Task<List<Usuario>> ListarAsync()
        {
            return await _clienteRepositorio.ListarAsync();
        }

        public async Task AtualizarAsync(Usuario cliente)
        {
            cliente.DataAtualizacao = DateTime.UtcNow;
            await _clienteRepositorio.AtualizarAsync(cliente);
        }

        public async Task AtualizarLocalizacaoAsync(Guid clienteId, decimal latitude, decimal longitude)
        {
            if (latitude < -90 || latitude > 90)
                throw new Exception("Latitude fora do intervalo válido (-90 a 90).");

            if (longitude < -180 || longitude > 180)
                throw new Exception("Longitude fora do intervalo válido (-180 a 180).");

            await _clienteRepositorio.AtualizarLocalizacaoAsync(clienteId, latitude, longitude);
        }

        public async Task<List<Usuario>> ObterPorProximidadeAsync(decimal latitude, decimal longitude, decimal raioKm)
        {
            if (raioKm <= 0)
                throw new Exception("Raio deve ser maior que zero.");

            return await _clienteRepositorio.ObterPorProximidadeAsync(latitude, longitude, raioKm);
        }

        public async Task AlterarSenhaAsync(Guid clienteId, string senhaAtual, string novaSenha)
        {
            if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6)
                throw new Exception("Senha deve ter pelo menos 6 caracteres.");

            var cliente = await _clienteRepositorio.ObterPorIdAsync(clienteId);
            if (cliente == null)
                throw new Exception("Usuário não encontrado.");

            if (!await VerificarSenhaAsync(cliente, senhaAtual))
                throw new Exception("Senha atual incorreta.");

            cliente.SenhaHash = _passwordHasher.Hash(novaSenha);
            await _clienteRepositorio.AtualizarAsync(cliente);
        }

        private async Task<bool> VerificarSenhaAsync(Usuario cliente, string senha)
        {
            if (_passwordHasher.Verify(senha, cliente.SenhaHash))
                return true;

            if (!_passwordHasher.IsBcryptHash(cliente.SenhaHash) && cliente.SenhaHash == senha)
            {
                cliente.SenhaHash = _passwordHasher.Hash(senha);
                await _clienteRepositorio.AtualizarAsync(cliente);
                return true;
            }

            return false;
        }

        public async Task RemoverAsync(Guid id)
        {
            var cliente = await _clienteRepositorio.ObterPorIdAsync(id);
            if (cliente != null)
            {
                cliente.Ativo = false;
                await _clienteRepositorio.AtualizarAsync(cliente);
            }
        }

        public async Task<string?> GerarTokenRecuperacaoSenhaAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var cliente = await _clienteRepositorio.ObterPorEmailAsync(email.Trim());
            if (cliente == null)
                return null;

            var token = Guid.NewGuid().ToString("N");
            cliente.TokenRecuperacaoSenha = token;
            cliente.TokenRecuperacaoExpira = DateTime.UtcNow.AddHours(1);
            await _clienteRepositorio.AtualizarAsync(cliente);
            return token;
        }

        public async Task<bool> RedefinirSenhaComTokenAsync(string token, string novaSenha)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6)
                throw new Exception("Senha deve ter pelo menos 6 caracteres.");

            var cliente = await _clienteRepositorio.ObterPorTokenRecuperacaoSenhaAsync(token);
            if (cliente == null)
                return false;

            cliente.SenhaHash = _passwordHasher.Hash(novaSenha);
            cliente.TokenRecuperacaoSenha = null;
            cliente.TokenRecuperacaoExpira = null;
            await _clienteRepositorio.AtualizarAsync(cliente);
            return true;
        }

        public async Task AlterarEmailAsync(Guid usuarioId, string senhaAtual, string novoEmail)
        {
            var cliente = await _clienteRepositorio.ObterPorIdAsync(usuarioId)
                ?? throw new Exception("Usuário não encontrado.");

            if (!await VerificarSenhaAsync(cliente, senhaAtual))
                throw new Exception("Senha atual incorreta.");

            await _validadorEmail.ValidarAsync(novoEmail);
            var emailNormalizado = EmailValidator.Normalizar(novoEmail);

            var existente = await _clienteRepositorio.ObterPorEmailCadastroAsync(emailNormalizado);
            if (existente != null && existente.Id != usuarioId)
                throw new EmailJaRegistradoException();

            cliente.Email = emailNormalizado;
            cliente.EmailConfirmado = true;
            await _clienteRepositorio.AtualizarAsync(cliente);
        }

        public async Task DefinirComoLojistaAsync(Guid usuarioId)
        {
            var usuario = await _clienteRepositorio.ObterPorIdAsync(usuarioId);
            if (usuario == null)
                throw new Exception("Usuário não encontrado.");

            usuario.Papel = PapelUsuario.Lojista;
            usuario.Tipo = TipoUsuario.Lojista;
            await _clienteRepositorio.AtualizarAsync(usuario);
        }

        public async Task PromoverParaVendedorAsync(Guid usuarioId, Guid lojaId, string? cargo)
        {
            var usuario = await _clienteRepositorio.ObterPorIdAsync(usuarioId);
            if (usuario == null)
                throw new Exception("Usuário não encontrado.");

            if (usuario.Papel == PapelUsuario.Lojista)
                throw new Exception("Lojistas não podem ser promovidos a vendedores.");

            usuario.Papel = PapelUsuario.Vendedor;
            usuario.LojaVinculadaId = lojaId;
            usuario.Cargo = string.IsNullOrWhiteSpace(cargo) ? "Vendedor" : cargo;
            await _clienteRepositorio.AtualizarAsync(usuario);
        }

        public async Task RemoverVendedorAsync(Guid usuarioId)
        {
            var usuario = await _clienteRepositorio.ObterPorIdAsync(usuarioId);
            if (usuario == null)
                throw new Exception("Usuário não encontrado.");

            if (usuario.Papel != PapelUsuario.Vendedor)
                throw new Exception("Usuário não é um vendedor.");

            usuario.Papel = PapelUsuario.Cliente;
            usuario.LojaVinculadaId = null;
            usuario.Cargo = null;
            await _clienteRepositorio.AtualizarAsync(usuario);
        }

        public async Task<List<Usuario>> ListarVendedoresPorLojaAsync(Guid lojaId)
        {
            var todos = await _clienteRepositorio.ListarAtivosAsync();
            return todos.FindAll(u => u.Papel == PapelUsuario.Vendedor && u.LojaVinculadaId == lojaId);
        }
    }
}
