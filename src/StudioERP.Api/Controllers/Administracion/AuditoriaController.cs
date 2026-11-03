using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers.Administracion;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/[controller]")]
public class AuditoriaController : ControllerBase
{
    private readonly StudioDbContext _context;
    public AuditoriaController(StudioDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> ObtenerLogs([FromQuery] int pagina = 1, [FromQuery] int tamano = 50)
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        var total = await _context.AuditLogs.CountAsync();

        return Ok(new { logs, total, pagina, tamano });
    }
}