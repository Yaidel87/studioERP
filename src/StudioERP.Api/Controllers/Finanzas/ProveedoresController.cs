using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers.Finanzas;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/[controller]")]
public class ProveedoresController : ControllerBase
{
    private readonly StudioDbContext _context;

    public ProveedoresController(StudioDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var proveedores = await _context.Proveedores
            .Where(proveedor => !proveedor.IsDeleted)
            .OrderBy(proveedor => proveedor.Nombre)
            .Include(proveedor => proveedor.Contactos.Where(contacto => !contacto.IsDeleted))
            .Select(proveedor => new
            {
                proveedor.Id,
                proveedor.Nombre,
                Telefono = proveedor.Contactos.Where(contacto => !contacto.IsDeleted && contacto.Tipo == "Telefono")
                               .Select(contacto => contacto.Valor).FirstOrDefault() ?? proveedor.Telefono,
                Contactos = proveedor.Contactos.Where(contacto => !contacto.IsDeleted)
                    .Select(contacto => new { contacto.Id, contacto.Tipo, contacto.Valor }).ToList()
            })
            .ToListAsync();

        return Ok(proveedores);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var proveedor = await _context.Proveedores
            .Include(item => item.Contactos.Where(contacto => !contacto.IsDeleted))
            .Where(item => item.Id == id && !item.IsDeleted)
            .Select(item => new
            {
                item.Id,
                item.Nombre,
                Telefono = item.Contactos.Where(contacto => !contacto.IsDeleted && contacto.Tipo == "Telefono")
                               .Select(contacto => contacto.Valor).FirstOrDefault() ?? item.Telefono,
                Contactos = item.Contactos.Where(contacto => !contacto.IsDeleted)
                    .Select(contacto => new { contacto.Id, contacto.Tipo, contacto.Valor }).ToList()
            })
            .FirstOrDefaultAsync();

        return proveedor == null ? NotFound("Proveedor no encontrado.") : Ok(proveedor);
    }

    public record ContactoRequest(string Tipo, string Valor);
    public record ProveedorRequest(string Nombre, string Telefono, List<ContactoRequest>? Contactos = null);

    [HttpPost]
    public async Task<IActionResult> Crear(ProveedorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return BadRequest("El nombre del proveedor es obligatorio.");
        var errorContacto = ValidarContactos(request.Contactos);
        if (errorContacto != null) return BadRequest(errorContacto);

        var proveedor = new Proveedor
        {
            Nombre = request.Nombre.Trim(),
            Telefono = request.Telefono?.Trim() ?? string.Empty
        };
        AgregarContactos(proveedor, request);

        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerPorId), new { id = proveedor.Id }, new
        {
            proveedor.Id,
            proveedor.Nombre,
            proveedor.Telefono
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, ProveedorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return BadRequest("El nombre del proveedor es obligatorio.");
        var errorContacto = ValidarContactos(request.Contactos);
        if (errorContacto != null) return BadRequest(errorContacto);

        var proveedor = await _context.Proveedores
            .Include(item => item.Contactos)
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (proveedor == null)
            return NotFound("Proveedor no encontrado.");

        proveedor.Nombre = request.Nombre.Trim();
        proveedor.Telefono = request.Telefono?.Trim() ?? string.Empty;
        foreach (var contacto in proveedor.Contactos.Where(contacto => !contacto.IsDeleted))
            contacto.IsDeleted = true;
        AgregarContactos(proveedor, request);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var proveedor = await _context.Proveedores
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (proveedor == null)
            return NotFound("Proveedor no encontrado.");

        _context.Proveedores.Remove(proveedor);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static string? ValidarContactos(List<ContactoRequest>? contactos)
    {
        if (contactos == null) return null;
        return contactos.Any(contacto =>
            contacto.Tipo is not ("Telefono" or "Email" or "Direccion") ||
            string.IsNullOrWhiteSpace(contacto.Valor))
            ? "Cada contacto debe tener un tipo válido y un valor."
            : null;
    }

    private static void AgregarContactos(Proveedor proveedor, ProveedorRequest request)
    {
        var contactos = request.Contactos ?? (string.IsNullOrWhiteSpace(request.Telefono)
            ? new List<ContactoRequest>()
            : new List<ContactoRequest> { new("Telefono", request.Telefono) });
        foreach (var contacto in contactos)
            proveedor.Contactos.Add(new Contacto { Tipo = contacto.Tipo, Valor = contacto.Valor.Trim() });
        if (request.Contactos != null)
            proveedor.Telefono = contactos.FirstOrDefault(contacto => contacto.Tipo == "Telefono")?.Valor ?? string.Empty;
    }
}
