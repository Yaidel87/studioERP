using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContratosConPaquetesPromocion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaqueteId",
                table: "ContractItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PromocionId",
                table: "ContractItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractItems_PaqueteId",
                table: "ContractItems",
                column: "PaqueteId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractItems_PromocionId",
                table: "ContractItems",
                column: "PromocionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractItems_Paquetes_PaqueteId",
                table: "ContractItems",
                column: "PaqueteId",
                principalTable: "Paquetes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractItems_Promociones_PromocionId",
                table: "ContractItems",
                column: "PromocionId",
                principalTable: "Promociones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractItems_Paquetes_PaqueteId",
                table: "ContractItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractItems_Promociones_PromocionId",
                table: "ContractItems");

            migrationBuilder.DropIndex(
                name: "IX_ContractItems_PaqueteId",
                table: "ContractItems");

            migrationBuilder.DropIndex(
                name: "IX_ContractItems_PromocionId",
                table: "ContractItems");

            migrationBuilder.DropColumn(
                name: "PaqueteId",
                table: "ContractItems");

            migrationBuilder.DropColumn(
                name: "PromocionId",
                table: "ContractItems");
        }
    }
}
