using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pc.Infraestrutura;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/health")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HealthController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>Liveness para Railway/Render. Sem banco — senão o deploy cai se o DNS do Postgres falhar.</summary>
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "ok" });

        [HttpGet("ready")]
        public async Task<IActionResult> Ready(CancellationToken cancellationToken)
        {
            try
            {
                var ok = await _db.Database.CanConnectAsync(cancellationToken);
                if (!ok)
                    return StatusCode(503, new { status = "error", database = "unreachable" });
                return Ok(new { status = "ok", database = "connected" });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "error", database = ex.Message });
            }
        }
    }
}
