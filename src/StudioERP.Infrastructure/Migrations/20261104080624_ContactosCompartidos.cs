using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContactosCompartidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contactos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Valor = table.Column<string>(type: "text", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProveedorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contactos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contactos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contactos_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contactos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO "Contactos" ("Id", "Tipo", "Valor", "ClienteId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
                SELECT gen_random_uuid(), contacto."Tipo", contacto."Valor", cliente."Id",
                       cliente."CreatedAt", cliente."CreatedBy", cliente."UpdatedAt", cliente."UpdatedBy", false
                FROM "Clientes" AS cliente
                CROSS JOIN LATERAL (
                    VALUES
                        ('Telefono', cliente."Telefono"),
                        ('Email', cliente."Email"),
                        ('Direccion', cliente."Direccion")
                ) AS contacto("Tipo", "Valor")
                WHERE NULLIF(BTRIM(contacto."Valor"), '') IS NOT NULL;

                INSERT INTO "Contactos" ("Id", "Tipo", "Valor", "UsuarioId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
                SELECT gen_random_uuid(), 'Email', usuario."Email", usuario."Id",
                       usuario."CreatedAt", usuario."CreatedBy", usuario."UpdatedAt", usuario."UpdatedBy", false
                FROM "Usuarios" AS usuario
                WHERE NULLIF(BTRIM(usuario."Email"), '') IS NOT NULL;

                INSERT INTO "Contactos" ("Id", "Tipo", "Valor", "ProveedorId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
                SELECT gen_random_uuid(), 'Telefono', proveedor."Telefono", proveedor."Id",
                       proveedor."CreatedAt", proveedor."CreatedBy", proveedor."UpdatedAt", proveedor."UpdatedBy", false
                FROM "Proveedores" AS proveedor
                WHERE NULLIF(BTRIM(proveedor."Telefono"), '') IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Contactos_ClienteId",
                table: "Contactos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Contactos_ProveedorId",
                table: "Contactos",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contactos_UsuarioId",
                table: "Contactos",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contactos");
        }
    }
}
