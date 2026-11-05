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
    
    public ICollection<ProveedorProducto> Proveedores { get; set; } = new List<ProveedorProducto>();
    public ICollection<FichaCostoProducto> FichasCosto { get; set; } = new List<FichaCostoProducto>();
    public ICollection<Promocion> Promociones { get; set; } = new List<Promocion>();
}

public class FichaCostoProducto : BaseEntity
{
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public string Variante { get; set; } = "General";
    public string Contexto { get; set; } = string.Empty;
    public decimal CostoTotalUSD { get; set; }
    public decimal MultiplicadorPrecioMinimo { get; set; } = 2m;
    public decimal MultiplicadorPrecioIdeal { get; set; } = 3m;
    public decimal PrecioVentaUSD { get; set; }
    public ICollection<PartidaCostoFicha> Partidas { get; set; } = new List<PartidaCostoFicha>();
}

public class PartidaCostoFicha : BaseEntity
{
    public Guid FichaCostoProductoId { get; set; }
    public FichaCostoProducto FichaCostoProducto { get; set; } = null!;
    public string Concepto { get; set; } = string.Empty;
    public string TipoCalculo { get; set; } = "Fijo";
    public decimal Cantidad { get; set; } = 1m;
    public decimal CostoUnitarioUSD { get; set; }
    public decimal Porcentaje { get; set; }
    public bool IncluirEnBasePorcentual { get; set; } = true;
    public decimal SubtotalUSD { get; set; }
    public int Orden { get; set; }
}

public class Promocion : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public Guid? ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public Guid? PaqueteId { get; set; }
    public Paquete? Paquete { get; set; }
    public string TipoDescuento { get; set; } = string.Empty;
    public decimal ValorDescuento { get; set; }
    public string DescripcionTrabajoGratis { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public class Proveedor : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public ICollection<Contacto> Contactos { get; set; } = new List<Contacto>();
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
    public ICollection<Promocion> Promociones { get; set; } = new List<Promocion>();
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