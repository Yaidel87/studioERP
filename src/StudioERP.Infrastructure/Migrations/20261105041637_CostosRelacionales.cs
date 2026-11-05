using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CostosRelacionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MultiplicadorPrecioIdeal",
                table: "FichasCostoProductos",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 3m);

            migrationBuilder.AddColumn<decimal>(
                name: "MultiplicadorPrecioMinimo",
                table: "FichasCostoProductos",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 2m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioVentaUSD",
                table: "FichasCostoProductos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Variante",
                table: "FichasCostoProductos",
                type: "text",
                nullable: false,
                defaultValue: "General");

            migrationBuilder.AddColumn<Guid>(
                name: "FichaCostoProductoId",
                table: "ContractItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PartidasCostoFicha",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FichaCostoProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Concepto = table.Column<string>(type: "text", nullable: false),
                    TipoCalculo = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoUnitarioUSD = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Porcentaje = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    IncluirEnBasePorcentual = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_PartidasCostoFicha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartidasCostoFicha_FichasCostoProductos_FichaCostoProductoId",
                        column: x => x.FichaCostoProductoId,
                        principalTable: "FichasCostoProductos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                UPDATE "FichasCostoProductos"
                SET "Variante" = COALESCE(NULLIF(BTRIM("DetalleCosto" ->> 'Formato'), ''), 'General'),
                    "MultiplicadorPrecioMinimo" = 2,
                    "MultiplicadorPrecioIdeal" = 3;

                CREATE TEMP TABLE "_CostosRelacionalesProductoFicha"
                    ("FichaId" uuid NOT NULL, "ProductoId" uuid NOT NULL)
                    ON COMMIT DROP;

                INSERT INTO "_CostosRelacionalesProductoFicha" ("FichaId", "ProductoId")
                SELECT gen_random_uuid(), producto."Id"
                FROM "Productos" AS producto;

                INSERT INTO "FichasCostoProductos"
                    ("Id", "ProductoId", "DetalleCosto", "Variante", "Contexto", "CostoTotalUSD",
                     "MultiplicadorPrecioMinimo", "MultiplicadorPrecioIdeal", "PrecioVentaUSD",
                     "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
                SELECT mapping."FichaId", producto."Id", '{}'::jsonb,
                       COALESCE(NULLIF(BTRIM(producto."DetalleCosto" ->> 'Formato'), ''), 'General'),
                       'General', 0, 2, 3, producto."PrecioVentaBaseUSD",
                       producto."CreatedAt", producto."CreatedBy",
                       producto."UpdatedAt", producto."UpdatedBy", producto."IsDeleted"
                FROM "Productos" AS producto
                INNER JOIN "_CostosRelacionalesProductoFicha" AS mapping
                    ON mapping."ProductoId" = producto."Id";

                INSERT INTO "PartidasCostoFicha"
                    ("Id", "FichaCostoProductoId", "Concepto", "TipoCalculo", "Cantidad",
                     "CostoUnitarioUSD", "Porcentaje", "IncluirEnBasePorcentual",
                     "SubtotalUSD", "Orden", "CreatedAt", "CreatedBy",
                     "UpdatedAt", "UpdatedBy", "IsDeleted")
                SELECT gen_random_uuid(), origen."FichaId", partida."Concepto", 'Fijo',
                       CASE WHEN partida."Clave" = 'CostoPliego'
                                  AND origen."Categoria" = 'Photobooks'
                            THEN GREATEST(CASE
                                WHEN COALESCE(origen."DetalleCosto" ->> 'CantidadPliegos', '') ~ '^-?[0-9]+([.][0-9]+)?([eE][-+]?[0-9]+)?$'
                                    THEN (origen."DetalleCosto" ->> 'CantidadPliegos')::numeric
                                ELSE 0
                            END, 0)
                            ELSE 1
                       END,
                       costo."CostoUnitarioUSD", 0, true,
                       ROUND(costo."CostoUnitarioUSD" *
                           CASE WHEN partida."Clave" = 'CostoPliego'
                                      AND origen."Categoria" = 'Photobooks'
                                THEN GREATEST(CASE
                                    WHEN COALESCE(origen."DetalleCosto" ->> 'CantidadPliegos', '') ~ '^-?[0-9]+([.][0-9]+)?([eE][-+]?[0-9]+)?$'
                                        THEN (origen."DetalleCosto" ->> 'CantidadPliegos')::numeric
                                    ELSE 0
                                END, 0)
                                ELSE 1
                           END, 2),
                       partida."Orden", origen."CreatedAt", origen."CreatedBy",
                       origen."UpdatedAt", origen."UpdatedBy", false
                FROM (
                    SELECT ficha."Id" AS "FichaId", ficha."DetalleCosto", ficha."CreatedAt",
                           ficha."CreatedBy", ficha."UpdatedAt", ficha."UpdatedBy",
                           categoria."Nombre" AS "Categoria"
                    FROM "FichasCostoProductos" AS ficha
                    INNER JOIN "Productos" AS producto ON producto."Id" = ficha."ProductoId"
                    LEFT JOIN "CategoriasProductos" AS categoria ON categoria."Id" = producto."CategoriaProductoId"
                    UNION ALL
                    SELECT mapping."FichaId", producto."DetalleCosto", producto."CreatedAt",
                           producto."CreatedBy", producto."UpdatedAt", producto."UpdatedBy",
                           categoria."Nombre"
                    FROM "_CostosRelacionalesProductoFicha" AS mapping
                    INNER JOIN "Productos" AS producto ON producto."Id" = mapping."ProductoId"
                    LEFT JOIN "CategoriasProductos" AS categoria ON categoria."Id" = producto."CategoriaProductoId"
                ) AS origen
                CROSS JOIN LATERAL (
                    VALUES
                        ('CostoImpresion', 'Impresión', 1),
                        ('CostoEdicion', 'Edición', 2),
                        ('CostoEnmarcado', 'Enmarcado', 3),
                        ('CostoConfeccion', 'Confección', 4),
                        ('CostoEsquinero', 'Esquinero', 5),
                        ('CostoDiseno', 'Diseño', 6),
                        ('CostoPortada', 'Portada', 7),
                        ('CostoPliego', 'Pliego', 8),
                        ('CostoMontaje', 'Montaje', 9),
                        ('CostoBase', 'Costo base', 10),
                        ('OtrosGastos', 'Otros gastos', 11)
                ) AS partida("Clave", "Concepto", "Orden")
                CROSS JOIN LATERAL (
                    SELECT CASE
                        WHEN COALESCE(origen."DetalleCosto" ->> partida."Clave", '') ~ '^-?[0-9]+([.][0-9]+)?([eE][-+]?[0-9]+)?$'
                            THEN (origen."DetalleCosto" ->> partida."Clave")::numeric
                        ELSE 0
                    END AS "CostoUnitarioUSD"
                ) AS costo
                WHERE costo."CostoUnitarioUSD" > 0;

                UPDATE "FichasCostoProductos" AS ficha
                SET "CostoTotalUSD" = COALESCE((
                    SELECT SUM(partida."SubtotalUSD")
                    FROM "PartidasCostoFicha" AS partida
                    WHERE partida."FichaCostoProductoId" = ficha."Id"
                ), ficha."CostoTotalUSD");
                """);

            migrationBuilder.DropColumn(
                name: "DetalleCosto",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "DetalleCosto",
                table: "FichasCostoProductos");

            migrationBuilder.CreateIndex(
                name: "IX_ContractItems_FichaCostoProductoId",
                table: "ContractItems",
                column: "FichaCostoProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasCostoFicha_FichaCostoProductoId_Orden",
                table: "PartidasCostoFicha",
                columns: new[] { "FichaCostoProductoId", "Orden" });

            migrationBuilder.AddForeignKey(
                name: "FK_ContractItems_FichasCostoProductos_FichaCostoProductoId",
                table: "ContractItems",
                column: "FichaCostoProductoId",
                principalTable: "FichasCostoProductos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractItems_FichasCostoProductos_FichaCostoProductoId",
                table: "ContractItems");

            migrationBuilder.DropTable(
                name: "PartidasCostoFicha");

            migrationBuilder.DropIndex(
                name: "IX_ContractItems_FichaCostoProductoId",
                table: "ContractItems");

            migrationBuilder.DropColumn(
                name: "MultiplicadorPrecioIdeal",
                table: "FichasCostoProductos");

            migrationBuilder.DropColumn(
                name: "MultiplicadorPrecioMinimo",
                table: "FichasCostoProductos");

            migrationBuilder.DropColumn(
                name: "PrecioVentaUSD",
                table: "FichasCostoProductos");

            migrationBuilder.DropColumn(
                name: "Variante",
                table: "FichasCostoProductos");

            migrationBuilder.DropColumn(
                name: "FichaCostoProductoId",
                table: "ContractItems");

            migrationBuilder.AddColumn<string>(
                name: "DetalleCosto",
                table: "Productos",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DetalleCosto",
                table: "FichasCostoProductos",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }
    }
}
