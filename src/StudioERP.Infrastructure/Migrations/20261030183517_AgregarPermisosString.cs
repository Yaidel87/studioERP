using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPermisosString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PuedeAgregar",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "PuedeConsultar",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "PuedeEditar",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "PuedeEliminar",
                table: "Roles");

            migrationBuilder.AddColumn<string>(
                name: "Permisos",
                table: "Roles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Permisos",
                table: "Roles");

            migrationBuilder.AddColumn<bool>(
                name: "PuedeAgregar",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeConsultar",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeEditar",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeEliminar",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
