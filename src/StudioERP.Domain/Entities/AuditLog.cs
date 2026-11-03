namespace StudioERP.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityName { get; set; } = string.Empty; // Ej: "Cliente", "Contrato"
    public string EntityId { get; set; } = string.Empty;   // El ID del registro modificado
    public string Action { get; set; } = string.Empty;     // "ADDED", "MODIFIED", "DELETED"
    public string Changes { get; set; } = string.Empty;    // JSON con el antes y después
    public DateTime Timestamp { get; set; }
    public Guid? UserId { get; set; }                      // Quién lo hizo
}