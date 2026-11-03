using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers.Catalogos;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class CategoriasController : ControllerBase
{
    private readonly StudioDbContext _context;
    public CategoriasController(StudioDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> ObtenerTodas()
    {
        var categorias = await _context.CategoriasProductos
            .Include(c => c.Productos)
            .Select(c => new
            {
                c.Id,
                c.Nombre,
                Cantidad = c.Productos.Count,
                Productos = c.Productos.Select(p => new { p.Id, p.Nombre, p.PrecioVentaBaseUSD }).ToList()
            })
            .ToListAsync();
        return Ok(categorias);
    }

    public record CategoriaRequest(string Nombre, string Descripcion);

    [HttpPost]
    public async Task<IActionResult> Crear(CategoriaRequest request)
    {
        var cat = new CategoriaProducto { Nombre = request.Nombre, Descripcion = request.Descripcion };
        _context.CategoriasProductos.Add(cat);
        await _context.SaveChangesAsync();
        return Ok(cat);
    }
}