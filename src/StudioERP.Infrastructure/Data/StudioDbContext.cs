using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
    public DbSet<FichaCostoProducto> FichasCostoProductos { get; set; }
    public DbSet<PartidaCostoFicha> PartidasCostoFicha { get; set; }
    public DbSet<Promocion> Promociones { get; set; }
    public DbSet<CategoriaProducto> CategoriasProductos { get; set; }
    public DbSet<Proveedor> Proveedores { get; set; }
    public DbSet<ProveedorProducto> ProveedorProductos { get; set; }
    public DbSet<HistorialCostoProveedor> HistorialCostosProveedores { get; set; }
    public DbSet<HistorialPrecioProducto> HistorialPreciosProductos { get; set; }
    public DbSet<Paquete> Paquetes { get; set; }
    public DbSet<PaqueteItem> PaqueteItems { get; set; }
    public DbSet<FichaCostoPaquete> FichasCostoPaquetes { get; set; }
    public DbSet<PartidaCostoPaquete> PartidasCostoPaquetes { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Contacto> Contactos { get; set; }
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
    public DbSet<TipoCambioDiario> TiposCambioDiarios { get; set; }
    public DbSet<CambioMoneda> CambiosMoneda { get; set; }
    public DbSet<Comision> Comisiones { get; set; }
    public DbSet<PagoPersonalIndirecto> PagosPersonalIndirecto { get; set; }
    public DbSet<Gasto> Gastos { get; set; }
    public DbSet<DocumentSequence> DocumentSequences { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "Token",
        "RefreshToken",
        "Secret",
        "SecurityStamp"
    };

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
        modelBuilder.Entity<Pago>().Property(p => p.MontoUSD).HasPrecision(18, 2);
        modelBuilder.Entity<PagoPersonalIndirecto>().Property(p => p.Monto).HasPrecision(18, 2);
        modelBuilder.Entity<PagoPersonalIndirecto>().Property(p => p.MontoUSD).HasPrecision(18, 2);
        modelBuilder.Entity<PagoPersonalIndirecto>().Property(p => p.TasaCambio).HasPrecision(18, 4);
        modelBuilder.Entity<Pago>().Property(p => p.TasaCambio).HasPrecision(18, 4);
        modelBuilder.Entity<TipoCambioDiario>().Property(rate => rate.CupPorUsd).HasPrecision(18, 4);
        modelBuilder.Entity<CambioMoneda>().Property(change => change.MontoCUP).HasPrecision(18, 2);
        modelBuilder.Entity<CambioMoneda>().Property(change => change.CupPorUsd).HasPrecision(18, 4);
        modelBuilder.Entity<CambioMoneda>().Property(change => change.MontoUSDRecibido).HasPrecision(18, 2);
        modelBuilder.Entity<Comision>().Property(c => c.MontoComisionUSD).HasPrecision(18, 2);
        modelBuilder.Entity<Suplemento>().Property(s => s.TotalUSD).HasPrecision(18, 2);
        modelBuilder.Entity<SuplementoItem>().Property(s => s.PrecioVentaUSD).HasPrecision(18, 2);
        modelBuilder.Entity<SuplementoItem>().Property(s => s.SubtotalUSD).HasPrecision(18, 2);
        modelBuilder.Entity<Gasto>().Property(g => g.Monto).HasPrecision(18, 2);
        modelBuilder.Entity<Gasto>().Property(g => g.MontoUSD).HasPrecision(18, 2);
        modelBuilder.Entity<Gasto>().Property(g => g.TasaCambio).HasPrecision(18, 4);
        modelBuilder.Entity<FichaCostoProducto>().Property(f => f.CostoTotalUSD).HasPrecision(18, 2);
        modelBuilder.Entity<FichaCostoProducto>().Property(f => f.MultiplicadorPrecioMinimo).HasPrecision(8, 2);
        modelBuilder.Entity<FichaCostoProducto>().Property(f => f.MultiplicadorPrecioIdeal).HasPrecision(8, 2);
        modelBuilder.Entity<FichaCostoProducto>().Property(f => f.PrecioVentaUSD).HasPrecision(18, 2);
        modelBuilder.Entity<PartidaCostoFicha>().Property(item => item.Cantidad).HasPrecision(18, 4);
        modelBuilder.Entity<PartidaCostoFicha>().Property(item => item.CostoUnitarioUSD).HasPrecision(18, 4);
        modelBuilder.Entity<PartidaCostoFicha>().Property(item => item.Porcentaje).HasPrecision(8, 4);
        modelBuilder.Entity<PartidaCostoFicha>().Property(item => item.SubtotalUSD).HasPrecision(18, 2);
        modelBuilder.Entity<FichaCostoPaquete>().Property(ficha => ficha.CostoTotalUSD).HasPrecision(18, 2);
        modelBuilder.Entity<FichaCostoPaquete>().Property(ficha => ficha.MultiplicadorPrecioMinimo).HasPrecision(8, 2);
        modelBuilder.Entity<FichaCostoPaquete>().Property(ficha => ficha.MultiplicadorPrecioIdeal).HasPrecision(8, 2);
        modelBuilder.Entity<FichaCostoPaquete>().Property(ficha => ficha.PrecioVentaUSD).HasPrecision(18, 2);
        modelBuilder.Entity<PartidaCostoPaquete>().Property(item => item.Cantidad).HasPrecision(18, 4);
        modelBuilder.Entity<PartidaCostoPaquete>().Property(item => item.CostoUnitarioUSD).HasPrecision(18, 4);
        modelBuilder.Entity<PartidaCostoPaquete>().Property(item => item.SubtotalUSD).HasPrecision(18, 4);
        modelBuilder.Entity<Promocion>().Property(p => p.ValorDescuento).HasPrecision(18, 2);

        // Índices
        modelBuilder.Entity<Usuario>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<DocumentSequence>().HasIndex(d => new { d.DocumentType, d.Year }).IsUnique();
        modelBuilder.Entity<TipoCambioDiario>().HasIndex(rate => rate.Fecha).IsUnique();
        modelBuilder.Entity<Cliente>().HasIndex(c => c.Telefono);
        modelBuilder.Entity<Contacto>().HasIndex(c => c.ClienteId);
        modelBuilder.Entity<Contacto>().HasIndex(c => c.UsuarioId);
        modelBuilder.Entity<Contacto>().HasIndex(c => c.ProveedorId);
        modelBuilder.Entity<Contacto>()
            .HasOne(c => c.Cliente)
            .WithMany(c => c.Contactos)
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FichaCostoProducto>()
            .HasOne(f => f.Producto)
            .WithMany(p => p.FichasCosto)
            .HasForeignKey(f => f.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartidaCostoFicha>()
            .HasOne(item => item.FichaCostoProducto)
            .WithMany(ficha => ficha.Partidas)
            .HasForeignKey(item => item.FichaCostoProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartidaCostoFicha>()
            .HasIndex(item => new { item.FichaCostoProductoId, item.Orden });
        modelBuilder.Entity<FichaCostoPaquete>()
            .HasOne(ficha => ficha.Paquete)
            .WithMany(paquete => paquete.FichasCosto)
            .HasForeignKey(ficha => ficha.PaqueteId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartidaCostoPaquete>()
            .HasOne(item => item.FichaCostoPaquete)
            .WithMany(ficha => ficha.Partidas)
            .HasForeignKey(item => item.FichaCostoPaqueteId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartidaCostoPaquete>()
            .HasIndex(item => new { item.FichaCostoPaqueteId, item.Orden });
        modelBuilder.Entity<Promocion>()
            .HasOne(p => p.Producto)
            .WithMany(producto => producto.Promociones)
            .HasForeignKey(p => p.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Promocion>()
            .HasOne(p => p.Paquete)
            .WithMany(paquete => paquete.Promociones)
            .HasForeignKey(p => p.PaqueteId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PagoPersonalIndirecto>()
            .HasOne(p => p.Staff)
            .WithMany()
            .HasForeignKey(p => p.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ContractItem>()
            .HasOne(item => item.Paquete)
            .WithMany()
            .HasForeignKey(item => item.PaqueteId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ContractItem>()
            .HasOne(item => item.FichaCostoProducto)
            .WithMany()
            .HasForeignKey(item => item.FichaCostoProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ContractItem>()
            .HasOne(item => item.Promocion)
            .WithMany()
            .HasForeignKey(item => item.PromocionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Contacto>()
            .HasOne(c => c.Usuario)
            .WithMany(u => u.Contactos)
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Contacto>()
            .HasOne(c => c.Proveedor)
            .WithMany(p => p.Contactos)
            .HasForeignKey(c => c.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Relación Suplemento -> Contrato
        modelBuilder.Entity<Suplemento>()
            .HasOne(s => s.Contrato)
            .WithMany(c => c.Suplementos)
            .HasForeignKey(s => s.ContratoId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChangesForSave();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareChangesForSave();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) 
                       ?? _httpContextAccessor.HttpContext?.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
            return userId;
        
        return null;
    }

    private void PrepareChangesForSave()
    {
        var userId = GetCurrentUserId();
        var timestamp = DateTime.UtcNow;
        var softDeletedEntities = new HashSet<object>(ReferenceEqualityComparer.Instance);

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = timestamp;
                entry.Entity.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = timestamp;
                entry.Entity.UpdatedBy = userId;
            }
            else if (entry.State == EntityState.Deleted)
            {
                softDeletedEntities.Add(entry.Entity);
                entry.Entity.IsDeleted = true;
                entry.Entity.UpdatedAt = timestamp;
                entry.Entity.UpdatedBy = userId;
                entry.State = EntityState.Modified;
            }
        }

        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var action = softDeletedEntities.Contains(entry.Entity)
                ? "DELETED"
                : entry.State switch
                {
                    EntityState.Added => "CREATED",
                    EntityState.Modified => "UPDATED",
                    EntityState.Deleted => "DELETED",
                    _ => null
                };

            if (action == null)
                continue;

            var auditEntry = new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetEntityId(entry),
                Action = action,
                Timestamp = timestamp,
                UserId = userId
            };

            var changes = new Dictionary<string, object?>();
            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary) continue;
                string propertyName = property.Metadata.Name;

                if (IsSensitiveProperty(propertyName))
                    continue;

                if (property.Metadata.IsPrimaryKey())
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        changes[propertyName] = property.CurrentValue;
                        break;
                    case EntityState.Deleted:
                        changes[propertyName] = property.OriginalValue;
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

        AuditLogs.AddRange(auditEntries);
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey == null)
            return string.Empty;

        return string.Join(",", primaryKey.Properties.Select(property =>
            entry.Property(property.Name).CurrentValue?.ToString() ?? string.Empty));
    }

    private static bool IsSensitiveProperty(string propertyName)
    {
        return SensitivePropertyNames.Contains(propertyName)
            || propertyName.EndsWith("Password", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Hash", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Token", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Secret", StringComparison.OrdinalIgnoreCase);
    }
}