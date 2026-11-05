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
    [Authorize(Roles = "Admin,Fotógrafo,Cajero")]
    public async Task<IActionResult> ObtenerCategorias()
    {
        var categorias = await _context.CategoriasProductos
            .Where(c => !c.IsDeleted)
            .Include(c => c.Productos.Where(p => !p.IsDeleted))
            .Select(c => new
            {
                c.Id,
                c.Nombre,
                Cantidad = c.Productos.Count,
                Productos = c.Productos.Select(p => new { 
                    p.Id, 
                    p.Nombre, 
                    p.PrecioVentaBaseUSD
                }).ToList()
            })
            .ToListAsync();
        return Ok(categorias);
    }

    public record CategoriaRequest(string Nombre, string Descripcion);

    [Authorize(Roles = "Admin")]
    [HttpPost("categorias")]
    public async Task<IActionResult> CrearCategoria(CategoriaRequest request)
    {
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest("El nombre de la categoría es obligatorio.");
        if (await _context.CategoriasProductos.AnyAsync(c => c.Nombre == nombre && !c.IsDeleted))
            return BadRequest("Ya existe una categoría con ese nombre.");

        var cat = new CategoriaProducto { Nombre = nombre, Descripcion = request.Descripcion?.Trim() ?? string.Empty };
        _context.CategoriasProductos.Add(cat);
        await _context.SaveChangesAsync();
        return Ok(cat);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("categorias/{id:guid}")]
    public async Task<IActionResult> ActualizarCategoria(Guid id, CategoriaRequest request)
    {
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest("El nombre de la categoría es obligatorio.");

        var categoria = await _context.CategoriasProductos
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (categoria == null) return NotFound("Categoría no encontrada.");
        if (await _context.CategoriasProductos.AnyAsync(c => c.Id != id && c.Nombre == nombre && !c.IsDeleted))
            return BadRequest("Ya existe una categoría con ese nombre.");

        categoria.Nombre = nombre;
        categoria.Descripcion = request.Descripcion?.Trim() ?? string.Empty;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Categoría actualizada correctamente." });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("categorias/{id:guid}")]
    public async Task<IActionResult> EliminarCategoria(Guid id)
    {
        var categoria = await _context.CategoriasProductos
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (categoria == null) return NotFound("Categoría no encontrada.");
        if (await _context.Productos.AnyAsync(p => p.CategoriaProductoId == id && !p.IsDeleted))
            return BadRequest("No se puede desactivar una categoría que contiene productos activos.");

        _context.CategoriasProductos.Remove(categoria);
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Categoría desactivada correctamente." });
    }
    // ==========================================
    // PRODUCTOS
    // ==========================================

    // 1. LISTAR TODOS LOS PRODUCTOS
    [Authorize(Roles = "Admin,Fotógrafo,Cajero")]
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
                FichasCosto = p.FichasCosto
                    .Where(ficha => !ficha.IsDeleted)
                    .Select(ficha => new
                    {
                        ficha.Id,
                        ficha.Variante,
                        ficha.Contexto,
                        ficha.PrecioVentaUSD
                    })
                    .ToList()
            })
            .ToListAsync();
        return Ok(productos);
    }

    // 2. CREAR UN PRODUCTO
    public record ProductoRequest(string Nombre, decimal PrecioVentaBaseUSD, Guid? CategoriaProductoId);

    [Authorize(Roles = "Admin")]
    [HttpPost("productos")]
    public async Task<IActionResult> CrearProducto(ProductoRequest request)
    {
        var validation = await ValidarProducto(request.Nombre, request.PrecioVentaBaseUSD, request.CategoriaProductoId);
        if (validation != null) return BadRequest(validation);

        var producto = new Producto
        {
            Nombre = request.Nombre.Trim(),
            PrecioVentaBaseUSD = request.PrecioVentaBaseUSD,
            CategoriaProductoId = request.CategoriaProductoId
        };
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();
        return Ok(producto);
    }

    // 3. OBTENER UN PRODUCTO POR ID (PARA EL MODAL DE EDICIÓN)
    [Authorize(Roles = "Admin,Fotógrafo,Cajero")]
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
            producto.CategoriaProductoId
        });
    }

    // 4. ACTUALIZAR UN PRODUCTO
    public record ActualizarProductoRequest(string Nombre, decimal PrecioVentaBaseUSD, Guid? CategoriaProductoId);

    [Authorize(Roles = "Admin")]
    [HttpPut("productos/{id}")]
    public async Task<IActionResult> ActualizarProducto(Guid id, ActualizarProductoRequest request)
    {
        var validation = await ValidarProducto(request.Nombre, request.PrecioVentaBaseUSD, request.CategoriaProductoId);
        if (validation != null) return BadRequest(validation);

        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (producto == null) return NotFound("Producto no encontrado.");

        producto.Nombre = request.Nombre.Trim();
        producto.PrecioVentaBaseUSD = request.PrecioVentaBaseUSD;
        producto.CategoriaProductoId = request.CategoriaProductoId;

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Producto actualizado correctamente." });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("productos/{id:guid}")]
    public async Task<IActionResult> EliminarProducto(Guid id)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (producto == null) return NotFound("Producto no encontrado.");

        _context.Productos.Remove(producto);
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Producto desactivado correctamente." });
    }
    // ==========================================
    // PAQUETES
    // ==========================================

    [HttpGet("paquetes")]
    [Authorize(Roles = "Admin,Fotógrafo,Cajero")]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CrearPaquete(PaqueteRequest request)
    {
        var validation = await ValidarPaquete(request);
        if (validation != null) return BadRequest(validation);

        var paquete = new Paquete
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim() ?? string.Empty,
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

    [Authorize(Roles = "Admin")]
    [HttpPut("paquetes/{id:guid}")]
    public async Task<IActionResult> ActualizarPaquete(Guid id, PaqueteRequest request)
    {
        var validation = await ValidarPaquete(request);
        if (validation != null) return BadRequest(validation);

        var paquete = await _context.Paquetes
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (paquete == null) return NotFound("Paquete no encontrado.");

        paquete.Nombre = request.Nombre.Trim();
        paquete.Descripcion = request.Descripcion?.Trim() ?? string.Empty;
        paquete.PrecioVentaFinalUSD = request.PrecioVentaFinalUSD;
        paquete.Items.Clear();
        foreach (var item in request.Items)
            paquete.Items.Add(new PaqueteItem { ProductoId = item.ProductoId, Cantidad = item.Cantidad });

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Paquete actualizado correctamente." });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("paquetes/{id:guid}")]
    public async Task<IActionResult> EliminarPaquete(Guid id)
    {
        var paquete = await _context.Paquetes
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (paquete == null) return NotFound("Paquete no encontrado.");

        _context.Paquetes.Remove(paquete);
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Paquete desactivado correctamente." });
    }

    private async Task<string?> ValidarProducto(string nombre, decimal precio, Guid? categoriaId)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return "El nombre del producto es obligatorio.";
        if (precio < 0)
            return "El precio no puede ser negativo.";
        if (categoriaId.HasValue && !await _context.CategoriasProductos.AnyAsync(c => c.Id == categoriaId && !c.IsDeleted))
            return "La categoría no existe o está inactiva.";

        return null;
    }

    private async Task<string?> ValidarPaquete(PaqueteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return "El nombre del paquete es obligatorio.";
        if (request.PrecioVentaFinalUSD < 0)
            return "El precio del paquete no puede ser negativo.";
        if (request.Items == null || request.Items.Count == 0)
            return "El paquete debe contener al menos un producto.";
        if (request.Items.Any(item => item.Cantidad <= 0))
            return "La cantidad de cada producto debe ser mayor que cero.";
        if (request.Items.Select(item => item.ProductoId).Distinct().Count() != request.Items.Count)
            return "No se puede agregar el mismo producto más de una vez al paquete.";

        var productIds = request.Items.Select(item => item.ProductoId).ToList();
        var activeProducts = await _context.Productos.CountAsync(p => productIds.Contains(p.Id) && !p.IsDeleted);
        if (activeProducts != productIds.Count)
            return "Uno o más productos no existen o están inactivos.";

        return null;
    }

    public record PartidaCostoRequest(
        string Concepto,
        string TipoCalculo,
        decimal Cantidad,
        decimal CostoUnitarioUSD,
        decimal Porcentaje,
        bool IncluirEnBasePorcentual,
        int Orden);

    public record FichaCostoRequest(
        string Variante,
        string Contexto,
        decimal MultiplicadorPrecioMinimo,
        decimal MultiplicadorPrecioIdeal,
        decimal PrecioVentaUSD,
        List<PartidaCostoRequest> Partidas);

    [Authorize(Roles = "Admin,Fotógrafo")]
    [HttpGet("productos/{productoId:guid}/fichas-costo")]
    public async Task<IActionResult> ObtenerFichasCosto(Guid productoId)
    {
        var fichas = await _context.FichasCostoProductos
            .Where(ficha => ficha.ProductoId == productoId && !ficha.IsDeleted)
            .Include(ficha => ficha.Partidas.Where(partida => !partida.IsDeleted))
            .OrderBy(ficha => ficha.Contexto)
            .Select(ficha => new
            {
                ficha.Id,
                ficha.ProductoId,
                ficha.Variante,
                ficha.Contexto,
                ficha.CostoTotalUSD,
                ficha.MultiplicadorPrecioMinimo,
                ficha.MultiplicadorPrecioIdeal,
                ficha.PrecioVentaUSD,
                PrecioMinimoUSD = ficha.CostoTotalUSD * ficha.MultiplicadorPrecioMinimo,
                PrecioIdealUSD = ficha.CostoTotalUSD * ficha.MultiplicadorPrecioIdeal,
                Partidas = ficha.Partidas
                    .Where(partida => !partida.IsDeleted)
                    .OrderBy(partida => partida.Orden)
                    .Select(partida => new
                    {
                        partida.Id,
                        partida.Concepto,
                        partida.TipoCalculo,
                        partida.Cantidad,
                        partida.CostoUnitarioUSD,
                        partida.Porcentaje,
                        partida.IncluirEnBasePorcentual,
                        partida.SubtotalUSD,
                        partida.Orden
                    })
                    .ToList()
            })
            .ToListAsync();
        return Ok(fichas);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("productos/{productoId:guid}/fichas-costo")]
    public async Task<IActionResult> CrearFichaCosto(Guid productoId, FichaCostoRequest request)
    {
        var error = await ValidarFichaCosto(productoId, request);
        if (error != null) return BadRequest(error);

        var ficha = CrearEntidadFichaCosto(productoId, request);
        _context.FichasCostoProductos.Add(ficha);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(ObtenerFichasCosto), new { productoId }, new { ficha.Id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("fichas-costo/{id:guid}")]
    public async Task<IActionResult> ActualizarFichaCosto(Guid id, FichaCostoRequest request)
    {
        var ficha = await _context.FichasCostoProductos
            .Include(item => item.Partidas)
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (ficha == null) return NotFound("Ficha de costo no encontrada.");
        var error = await ValidarFichaCosto(ficha.ProductoId, request);
        if (error != null) return BadRequest(error);

        ficha.Variante = request.Variante.Trim();
        ficha.Contexto = request.Contexto.Trim();
        ficha.MultiplicadorPrecioMinimo = request.MultiplicadorPrecioMinimo;
        ficha.MultiplicadorPrecioIdeal = request.MultiplicadorPrecioIdeal;
        ficha.PrecioVentaUSD = request.PrecioVentaUSD;
        foreach (var partida in ficha.Partidas.Where(partida => !partida.IsDeleted))
            _context.PartidasCostoFicha.Remove(partida);
        var actualizada = CrearPartidas(request.Partidas);
        foreach (var partida in actualizada)
            ficha.Partidas.Add(partida);
        ficha.CostoTotalUSD = CalcularCostoTotal(actualizada);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("fichas-costo/{id:guid}")]
    public async Task<IActionResult> EliminarFichaCosto(Guid id)
    {
        var ficha = await _context.FichasCostoProductos
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (ficha == null) return NotFound("Ficha de costo no encontrada.");
        _context.FichasCostoProductos.Remove(ficha);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    public record PromocionRequest(
        string Nombre,
        Guid? ProductoId,
        Guid? PaqueteId,
        string TipoDescuento,
        decimal ValorDescuento,
        string DescripcionTrabajoGratis,
        DateTime FechaInicio,
        DateTime FechaFin);

    [Authorize(Roles = "Admin,Fotógrafo,Cajero")]
    [HttpGet("promociones")]
    public async Task<IActionResult> ObtenerPromociones()
    {
        var promociones = await _context.Promociones
            .Where(p => !p.IsDeleted)
            .Include(p => p.Producto)
            .Include(p => p.Paquete)
            .OrderBy(p => p.FechaInicio)
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                p.ProductoId,
                Producto = p.Producto != null ? p.Producto.Nombre : null,
                p.PaqueteId,
                Paquete = p.Paquete != null ? p.Paquete.Nombre : null,
                p.TipoDescuento,
                p.ValorDescuento,
                p.DescripcionTrabajoGratis,
                p.FechaInicio,
                p.FechaFin,
                Activa = p.FechaInicio <= DateTime.UtcNow && p.FechaFin >= DateTime.UtcNow
            })
            .ToListAsync();
        return Ok(promociones);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("promociones")]
    public async Task<IActionResult> CrearPromocion(PromocionRequest request)
    {
        var error = await ValidarPromocion(request);
        if (error != null) return BadRequest(error);

        var promocion = CrearEntidadPromocion(request);
        _context.Promociones.Add(promocion);
        await _context.SaveChangesAsync();
        return Ok(new { promocion.Id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("promociones/{id:guid}")]
    public async Task<IActionResult> ActualizarPromocion(Guid id, PromocionRequest request)
    {
        var promocion = await _context.Promociones
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (promocion == null) return NotFound("Promoción no encontrada.");
        var error = await ValidarPromocion(request, id);
        if (error != null) return BadRequest(error);

        var actualizada = CrearEntidadPromocion(request);
        promocion.Nombre = actualizada.Nombre;
        promocion.ProductoId = actualizada.ProductoId;
        promocion.PaqueteId = actualizada.PaqueteId;
        promocion.TipoDescuento = actualizada.TipoDescuento;
        promocion.ValorDescuento = actualizada.ValorDescuento;
        promocion.DescripcionTrabajoGratis = actualizada.DescripcionTrabajoGratis;
        promocion.FechaInicio = actualizada.FechaInicio;
        promocion.FechaFin = actualizada.FechaFin;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("promociones/{id:guid}")]
    public async Task<IActionResult> EliminarPromocion(Guid id)
    {
        var promocion = await _context.Promociones
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (promocion == null) return NotFound("Promoción no encontrada.");
        _context.Promociones.Remove(promocion);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string?> ValidarFichaCosto(Guid productoId, FichaCostoRequest request)
    {
        if (!await _context.Productos.AnyAsync(p => p.Id == productoId && !p.IsDeleted))
            return "El producto no existe o está inactivo.";
        if (string.IsNullOrWhiteSpace(request.Variante))
            return "Debe indicar el formato o variante del producto.";
        if (string.IsNullOrWhiteSpace(request.Contexto))
            return "Debe describir el taller, editor u otro contexto del costo.";
        if (request.MultiplicadorPrecioMinimo <= 0 ||
            request.MultiplicadorPrecioIdeal < request.MultiplicadorPrecioMinimo)
            return "Los multiplicadores deben ser positivos y el precio ideal no puede ser menor que el mínimo.";
        if (request.PrecioVentaUSD < 0)
            return "El precio de venta no puede ser negativo.";
        if (request.Partidas == null || request.Partidas.Count == 0)
            return "Agrega al menos una partida de costo.";
        if (request.Partidas.Any(item =>
                string.IsNullOrWhiteSpace(item.Concepto) ||
                item.TipoCalculo is not ("Fijo" or "Porcentaje") ||
                item.Cantidad <= 0 ||
                item.CostoUnitarioUSD < 0 ||
                item.Porcentaje < 0 ||
                item.Porcentaje > 100 ||
                (item.TipoCalculo == "Fijo" && item.Porcentaje != 0) ||
                (item.TipoCalculo == "Porcentaje" && item.CostoUnitarioUSD != 0)))
            return "Revisa las partidas: conceptos, cantidades, importes y porcentajes deben ser válidos.";
        return null;
    }

    private static FichaCostoProducto CrearEntidadFichaCosto(Guid productoId, FichaCostoRequest request)
    {
        var partidas = CrearPartidas(request.Partidas);
        return new FichaCostoProducto
        {
            ProductoId = productoId,
            Variante = request.Variante.Trim(),
            Contexto = request.Contexto.Trim(),
            MultiplicadorPrecioMinimo = request.MultiplicadorPrecioMinimo,
            MultiplicadorPrecioIdeal = request.MultiplicadorPrecioIdeal,
            PrecioVentaUSD = request.PrecioVentaUSD,
            CostoTotalUSD = CalcularCostoTotal(partidas),
            Partidas = partidas
        };
    }

    private static List<PartidaCostoFicha> CrearPartidas(IEnumerable<PartidaCostoRequest> requests)
    {
        var partidas = requests
            .OrderBy(request => request.Orden)
            .Select(request => new PartidaCostoFicha
            {
                Concepto = request.Concepto.Trim(),
                TipoCalculo = request.TipoCalculo,
                Cantidad = request.TipoCalculo == "Porcentaje" ? 1 : request.Cantidad,
                CostoUnitarioUSD = request.TipoCalculo == "Fijo" ? request.CostoUnitarioUSD : 0,
                Porcentaje = request.TipoCalculo == "Porcentaje" ? request.Porcentaje : 0,
                IncluirEnBasePorcentual = request.IncluirEnBasePorcentual,
                Orden = request.Orden
            })
            .ToList();

        var basePorcentual = partidas
            .Where(item => item.TipoCalculo == "Fijo" && item.IncluirEnBasePorcentual)
            .Sum(item => Math.Round(item.Cantidad * item.CostoUnitarioUSD, 2, MidpointRounding.AwayFromZero));

        foreach (var partida in partidas)
        {
            partida.SubtotalUSD = partida.TipoCalculo == "Porcentaje"
                ? Math.Round(basePorcentual * partida.Porcentaje / 100m, 2, MidpointRounding.AwayFromZero)
                : Math.Round(partida.Cantidad * partida.CostoUnitarioUSD, 2, MidpointRounding.AwayFromZero);
        }

        return partidas;
    }

    private static decimal CalcularCostoTotal(IEnumerable<PartidaCostoFicha> partidas) =>
        partidas.Sum(partida => partida.SubtotalUSD);

    private async Task<string?> ValidarPromocion(PromocionRequest request, Guid? idExcluido = null)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return "El nombre de la promoción es obligatorio.";
        if (request.ProductoId.HasValue == request.PaqueteId.HasValue)
            return "La promoción debe aplicarse a un producto o a un paquete, pero no a ambos.";
        if (request.FechaFin < request.FechaInicio)
            return "La fecha final no puede ser anterior a la fecha inicial.";
        if (request.TipoDescuento is not ("FijoUSD" or "Porcentaje" or "TrabajoGratis"))
            return "Tipo de promoción no válido.";
        if (request.TipoDescuento == "Porcentaje" && (request.ValorDescuento <= 0 || request.ValorDescuento > 100))
            return "El descuento porcentual debe estar entre 0 y 100.";
        if (request.TipoDescuento == "FijoUSD" && request.ValorDescuento <= 0)
            return "El descuento fijo debe ser mayor que cero.";
        if (request.TipoDescuento == "TrabajoGratis" && string.IsNullOrWhiteSpace(request.DescripcionTrabajoGratis))
            return "Debe indicar explícitamente el trabajo gratuito incluido.";

        if (request.ProductoId.HasValue &&
            !await _context.Productos.AnyAsync(p => p.Id == request.ProductoId && !p.IsDeleted))
            return "El producto no existe o está inactivo.";
        if (request.PaqueteId.HasValue &&
            !await _context.Paquetes.AnyAsync(p => p.Id == request.PaqueteId && !p.IsDeleted))
            return "El paquete no existe o está inactivo.";

        var inicio = request.FechaInicio.ToUniversalTime();
        var fin = request.FechaFin.ToUniversalTime();
        var promocionesSuperpuestas = await _context.Promociones.AnyAsync(p =>
            !p.IsDeleted &&
            (!idExcluido.HasValue || p.Id != idExcluido.Value) &&
            p.ProductoId == request.ProductoId &&
            p.PaqueteId == request.PaqueteId &&
            p.FechaInicio <= fin &&
            p.FechaFin >= inicio);
        return promocionesSuperpuestas
            ? "Ya existe otra promoción para ese producto o paquete durante esas fechas. No se acumulan promociones."
            : null;
    }

    private static Promocion CrearEntidadPromocion(PromocionRequest request) => new()
    {
        Nombre = request.Nombre.Trim(),
        ProductoId = request.ProductoId,
        PaqueteId = request.PaqueteId,
        TipoDescuento = request.TipoDescuento,
        ValorDescuento = request.TipoDescuento == "TrabajoGratis" ? 0 : request.ValorDescuento,
        DescripcionTrabajoGratis = request.DescripcionTrabajoGratis?.Trim() ?? string.Empty,
        FechaInicio = request.FechaInicio.ToUniversalTime(),
        FechaFin = request.FechaFin.ToUniversalTime()
    };
}