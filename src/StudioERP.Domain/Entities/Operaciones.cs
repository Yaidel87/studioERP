namespace StudioERP.Domain.Entities;

// --- CLIENTES ---
public class Cliente : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
}

// --- PERSONAL DEL ESTUDIO (STAFF) ---
public class TipoStaff : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public ICollection<Staff> Staffs { get; set; } = new List<Staff>();
}

public class Staff : BaseEntity
{
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public ICollection<TipoStaff> TiposStaff { get; set; } = new List<TipoStaff>();
    public ICollection<DisponibilidadStaff> BloqueosAgenda { get; set; } = new List<DisponibilidadStaff>();
}

public class DisponibilidadStaff : BaseEntity
{
    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

// --- VENTAS Y CONTRATOS ---
public class Contrato : BaseEntity
{
    public string NumeroContrato { get; set; } = string.Empty;
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public decimal TotalUSD { get; set; }
    public string Estado { get; set; } = "Borrador";
    public ICollection<ContractItem> Items { get; set; } = new List<ContractItem>();
    public ICollection<Sesion> Sesiones { get; set; } = new List<Sesion>();
    public ICollection<Suplemento> Suplementos { get; set; } = new List<Suplemento>();
}

public class ContractItem : BaseEntity
{
    public Guid ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;
    public Guid? ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal PrecioVentaUSD { get; set; }
    public decimal SubtotalUSD { get; set; }
}

public class Suplemento : BaseEntity
{
    public Guid ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;
    public string NumeroSuplemento { get; set; } = string.Empty;
    public string Estado { get; set; } = "Borrador";
    public decimal TotalUSD { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public ICollection<SuplementoItem> Items { get; set; } = new List<SuplementoItem>();
}

public class SuplementoItem : BaseEntity
{
    public Guid SuplementoId { get; set; }
    public Suplemento Suplemento { get; set; } = null!;
    public Guid? ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal PrecioVentaUSD { get; set; }
    public decimal SubtotalUSD { get; set; }
}

public class Sesion : BaseEntity
{
    public Guid ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Locacion { get; set; } = string.Empty;
    public ICollection<SessionStaff> PersonalAsignado { get; set; } = new List<SessionStaff>();
}

public class SessionStaff : BaseEntity
{
    public Guid SesionId { get; set; }
    public Sesion Sesion { get; set; } = null!;
    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public decimal ComisionPactadaUSD { get; set; }
}

// --- SECUENCIAS ---
public class DocumentSequence : BaseEntity
{
    public string DocumentType { get; set; } = string.Empty;
    public int Year { get; set; }
    public int LastNumber { get; set; }
}

// --- FINANZAS ---
public class Pago : BaseEntity
{
    public Guid ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "USD";
    public decimal TasaCambio { get; set; } = 1;
    public string MetodoPago { get; set; } = string.Empty;
    public string ReferenciaExterna { get; set; } = string.Empty;
    public string EstadoConciliacion { get; set; } = "PENDIENTE";
    public DateTime FechaPago { get; set; }
}

public class Gasto : BaseEntity
{
    public string Categoria { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "USD";
    public DateTime Fecha { get; set; }
}

public class Comision : BaseEntity
{
    public Guid SessionStaffId { get; set; }
    public SessionStaff SessionStaff { get; set; } = null!;
    public decimal MontoComisionUSD { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public DateTime? FechaPago { get; set; }
}