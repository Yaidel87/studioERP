using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDetalleCostoProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaqueteItems_Productos_ProductoId",
                table: "PaqueteItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PaqueteItems_Servicios_ServicioId",
                table: "PaqueteItems");

            migrationBuilder.DropTable(
                name: "FacturaProveedorItems");

            migrationBuilder.DropTable(
                name: "FacturasProveedores");

            migrationBuilder.DropIndex(
                name: "IX_PaqueteItems_ServicioId",
                table: "PaqueteItems");

            migrationBuilder.DropColumn(
                name: "ServicioId",
                table: "PaqueteItems");

            migrationBuilder.DropColumn(
                name: "EsSuplemento",
                table: "ContractItems");

            migrationBuilder.AddColumn<string>(
                name: "DetalleCosto",
                table: "Productos",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductoId",
                table: "PaqueteItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioNuevoUSD",
                table: "HistorialPreciosProductos",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioAnteriorUSD",
                table: "HistorialPreciosProductos",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "CostoNuevoUSD",
                table: "HistorialCostosProveedores",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "CostoAnteriorUSD",
                table: "HistorialCostosProveedores",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddForeignKey(
                name: "FK_PaqueteItems_Productos_ProductoId",
                table: "PaqueteItems",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaqueteItems_Productos_ProductoId",
                table: "PaqueteItems");

            migrationBuilder.DropColumn(
                name: "DetalleCosto",
                table: "Productos");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductoId",
                table: "PaqueteItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ServicioId",
                table: "PaqueteItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioNuevoUSD",
                table: "HistorialPreciosProductos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioAnteriorUSD",
                table: "HistorialPreciosProductos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "CostoNuevoUSD",
                table: "HistorialCostosProveedores",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "CostoAnteriorUSD",
                table: "HistorialCostosProveedores",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<bool>(
                name: "EsSuplemento",
                table: "ContractItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FacturasProveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaFactura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MetodoPago = table.Column<string>(type: "text", nullable: false),
                    NumeroFactura = table.Column<string>(type: "text", nullable: false),
                    TotalFacturaUSD = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasProveedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturasProveedores_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FacturaProveedorItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FacturaProveedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    CostoUnitarioFacturaUSD = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaProveedorItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaProveedorItems_FacturasProveedores_FacturaProveedorId",
                        column: x => x.FacturaProveedorId,
                        principalTable: "FacturasProveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturaProveedorItems_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaqueteItems_ServicioId",
                table: "PaqueteItems",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaProveedorItems_FacturaProveedorId",
                table: "FacturaProveedorItems",
                column: "FacturaProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaProveedorItems_ProductoId",
                table: "FacturaProveedorItems",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasProveedores_ProveedorId",
                table: "FacturasProveedores",
                column: "ProveedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaqueteItems_Productos_ProductoId",
                table: "PaqueteItems",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaqueteItems_Servicios_ServicioId",
                table: "PaqueteItems",
                column: "ServicioId",
                principalTable: "Servicios",
                principalColumn: "Id");
        }
    }
}
