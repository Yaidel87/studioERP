using System.Text.Json.Serialization;
namespace StudioERP.Domain.Entities;


public class Rol : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

     // Guardamos los permisos como un string (ej: "Agenda.Ver,Agenda.Editar,Clientes.Ver")
    public string Permisos { get; set; } = string.Empty;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

public class Permiso : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public bool PuedeAgregar { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeEliminar { get; set; }
    public bool PuedeConsultar { get; set; }
    public ICollection<Rol> Roles { get; set; } = new List<Permiso>().Count == 0 ? new List<Rol>() : new List<Rol>(); // Corrección simple
}

public class Usuario : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty; // <-- AGREGAR ESTO
    public bool Activo { get; set; } = true;
    
    public Guid RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    
    [JsonIgnore]
    public Staff? Staff { get; set; } 
}