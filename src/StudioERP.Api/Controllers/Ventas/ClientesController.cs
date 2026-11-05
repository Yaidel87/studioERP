using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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

    [Authorize(Roles = "Admin,Fotógrafo,Cajero")]
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] string? buscar)
    {
        var query = _context.Clientes
            .Where(c => !c.IsDeleted)
            .Include(c => c.Contactos.Where(contacto => !contacto.IsDeleted))
            .AsQueryable();

        if (!string.IsNullOrEmpty(buscar))
        {
            query = query.Where(c => c.Nombre.Contains(buscar) ||
                                     c.Apellidos.Contains(buscar) ||
                                     c.Telefono.Contains(buscar) ||
                                     c.Contactos.Any(contacto => !contacto.IsDeleted && contacto.Valor.Contains(buscar)));
        }

        var clientes = await query.OrderBy(c => c.Nombre).Select(c => new
        {
            c.Id,
            c.Nombre,
            c.Apellidos,
            c.Descripcion,
            Telefono = c.Contactos.Where(contacto => !contacto.IsDeleted && contacto.Tipo == "Telefono")
                          .Select(contacto => contacto.Valor).FirstOrDefault() ?? c.Telefono,
            Email = c.Contactos.Where(contacto => !contacto.IsDeleted && contacto.Tipo == "Email")
                       .Select(contacto => contacto.Valor).FirstOrDefault() ?? c.Email,
            Direccion = c.Contactos.Where(contacto => !contacto.IsDeleted && contacto.Tipo == "Direccion")
                           .Select(contacto => contacto.Valor).FirstOrDefault() ?? c.Direccion,
            Contactos = c.Contactos.Where(contacto => !contacto.IsDeleted)
                .Select(contacto => new { contacto.Id, contacto.Tipo, contacto.Valor })
                .ToList()
        }).ToListAsync();
        return Ok(clientes);
    }

    public record ContactoRequest(string Tipo, string Valor);
    public record ClienteRequest(
        string Nombre,
        string Apellidos,
        string Descripcion,
        string Telefono,
        string Email,
        string Direccion,
        List<ContactoRequest>? Contactos = null);

    [Authorize(Roles = "Admin,Cajero")]
    [HttpPost]
    public async Task<IActionResult> Crear(ClienteRequest request)
    {
        var errores = ValidarContactos(request.Contactos);
        if (errores != null) return BadRequest(errores);
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellidos))
            return BadRequest("El nombre y los apellidos del cliente son obligatorios.");

        var cliente = new Cliente
        {
            Nombre = request.Nombre.Trim(),
            Apellidos = request.Apellidos.Trim(),
            Descripcion = request.Descripcion?.Trim() ?? string.Empty,
            Telefono = request.Telefono?.Trim() ?? string.Empty,
            Email = request.Email?.Trim() ?? string.Empty,
            Direccion = request.Direccion?.Trim() ?? string.Empty
        };
        AgregarContactos(cliente, request);

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Cliente creado", id = cliente.Id });
    }

    [Authorize(Roles = "Admin,Cajero")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(Guid id, ClienteRequest request)
    {
        var errores = ValidarContactos(request.Contactos);
        if (errores != null) return BadRequest(errores);
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellidos))
            return BadRequest("El nombre y los apellidos del cliente son obligatorios.");

        var cliente = await _context.Clientes
            .Include(c => c.Contactos)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (cliente == null) return NotFound("Cliente no encontrado.");

        cliente.Nombre = request.Nombre.Trim();
        cliente.Apellidos = request.Apellidos.Trim();
        cliente.Descripcion = request.Descripcion?.Trim() ?? string.Empty;
        cliente.Telefono = request.Telefono?.Trim() ?? string.Empty;
        cliente.Email = request.Email?.Trim() ?? string.Empty;
        cliente.Direccion = request.Direccion?.Trim() ?? string.Empty;
        ReemplazarContactos(cliente, request);

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Cliente actualizado" });
    }

    [Authorize(Roles = "Admin,Cajero")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (cliente == null) return NotFound("Cliente no encontrado.");

        cliente.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Cliente eliminado" });
    }

    private static string? ValidarContactos(List<ContactoRequest>? contactos)
    {
        if (contactos == null) return null;
        foreach (var contacto in contactos)
        {
            if (contacto.Tipo is not ("Telefono" or "Email" or "Direccion") || string.IsNullOrWhiteSpace(contacto.Valor))
                return "Cada contacto debe tener un tipo válido y un valor.";
            if (contacto.Tipo == "Email" && !new EmailAddressAttribute().IsValid(contacto.Valor))
                return $"El correo «{contacto.Valor}» no es válido.";
        }
        return null;
    }

    private static void AgregarContactos(Cliente cliente, ClienteRequest request)
    {
        var contactos = request.Contactos ?? ContactosLegados(request.Telefono, request.Email, request.Direccion);
        foreach (var contacto in contactos)
            cliente.Contactos.Add(new Contacto { Tipo = contacto.Tipo, Valor = contacto.Valor.Trim() });
        if (request.Contactos != null)
        {
            cliente.Telefono = contactos.FirstOrDefault(contacto => contacto.Tipo == "Telefono")?.Valor ?? string.Empty;
            cliente.Email = contactos.FirstOrDefault(contacto => contacto.Tipo == "Email")?.Valor ?? string.Empty;
            cliente.Direccion = contactos.FirstOrDefault(contacto => contacto.Tipo == "Direccion")?.Valor ?? string.Empty;
        }
    }

    private static void ReemplazarContactos(Cliente cliente, ClienteRequest request)
    {
        foreach (var contacto in cliente.Contactos.Where(contacto => !contacto.IsDeleted))
            contacto.IsDeleted = true;
        AgregarContactos(cliente, request);
    }

    private static List<ContactoRequest> ContactosLegados(string? telefono, string? email, string? direccion) =>
        new[]
        {
            new ContactoRequest("Telefono", telefono ?? string.Empty),
            new ContactoRequest("Email", email ?? string.Empty),
            new ContactoRequest("Direccion", direccion ?? string.Empty)
        }
        .Where(contacto => !string.IsNullOrWhiteSpace(contacto.Valor))
        .ToList();
}