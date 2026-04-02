using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkstationFromBOP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductBopLines_Workstations_WorkstationId",
                table: "ProductBopLines");

            migrationBuilder.DropColumn(
                name: "RunMinutes",
                table: "ProductBopLines");

            migrationBuilder.DropColumn(
                name: "SetupMinutes",
                table: "ProductBopLines");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkstationId",
                table: "ProductBopLines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBopLines_Workstations_WorkstationId",
                table: "ProductBopLines",
                column: "WorkstationId",
                principalTable: "Workstations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductBopLines_Workstations_WorkstationId",
                table: "ProductBopLines");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkstationId",
                table: "ProductBopLines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RunMinutes",
                table: "ProductBopLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SetupMinutes",
                table: "ProductBopLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBopLines_Workstations_WorkstationId",
                table: "ProductBopLines",
                column: "WorkstationId",
                principalTable: "Workstations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
