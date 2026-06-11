using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Validacoes;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultasController : ControllerBase
    {
        private readonly IConsultaCnpjServico _consultaCnpj;

        public ConsultasController(IConsultaCnpjServico consultaCnpj)
        {
            _consultaCnpj = consultaCnpj;
        }

        [HttpGet("cnpj/{cnpj}")]
        [AllowAnonymous]
        public async Task<IActionResult> ConsultarCnpj(string cnpj, CancellationToken cancellationToken)
        {
            if (!CnpjValidator.IsValido(cnpj))
                return BadRequest(new { message = "CNPJ inválido." });

            try
            {
                var resultado = await _consultaCnpj.ConsultarAsync(cnpj, cancellationToken);
                if (resultado == null)
                    return NotFound(new { message = "CNPJ não encontrado na base da Receita. Verifique se o número está correto e se a empresa está cadastrada e ativa." });

                if (!resultado.Ativo)
                    return BadRequest(new { message = $"CNPJ com situação cadastral: {resultado.SituacaoCadastral}." });

                return Ok(new
                {
                    cnpj = resultado.Cnpj,
                    razaoSocial = resultado.RazaoSocial,
                    nomeFantasia = resultado.NomeFantasia,
                    situacaoCadastral = resultado.SituacaoCadastral,
                    ativo = resultado.Ativo
                });
            }
            catch (CnpjConsultaIndisponivelException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
            }
        }
    }
}
