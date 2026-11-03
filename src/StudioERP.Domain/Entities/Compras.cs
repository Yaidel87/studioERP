namespace StudioERP.Domain.Entities;

public class FacturaProveedor : BaseEntity
{
    public Guid ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaFactura { get; set; }
    public decimal TotalFacturaUSD { get; set; }
    public string MetodoPago { get; set; } = string.Empty;

    public ICollection<FacturaProveedorItem> Items { get; set; } = new List<FacturaProveedorItem>();
}

public class FacturaProveedorItem : BaseEntity
{
    public Guid FacturaProveedorId { get; set; }
    public FacturaProveedor FacturaProveedor { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; }
    public decimal CostoUnitarioFacturaUSD { get; set; }
    public decimal SubtotalUSD => Cantidad * CostoUnitarioFacturaUSD;
}