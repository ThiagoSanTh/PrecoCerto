using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Validacoes;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Estabelecimentos;
using Pc.WebApi.Extensions;
using Pc.WebApi.Helpers;
using Pc.WebApi.Mappings;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LojasController : ControllerBase
    {
        private readonly ILojaServico _lojaServico;
        private readonly IClienteServico _usuarioServico;
        private readonly IConsultaCnpjServico _consultaCnpj;
        private readonly IIdCodificador _idCodificador;

        public LojasController(
            ILojaServico lojaServico,
            IClienteServico usuarioServico,
            IConsultaCnpjServico consultaCnpj,
            IIdCodificador idCodificador)
        {
            _lojaServico = lojaServico;
            _usuarioServico = usuarioServico;
            _consultaCnpj = consultaCnpj;
            _idCodificador = idCodificador;
        }

        [HttpGet]
        [AllowAnonymous]
        [EnableRateLimiting("catalogo")]
        public async Task<IActionResult> Listar([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var paginacao = PaginacaoHelper.Normalizar(page, pageSize);
            var lojas = await _lojaServico.ListarPaginadoAsync(paginacao);
            return Ok(PaginacaoHelper.ParaResposta(lojas, l => LojaMapper.ParaRespostaDto(l, _idCodificador)));
        }

        [HttpGet("mapa")]
        [AllowAnonymous]
        [EnableRateLimiting("catalogo")]
        public async Task<IActionResult> ListarParaMapa(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude,
            [FromQuery] decimal raioKm = 15)
        {
            if (raioKm <= 0 || raioKm > 100)
                return BadRequest("raioKm deve estar entre 0 e 100.");

            var lojas = await _lojaServico.ListarPorProximidadeAsync(latitude, longitude, raioKm);
            return Ok(lojas.Select(l => LojaMapper.ParaMapaDto(l, _idCodificador)));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [EnableRateLimiting("catalogo")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var loja = await _lojaServico.ObterPorIdAsync(id);
            if (loja is null)
                return NotFound("Loja não encontrada.");

            return Ok(LojaMapper.ParaRespostaDto(loja, _idCodificador));
        }

        [HttpPost("buscar")]
        [AllowAnonymous]
        [EnableRateLimiting("catalogo")]
        public async Task<IActionResult> BuscarPorNome(
            [FromBody] string nome,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var paginacao = PaginacaoHelper.Normalizar(page, pageSize);
            var lojas = await _lojaServico.BuscarPorNomePaginadoAsync(nome, paginacao);
            return Ok(PaginacaoHelper.ParaResposta(lojas, l => LojaMapper.ParaRespostaDto(l, _idCodificador)));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Adicionar([FromBody] LojaCriarDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // Abrir loja exige CNPJ válido — é isso que promove o usuário a Lojista.
            if (!CnpjValidator.IsValido(dto.Cnpj))
                return BadRequest("CNPJ inválido. É necessário um CNPJ válido para abrir uma loja.");

            var (consulta, erroCnpj) = await ValidarCnpjNaReceitaAsync(dto.Cnpj);
            if (erroCnpj is not null)
                return erroCnpj;

            // O dono da loja é o usuário autenticado (admin pode informar outro).
            var usuarioId = User.GetUserId();
            if (User.IsAdmin() && dto.UsuarioId.HasValue && dto.UsuarioId.Value != Guid.Empty)
                usuarioId = dto.UsuarioId;

            if (usuarioId is null || usuarioId == Guid.Empty)
                return BadRequest("Usuário inválido.");

            var lojaDoUsuario = await _lojaServico.ObterPorUsuarioIdAsync(usuarioId.Value);
            if (lojaDoUsuario is not null)
            {
                await _usuarioServico.DefinirComoLojistaAsync(usuarioId.Value);
                return Ok(LojaMapper.ParaRespostaDto(lojaDoUsuario, _idCodificador));
            }

            var endereco = CriarEndereco(dto.Endereco);
            var loja = new Loja
            {
                NomeFantasia = string.IsNullOrWhiteSpace(dto.NomeFantasia)
                    ? consulta!.NomeFantasia ?? consulta.RazaoSocial
                    : dto.NomeFantasia,
                RazaoSocial = string.IsNullOrWhiteSpace(dto.RazaoSocial)
                    ? consulta!.RazaoSocial
                    : dto.RazaoSocial,
                Cnpj = CnpjValidator.ApenasDigitos(dto.Cnpj),
                Telefone = dto.Telefone,
                Email = dto.Email,
                Descricao = dto.Descricao,
                UsuarioId = usuarioId,
                Endereco = endereco,
                EnderecoId = endereco.Id
            };

            Loja novaLoja;
            try
            {
                novaLoja = await _lojaServico.AdicionarAsync(loja);
                await _usuarioServico.DefinirComoLojistaAsync(usuarioId.Value);
            }
            catch (DbUpdateException ex)
            {
                var detalhe = ex.InnerException?.Message ?? ex.Message;
                if (detalhe.Contains("IX_Lojas_UsuarioId", StringComparison.OrdinalIgnoreCase)
                    || detalhe.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new
                    {
                        message = "Você já possui uma loja cadastrada. Saia e entre novamente para acessar o painel da loja."
                    });
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Não foi possível salvar a loja. Verifique os dados e tente novamente.",
                    detail = detalhe
                });
            }

            return CreatedAtAction(nameof(ObterPorId), new { id = novaLoja.Id }, LojaMapper.ParaRespostaDto(novaLoja, _idCodificador));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Lojista,Admin")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] LojaCriarDto dto)
        {
            var lojaExistente = await _lojaServico.ObterPorIdAsync(id);
            if (lojaExistente is null)
                return NotFound("Loja não encontrada.");

            if (!Authz.OwnsLoja(this, id) && !User.IsAdmin())
            {
                if (lojaExistente.UsuarioId != User.GetUserId())
                    return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(dto.Cnpj) && !CnpjValidator.IsValido(dto.Cnpj))
                return BadRequest("CNPJ inválido.");

            lojaExistente.NomeFantasia = dto.NomeFantasia;
            lojaExistente.RazaoSocial = dto.RazaoSocial;
            lojaExistente.Cnpj = string.IsNullOrWhiteSpace(dto.Cnpj) ? dto.Cnpj : CnpjValidator.ApenasDigitos(dto.Cnpj);
            lojaExistente.Telefone = dto.Telefone;
            lojaExistente.Email = dto.Email;
            lojaExistente.Descricao = dto.Descricao;

            if (dto.EnderecoId.HasValue)
                lojaExistente.EnderecoId = dto.EnderecoId.Value;

            if (dto.UsuarioId.HasValue)
                lojaExistente.UsuarioId = dto.UsuarioId.Value;

            if (dto.Endereco is not null)
                lojaExistente.Endereco = CriarEndereco(dto.Endereco);

            await _lojaServico.AtualizarAsync(lojaExistente);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remover(Guid id)
        {
            var lojaExistente = await _lojaServico.ObterPorIdAsync(id);
            if (lojaExistente is null)
                return NotFound("Loja não encontrada.");

            await _lojaServico.RemoverAsync(id);
            return NoContent();
        }

        private async Task<(CnpjConsultaResultado? Resultado, IActionResult? Erro)> ValidarCnpjNaReceitaAsync(string? cnpj)
        {
            try
            {
                var consulta = await _consultaCnpj.ConsultarAsync(cnpj!);
                if (consulta == null)
                    return (null, BadRequest(new { message = "CNPJ não encontrado na Receita Federal. Use um CNPJ cadastrado e ativo." }));
                if (!consulta.Ativo)
                    return (null, BadRequest(new { message = $"CNPJ com situação cadastral: {consulta.SituacaoCadastral}." }));
                return (consulta, null);
            }
            catch (CnpjConsultaIndisponivelException ex)
            {
                return (null, StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message }));
            }
        }

        private static Endereco CriarEndereco(EnderecoDto? endereco)
        {
            if (endereco is null)
                return new Endereco();

            var bairro = string.IsNullOrWhiteSpace(endereco.Bairro)
                ? "Centro"
                : endereco.Bairro.Trim();

            var estado = endereco.Estado.Trim().ToUpperInvariant();
            if (estado.Length > 2)
                estado = estado[..2];

            return new Endereco
            {
                Cep = endereco.Cep,
                Logradouro = endereco.Logradouro,
                Numero = endereco.Numero,
                Bairro = bairro,
                Cidade = endereco.Cidade,
                Estado = estado,
                Latitude = endereco.Latitude,
                Longitude = endereco.Longitude
            };
        }
    }
}
