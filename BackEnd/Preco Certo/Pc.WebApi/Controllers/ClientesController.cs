using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Usuarios;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Usuarios;

namespace Pc.WebApi.Controllers
{
    /// <summary>
    /// Controller para operações com Clientes
    /// Endpoints para autenticação, gerenciar perfil e localização
    /// Rota base: /api/clientes
    /// Consolidado - sem intermediário Usuario
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteServico _clienteServico;

        public ClientesController(IClienteServico clienteServico)
        {
            _clienteServico = clienteServico;
        }

        /// <summary>
        /// POST: /api/clientes/registrar
        /// Registra um novo cliente (signup)
        /// Body: ClienteCriarDto
        /// </summary>
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] ClienteCriarDto dto)
        {
            var cliente = new Cliente
            {
                NomeUsuario = dto.NomeUsuario,
                Email = dto.Email,
                SenhaHash = dto.Senha, // TODO: Hash com bcrypt
                Telefone = dto.Telefone,
                LatitudeAtual = dto.LatitudeAtual,
                LongitudeAtual = dto.LongitudeAtual
            };

            var novoCliente = await _clienteServico.RegistrarAsync(cliente);

            var resposta = new ClienteRespostaDto
            {
                Id = novoCliente.Id,
                NomeUsuario = novoCliente.NomeUsuario,
                Email = novoCliente.Email,
                Telefone = novoCliente.Telefone,
                Tipo = (int)novoCliente.Tipo,
                UltimoLogin = novoCliente.UltimoLogin,
                LatitudeAtual = novoCliente.LatitudeAtual,
                LongitudeAtual = novoCliente.LongitudeAtual,
                Ativo = novoCliente.Ativo,
                DataCriacao = novoCliente.DataCriacao
            };

            return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
        }

        /// <summary>
        /// POST: /api/clientes/login
        /// Valida credenciais de login
        /// Body: { "email": "user@email.com", "senha": "123456" }
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var cliente = await _clienteServico.ValidarLoginAsync(dto.Email, dto.Senha);

            if (cliente == null)
                return Unauthorized("Email ou senha incorretos.");

            var resposta = new ClienteRespostaDto
            {
                Id = cliente.Id,
                NomeUsuario = cliente.NomeUsuario,
                Email = cliente.Email,
                Telefone = cliente.Telefone,
                Tipo = (int)cliente.Tipo,
                UltimoLogin = cliente.UltimoLogin,
                LatitudeAtual = cliente.LatitudeAtual,
                LongitudeAtual = cliente.LongitudeAtual,
                Ativo = cliente.Ativo,
                DataCriacao = cliente.DataCriacao
            };

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/clientes/{id}
        /// Retorna um cliente específico
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var cliente = await _clienteServico.ObterPorIdAsync(id);

            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            var resposta = new ClienteRespostaDto
            {
                Id = cliente.Id,
                NomeUsuario = cliente.NomeUsuario,
                Email = cliente.Email,
                Telefone = cliente.Telefone,
                Tipo = (int)cliente.Tipo,
                UltimoLogin = cliente.UltimoLogin,
                LatitudeAtual = cliente.LatitudeAtual,
                LongitudeAtual = cliente.LongitudeAtual,
                Ativo = cliente.Ativo,
                DataCriacao = cliente.DataCriacao
            };

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/clientes/email/{email}
        /// Busca cliente por email
        /// </summary>
        [HttpGet("email/{email}")]
        public async Task<IActionResult> BuscarPorEmail(string email)
        {
            var cliente = await _clienteServico.ObterPorEmailAsync(email);

            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            var resposta = new ClienteRespostaDto
            {
                Id = cliente.Id,
                NomeUsuario = cliente.NomeUsuario,
                Email = cliente.Email,
                Telefone = cliente.Telefone,
                Tipo = (int)cliente.Tipo,
                UltimoLogin = cliente.UltimoLogin,
                LatitudeAtual = cliente.LatitudeAtual,
                LongitudeAtual = cliente.LongitudeAtual,
                Ativo = cliente.Ativo,
                DataCriacao = cliente.DataCriacao
            };

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/clientes
        /// Lista todos os clientes ativos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var clientes = await _clienteServico.ListarAtivosAsync();

            var resposta = clientes.Select(c => new ClienteRespostaDto
            {
                Id = c.Id,
                NomeUsuario = c.NomeUsuario,
                Email = c.Email,
                Telefone = c.Telefone,
                Tipo = (int)c.Tipo,
                UltimoLogin = c.UltimoLogin,
                LatitudeAtual = c.LatitudeAtual,
                LongitudeAtual = c.LongitudeAtual,
                Ativo = c.Ativo,
                DataCriacao = c.DataCriacao
            });

            return Ok(resposta);
        }

        /// <summary>
        /// PUT: /api/clientes/{id}
        /// Atualiza dados do cliente
        /// Body: ClienteCriarDto
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] ClienteCriarDto dto)
        {
            var cliente = await _clienteServico.ObterPorIdAsync(id);
            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            cliente.NomeUsuario = dto.NomeUsuario;
            cliente.Email = dto.Email;
            cliente.Telefone = dto.Telefone;

            await _clienteServico.AtualizarAsync(cliente);

            var resposta = new ClienteRespostaDto
            {
                Id = cliente.Id,
                NomeUsuario = cliente.NomeUsuario,
                Email = cliente.Email,
                Telefone = cliente.Telefone,
                Tipo = (int)cliente.Tipo,
                UltimoLogin = cliente.UltimoLogin,
                LatitudeAtual = cliente.LatitudeAtual,
                LongitudeAtual = cliente.LongitudeAtual,
                Ativo = cliente.Ativo,
                DataCriacao = cliente.DataCriacao
            };

            return Ok(resposta);
        }

        /// <summary>
        /// PUT: /api/clientes/{id}/localizacao
        /// Atualiza a localização do cliente (geolocalização)
        /// Body: { "latitude": -23.5505, "longitude": -46.6333 }
        /// </summary>
        [HttpPut("{id:guid}/localizacao")]
        public async Task<IActionResult> AtualizarLocalizacao(Guid id, [FromBody] LocalizacaoDto dto)
        {
            await _clienteServico.AtualizarLocalizacaoAsync(id, dto.Latitude, dto.Longitude);

            return Ok(new { mensagem = "Localização atualizada com sucesso" });
        }

        /// <summary>
        /// GET: /api/clientes/proximidade/buscar?latitude=-23.5505&longitude=-46.6333&raio=5
        /// Busca clientes próximos em um raio especificado (em KM)
        /// </summary>
        [HttpGet("proximidade/buscar")]
        public async Task<IActionResult> BuscarPorProximidade(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude,
            [FromQuery] decimal raio = 5)
        {
            var clientes = await _clienteServico.ObterPorProximidadeAsync(latitude, longitude, raio);

            var resposta = clientes.Select(c => new ClienteRespostaDto
            {
                Id = c.Id,
                NomeUsuario = c.NomeUsuario,
                Email = c.Email,
                Telefone = c.Telefone,
                Tipo = (int)c.Tipo,
                UltimoLogin = c.UltimoLogin,
                LatitudeAtual = c.LatitudeAtual,
                LongitudeAtual = c.LongitudeAtual,
                Ativo = c.Ativo,
                DataCriacao = c.DataCriacao
            });

            return Ok(resposta);
        }

        /// <summary>
        /// PUT: /api/clientes/{id}/senha
        /// Altera a senha do cliente
        /// Body: { "senhaAtual": "123456", "novaSenha": "654321" }
        /// </summary>
        [HttpPut("{id:guid}/senha")]
        public async Task<IActionResult> AlterarSenha(Guid id, [FromBody] AlterarSenhaDto dto)
        {
            await _clienteServico.AlterarSenhaAsync(id, dto.SenhaAtual, dto.NovaSenha);

            return Ok(new { mensagem = "Senha alterada com sucesso" });
        }

        /// <summary>
        /// DELETE: /api/clientes/{id}
        /// Remove/desativa um cliente
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _clienteServico.RemoverAsync(id);
            return NoContent();
        }
    }
}
