using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace StudioERP.Infrastructure.Data;

public class StudioDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StudioDbContext(DbContextOptions<StudioDbContext> options, IHttpContextAccessor httpContextAccessor) 
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Rol> Roles { get; set; }
    public DbSet<Permiso> Permisos { get; set; }
    public DbSet<TipoServicio> TiposServicio { get; set; }
    public DbSet<Servicio> Servicios { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<CategoriaProducto> CategoriasProductos { get; set; }
    public DbSet<Proveedor> Proveedores { get; set; }
    public DbSet<ProveedorProducto> ProveedorProductos { get; set; }
    public DbSet<HistorialCostoProveedor> HistorialCostosProveedores { get; set; }
    public DbSet<HistorialPrecioProducto> HistorialPreciosProductos { get; set; }
    public DbSet<Paquete> Paquetes { get; set; }
    public DbSet<PaqueteItem> PaqueteItems { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Contrato> Contratos { get; set; }
    public DbSet<ContractItem> ContractItems { get; set; }
    public DbSet<Sesion> Sesiones { get; set; }
    public DbSet<SessionStaff> SessionStaffs { get; set; }
    public DbSet<Suplemento> Suplementos { get; set; }
    public DbSet<SuplementoItem> SuplementoItems { get; set; }
    public DbSet<TipoStaff> TiposStaff { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<DisponibilidadStaff> DisponibilidadesStaff { get; set; }
    public DbSet<Pago> Pagos { get; set; }
    public DbSet<Comision> Comisiones { get; set; }
    public DbSet<Gasto> Gastos { get; set; }
    public DbSet<DocumentSequence> DocumentSequences { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Precisión monetaria
        modelBuilder.Entity<Servicio>().Property(s => s.PrecioBaseUSD).HasPrecision(18, 2);
        modelBuilder.Entity<Producto>().Property(p => p.PrecioVentaBaseUSD).HasPrecision(18, 2);
        modelBuilder.Entity<ProveedorProducto>().Property(p => p.CostoUnitarioUSD).HasPrecision(18, 2);
        modelBuilder.Entity<Contrato>().Property(c => c.TotalUSD).HasPrecision(18, 2);
        modelBuilder.Entity<ContractItem>().Property(c => c.PrecioVentaUSD).HasPrecision(18, 2);
        modelBuilder.Entity<ContractItem>().Property(c => c.SubtotalUSD).HasPrecision(18, 2);
        modelBuilder.Entity<SessionStaff>().Property(s => s.ComisionPactadaUSD).HasPrecision(18, 2);
        modelBuilder.Entity<Pago>().Property(p => p.Monto).HasPrecision(18, 2);
        modelBuilder.Entity<Pago>().Property(p => p.TasaCambio).HasPrecision(18, 4);
        modelBuilder.Entity<Comision>().Property(c => c.MontoComisionUSD).HasPrecision(18, 2);
        modelBuilder.Entity<Suplemento>().Property(s => s.TotalUSD).HasPrecision(18, 2);
        modelBuilder.Entity<SuplementoItem>().Property(s => s.PrecioVentaUSD).HasPrecision(18, 2);
        modelBuilder.Entity<SuplementoItem>().Property(s => s.SubtotalUSD).HasPrecision(18, 2);
        modelBuilder.Entity<Gasto>().Property(g => g.Monto).HasPrecision(18, 2);

        // Índices
        modelBuilder.Entity<Usuario>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<DocumentSequence>().HasIndex(d => new { d.DocumentType, d.Year }).IsUnique();
        modelBuilder.Entity<Cliente>().HasIndex(c => c.Telefono);
        
        // Relación Suplemento -> Contrato
        modelBuilder.Entity<Suplemento>()
            .HasOne(s => s.Contrato)
            .WithMany(c => c.Suplementos)
            .HasForeignKey(s => s.ContratoId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = currentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedBy = currentUserId;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedBy = currentUserId;
            }
        }

        var auditEntries = OnBeforeSaveChanges(currentUserId);
        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditEntries.Any())
        {
            AuditLogs.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) 
                       ?? _httpContextAccessor.HttpContext?.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
            return userId;
        
        return null;
    }

    private List<AuditLog> OnBeforeSaveChanges(Guid? userId)
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                Action = entry.State.ToString().ToUpper(),
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };

            var changes = new Dictionary<string, object>();
            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary) continue;
                string propertyName = property.Metadata.Name;
                
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.EntityId = property.CurrentValue?.ToString() ?? "";
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        changes[propertyName] = property.CurrentValue!;
                        break;
                    case EntityState.Deleted:
                        changes[propertyName] = property.OriginalValue!;
                        break;
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            changes[propertyName] = new { Old = property.OriginalValue, New = property.CurrentValue };
                        }
                        break;
                }
            }

            auditEntry.Changes = JsonSerializer.Serialize(changes);
            auditEntries.Add(auditEntry);
        }

        return auditEntries;
    }
}