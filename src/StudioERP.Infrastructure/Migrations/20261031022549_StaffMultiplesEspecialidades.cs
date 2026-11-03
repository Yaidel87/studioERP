using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StaffMultiplesEspecialidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staffs_TiposStaff_TipoStaffId",
                table: "Staffs");

            migrationBuilder.DropIndex(
                name: "IX_Staffs_TipoStaffId",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "TipoStaffId",
                table: "Staffs");

            migrationBuilder.CreateTable(
                name: "StaffTipoStaff",
                columns: table => new
                {
                    StaffsId = table.Column<Guid>(type: "uuid", nullable: false),
                    TiposStaffId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffTipoStaff", x => new { x.StaffsId, x.TiposStaffId });
                    table.ForeignKey(
                        name: "FK_StaffTipoStaff_Staffs_StaffsId",
                        column: x => x.StaffsId,
                        principalTable: "Staffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffTipoStaff_TiposStaff_TiposStaffId",
                        column: x => x.TiposStaffId,
                        principalTable: "TiposStaff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffTipoStaff_TiposStaffId",
                table: "StaffTipoStaff",
                column: "TiposStaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffTipoStaff");

            migrationBuilder.AddColumn<Guid>(
                name: "TipoStaffId",
                table: "Staffs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_TipoStaffId",
                table: "Staffs",
                column: "TipoStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Staffs_TiposStaff_TipoStaffId",
                table: "Staffs",
                column: "TipoStaffId",
                principalTable: "TiposStaff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
