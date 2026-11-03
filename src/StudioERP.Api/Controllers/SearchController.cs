using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class SearchController : ControllerBase
{
    private readonly StudioDbContext _context;
    public SearchController(StudioDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(new { clientes = new List<object>(), contratos = new List<object>() });

        var clientes = await _context.Clientes
            .Where(c => !c.IsDeleted && (c.Nombre.Contains(q) || c.Apellidos.Contains(q) || c.Telefono.Contains(q)))
            .Take(5)
            .Select(c => new { c.Id, Nombre = c.Nombre + " " + c.Apellidos, Tipo = "Cliente" })
            .ToListAsync();

        var contratos = await _context.Contratos
            .Where(c => !c.IsDeleted && c.NumeroContrato.Contains(q))
            .Take(5)
            .Select(c => new { c.Id, Nombre = c.NumeroContrato, Tipo = "Contrato" })
            .ToListAsync();

        return Ok(new { clientes, contratos });
    }
}