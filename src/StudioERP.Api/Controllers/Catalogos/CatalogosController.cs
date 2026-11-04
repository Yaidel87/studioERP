using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers.Catalogos;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")] // Ruta base: api/v1/Catalogos
public class CatalogosController : ControllerBase
{
    private readonly StudioDbContext _context;

    public CatalogosController(StudioDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // CATEGORÍAS DE PRODUCTOS
    // ==========================================

    [HttpGet("categorias")]
    public async Task<IActionResult> ObtenerCategorias()
    {
        var categorias = await _context.CategoriasProductos
            .Include(c => c.Productos)
            .Select(c => new
            {
                c.Id,
                c.Nombre,
                Cantidad = c.Productos.Count,
                Productos = c.Productos.Select(p => new { 
                    p.Id, 
                    p.Nombre, 
                    p.PrecioVentaBaseUSD, 
                    p.DetalleCosto 
                }).ToList()
            })
            .ToListAsync();
        return Ok(categorias);
    }

    public record CategoriaRequest(string Nombre, string Descripcion);

    [HttpPost("categorias")]
    public async Task<IActionResult> CrearCategoria(CategoriaRequest request)
    {
        var cat = new CategoriaProducto { Nombre = request.Nombre, Descripcion = request.Descripcion };
        _context.CategoriasProductos.Add(cat);
        await _context.SaveChangesAsync();
        return Ok(cat);
    }

    // ==========================================
    // PRODUCTOS
    // ==========================================

    // 1. LISTAR TODOS LOS PRODUCTOS
    [HttpGet("productos")]
    public async Task<IActionResult> ObtenerProductos()
    {
        var productos = await _context.Productos
            .Include(p => p.CategoriaProducto)
            .Where(p => !p.IsDeleted)
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                p.PrecioVentaBaseUSD,
                CategoriaId = p.CategoriaProductoId,
                CategoriaNombre = p.CategoriaProducto != null ? p.CategoriaProducto.Nombre : "Sin categoría",
                p.DetalleCosto
            })
            .ToListAsync();
        return Ok(productos);
    }

    // 2. CREAR UN PRODUCTO
    public record ProductoRequest(string Nombre, decimal PrecioVentaBaseUSD, Guid? CategoriaProductoId, string DetalleCosto);

    [HttpPost("productos")]
    public async Task<IActionResult> CrearProducto(ProductoRequest request)
    {
        var producto = new Producto
        {
            Nombre = request.Nombre,
            PrecioVentaBaseUSD = request.PrecioVentaBaseUSD,
            CategoriaProductoId = request.CategoriaProductoId,
            DetalleCosto = request.DetalleCosto ?? "{}"
        };
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();
        return Ok(producto);
    }

    // 3. OBTENER UN PRODUCTO POR ID (PARA EL MODAL DE EDICIÓN)
    [HttpGet("productos/{id}")]
    public async Task<IActionResult> ObtenerProductoPorId(Guid id)
    {
        var producto = await _context.Productos
            .Include(p => p.CategoriaProducto)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (producto == null) return NotFound("Producto no encontrado.");

        return Ok(new
        {
            producto.Id,
            producto.Nombre,
            producto.PrecioVentaBaseUSD,
            producto.CategoriaProductoId,
            producto.DetalleCosto
        });
    }

    // 4. ACTUALIZAR UN PRODUCTO
    public record ActualizarProductoRequest(string Nombre, decimal PrecioVentaBaseUSD, Guid? CategoriaProductoId, string DetalleCosto);

    [HttpPut("productos/{id}")]
    public async Task<IActionResult> ActualizarProducto(Guid id, ActualizarProductoRequest request)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null) return NotFound("Producto no encontrado.");

        producto.Nombre = request.Nombre;
        producto.PrecioVentaBaseUSD = request.PrecioVentaBaseUSD;
        producto.CategoriaProductoId = request.CategoriaProductoId;
        producto.DetalleCosto = request.DetalleCosto ?? "{}";

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Producto actualizado correctamente." });
    }

    // ==========================================
    // PAQUETES
    // ==========================================

    [HttpGet("paquetes")]
    public async Task<IActionResult> ObtenerPaquetes()
    {
        var paquetes = await _context.Paquetes
            .Include(p => p.Items).ThenInclude(i => i.Producto)
            .Where(p => !p.IsDeleted)
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                p.PrecioVentaFinalUSD,
                Items = p.Items.Select(i => new { 
                    Producto = i.Producto != null ? i.Producto.Nombre : "Eliminado", 
                    i.Cantidad 
                }).ToList()
            })
            .ToListAsync();
        return Ok(paquetes);
    }

    public record PaqueteItemRequest(Guid ProductoId, int Cantidad);
    public record PaqueteRequest(string Nombre, string Descripcion, decimal PrecioVentaFinalUSD, List<PaqueteItemRequest> Items);

    [HttpPost("paquetes")]
    public async Task<IActionResult> CrearPaquete(PaqueteRequest request)
    {
        var paquete = new Paquete
        {
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            PrecioVentaFinalUSD = request.PrecioVentaFinalUSD
        };

        foreach (var item in request.Items)
        {
            paquete.Items.Add(new PaqueteItem { ProductoId = item.ProductoId, Cantidad = item.Cantidad });
        }

        _context.Paquetes.Add(paquete);
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Paquete creado", id = paquete.Id });
    }
}