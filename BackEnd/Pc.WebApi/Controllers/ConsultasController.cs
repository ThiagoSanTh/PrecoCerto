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
                return Problema(
                    StatusCodes.Status400BadRequest,
                    "cnpj_invalido",
                    "CNPJ inválido",
                    "O CNPJ não tem formato ou dígito verificador válido.");

            try
            {
                var resultado = await _consultaCnpj.ConsultarAsync(cnpj, cancellationToken);
                if (resultado == null)
                    return Problema(
                        StatusCodes.Status404NotFound,
                        "cnpj_nao_encontrado",
                        "CNPJ não encontrado",
                        "CNPJ não encontrado na base da Receita. Verifique se o número está correto e se a empresa está cadastrada.");

                if (!resultado.Ativo)
                    return Problema(
                        StatusCodes.Status422UnprocessableEntity,
                        "cnpj_inativo",
                        "CNPJ inativo",
                        $"CNPJ com situação cadastral: {resultado.SituacaoCadastral}.");

                return Ok(new
                {
                    cnpj = resultado.Cnpj,
                    razaoSocial = resultado.RazaoSocial,
                    nomeFantasia = resultado.NomeFantasia,
                    situacaoCadastral = resultado.SituacaoCadastral,
                    situacao = string.IsNullOrWhiteSpace(resultado.SituacaoCadastral)
                        ? null
                        : resultado.SituacaoCadastral,
                    ativo = resultado.Ativo,
                    endereco = resultado.Endereco,
                    email = resultado.Email,
                    telefone = resultado.Telefone
                });
            }
            catch (CnpjConsultaIndisponivelException ex)
            {
                return Problema(
                    StatusCodes.Status503ServiceUnavailable,
                    "cnpj_fonte_indisponivel",
                    "Consulta de CNPJ indisponível",
                    ex.Message);
            }
        }

        private ObjectResult Problema(int status, string code, string title, string detail)
        {
            Response.Headers["X-Error-Code"] = code;
            return new ObjectResult(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Extensions = { ["code"] = code }
            })
            {
                StatusCode = status
            };
        }
    }
}
