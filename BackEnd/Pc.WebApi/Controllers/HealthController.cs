using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pc.Infraestrutura;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HealthController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            try
            {
                await _db.Database.CanConnectAsync(cancellationToken);
                var clientes = await _db.Clientes.CountAsync(cancellationToken);
                return Ok(new { status = "ok", database = "connected", clientes });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "error", database = ex.Message });
            }
        }
    }
}
