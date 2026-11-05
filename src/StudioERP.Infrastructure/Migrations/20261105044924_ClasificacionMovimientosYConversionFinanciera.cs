using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClasificacionMovimientosYConversionFinanciera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoUSD",
                table: "PagosPersonalIndirecto",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TasaCambio",
                table: "PagosPersonalIndirecto",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoUSD",
                table: "Gastos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TasaCambio",
                table: "Gastos",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoMovimiento",
                table: "Gastos",
                type: "text",
                nullable: false,
                defaultValue: "GastoOperativo");

            migrationBuilder.Sql("""
                UPDATE "Gastos"
                SET "MontoUSD" = CASE WHEN "Moneda" = 'USD' THEN "Monto" ELSE NULL END,
                    "TasaCambio" = CASE WHEN "Moneda" = 'USD' THEN 1 ELSE NULL END,
                    "TipoMovimiento" = 'GastoOperativo';

                UPDATE "PagosPersonalIndirecto"
                SET "MontoUSD" = CASE WHEN "Moneda" = 'USD' THEN "Monto" ELSE NULL END,
                    "TasaCambio" = CASE WHEN "Moneda" = 'USD' THEN 1 ELSE NULL END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoUSD",
                table: "PagosPersonalIndirecto");

            migrationBuilder.DropColumn(
                name: "TasaCambio",
                table: "PagosPersonalIndirecto");

            migrationBuilder.DropColumn(
                name: "MontoUSD",
                table: "Gastos");

            migrationBuilder.DropColumn(
                name: "TasaCambio",
                table: "Gastos");

            migrationBuilder.DropColumn(
                name: "TipoMovimiento",
                table: "Gastos");
        }
    }
}
