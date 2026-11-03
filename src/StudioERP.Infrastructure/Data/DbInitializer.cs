using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudioERP.Domain.Entities;

namespace StudioERP.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StudioDbContext>();

        // --- 1. SEMBRAR ROLES ---
        var rolesBase = new List<Rol>
        {
            new() { 
                Nombre = "Admin", 
                Descripcion = "Acceso total al sistema", 
                Permisos = "Agenda.Ver,Agenda.Editar,Clientes.Ver,Clientes.Crear,Clientes.Editar,Clientes.Eliminar,Contratos.Ver,Contratos.Crear,Contratos.Editar,Contratos.Cancelar,Catalogos.Ver,Catalogos.Crear,Catalogos.Editar,Finanzas.Ver,Finanzas.RegistrarPago,Finanzas.VerificarPago,Finanzas.VerComisiones,Usuarios.Ver,Usuarios.Crear,Usuarios.Editar,Usuarios.Eliminar,Roles.Ver,Roles.Crear,Roles.Editar,Staff.Ver,Staff.Crear,Staff.Editar,Proveedores.Ver,Proveedores.Crear,Proveedores.Editar" 
            },
            new() { Nombre = "Fotógrafo", Descripcion = "Toma las fotos, ve su agenda y marca días libres", Permisos = "Agenda.Ver,Agenda.Editar,Catalogos.Ver" },
            new() { Nombre = "Maquillador", Descripcion = "Apoya en las sesiones, ve su agenda y marca días libres", Permisos = "Agenda.Ver,Agenda.Editar" },
            new() { Nombre = "Editor", Descripcion = "Se encarga de la edición y retoque de fotos", Permisos = "Agenda.Ver,Catalogos.Ver" },
            new() { Nombre = "Cajero", Descripcion = "Atiende clientes, crea contratos y registra pagos", Permisos = "Clientes.Ver,Clientes.Crear,Clientes.Editar,Contratos.Ver,Contratos.Crear,Finanzas.Ver,Finanzas.RegistrarPago" }
        };

        foreach (var rol in rolesBase)
        {
            if (!await context.Roles.AnyAsync(r => r.Nombre == rol.Nombre))
            {
                context.Roles.Add(rol);
            }
        }
        await context.SaveChangesAsync();

        // --- 2. SEMBRAR ESPECIALIDADES (TIPOS DE STAFF) ---
        var especialidadesBase = new List<string> { "Fotógrafo", "Maquillador", "Editor" };

        foreach (var especialidad in especialidadesBase)
        {
            if (!await context.TiposStaff.AnyAsync(t => t.Nombre == especialidad))
            {
                context.TiposStaff.Add(new TipoStaff { Nombre = especialidad });
            }
        }
        await context.SaveChangesAsync();
    }
}