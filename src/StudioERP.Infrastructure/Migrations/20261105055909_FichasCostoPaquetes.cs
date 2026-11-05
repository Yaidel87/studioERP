using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FichasCostoPaquetes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FichasCostoPaquetes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaqueteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Contexto = table.Column<string>(type: "text", nullable: false),
                    CostoTotalUSD = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MultiplicadorPrecioMinimo = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    MultiplicadorPrecioIdeal = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    PrecioVentaUSD = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichasCostoPaquetes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FichasCostoPaquetes_Paquetes_PaqueteId",
                        column: x => x.PaqueteId,
                        principalTable: "Paquetes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartidasCostoPaquetes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FichaCostoPaqueteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Concepto = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoUnitarioUSD = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SubtotalUSD = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartidasCostoPaquetes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartidasCostoPaquetes_FichasCostoPaquetes_FichaCostoPaquete~",
                        column: x => x.FichaCostoPaqueteId,
                        principalTable: "FichasCostoPaquetes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FichasCostoPaquetes_PaqueteId",
                table: "FichasCostoPaquetes",
                column: "PaqueteId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasCostoPaquetes_FichaCostoPaqueteId_Orden",
                table: "PartidasCostoPaquetes",
                columns: new[] { "FichaCostoPaqueteId", "Orden" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartidasCostoPaquetes");

            migrationBuilder.DropTable(
                name: "FichasCostoPaquetes");
        }
    }
}
