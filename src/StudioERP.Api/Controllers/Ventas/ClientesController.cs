using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers.Ventas;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly StudioDbContext _context;

    public ClientesController(StudioDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] string? buscar)
    {
        var query = _context.Clientes.Where(c => !c.IsDeleted).AsQueryable();

        if (!string.IsNullOrEmpty(buscar))
        {
            query = query.Where(c => c.Nombre.Contains(buscar) || c.Apellidos.Contains(buscar) || c.Telefono.Contains(buscar));
        }

        var clientes = await query.OrderBy(c => c.Nombre).ToListAsync();
        return Ok(clientes);
    }

    public record ClienteRequest(string Nombre, string Apellidos, string Descripcion, string Telefono, string Email, string Direccion);

    [HttpPost]
    public async Task<IActionResult> Crear(ClienteRequest request)
    {
        var cliente = new Cliente
        {
            Nombre = request.Nombre,
            Apellidos = request.Apellidos,
            Descripcion = request.Descripcion,
            Telefono = request.Telefono,
            Email = request.Email,
            Direccion = request.Direccion
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Cliente creado", id = cliente.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(Guid id, ClienteRequest request)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound("Cliente no encontrado.");

        cliente.Nombre = request.Nombre;
        cliente.Apellidos = request.Apellidos;
        cliente.Descripcion = request.Descripcion;
        cliente.Telefono = request.Telefono;
        cliente.Email = request.Email;
        cliente.Direccion = request.Direccion;

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Cliente actualizado" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound("Cliente no encontrado.");

        cliente.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Cliente eliminado" });
    }
}