using System.ComponentModel.DataAnnotations.Schema;

namespace StudioERP.Domain.Entities;

public class TipoServicio : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
}

public class Servicio : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioBaseUSD { get; set; }
    public Guid TipoServicioId { get; set; }
    public TipoServicio TipoServicio { get; set; } = null!;
}

public class CategoriaProducto : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

public class Producto : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioVentaBaseUSD { get; set; }
    public Guid? CategoriaProductoId { get; set; }
    public CategoriaProducto? CategoriaProducto { get; set; }
    
    // FICHA DE COSTO DINÁMICA (JSON)
    [Column(TypeName = "jsonb")]
    public string DetalleCosto { get; set; } = "{}"; 
    
    public ICollection<ProveedorProducto> Proveedores { get; set; } = new List<ProveedorProducto>();
}

public class Proveedor : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public ICollection<ProveedorProducto> ProductosOfertados { get; set; } = new List<ProveedorProducto>();
}

public class ProveedorProducto : BaseEntity
{
    public Guid ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public decimal CostoUnitarioUSD { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public bool EsPrecioDeOferta { get; set; }
    public DateTime? OfertaValidaHasta { get; set; }
    public decimal CostoFinalUSD => CostoUnitarioUSD - (CostoUnitarioUSD * (DescuentoPorcentaje / 100));
}

public class Paquete : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal PrecioVentaFinalUSD { get; set; }
    public ICollection<PaqueteItem> Items { get; set; } = new List<PaqueteItem>();
}

public class PaqueteItem : BaseEntity
{
    public Guid PaqueteId { get; set; }
    public Paquete Paquete { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public int Cantidad { get; set; } = 1;
}

public class HistorialCostoProveedor : BaseEntity
{
    public Guid ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public decimal CostoAnteriorUSD { get; set; }
    public decimal CostoNuevoUSD { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public class HistorialPrecioProducto : BaseEntity
{
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public decimal PrecioAnteriorUSD { get; set; }
    public decimal PrecioNuevoUSD { get; set; }
    public string Motivo { get; set; } = string.Empty;
}