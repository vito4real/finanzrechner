using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzRechner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductBopLines_Workstations_WorkstationId",
                table: "ProductBopLines");

            migrationBuilder.DropColumn(
                name: "OperatorRatePerHour",
                table: "Workstations");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkstationId",
                table: "ProductBopLines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Duration",
                table: "ProductBopLines",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<Guid>(
                name: "JobPositionId",
                table: "ProductBopLines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "JobPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BaseHourlyRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPositions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBopLines_JobPositionId",
                table: "ProductBopLines",
                column: "JobPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBopLines_JobPositions_JobPositionId",
                table: "ProductBopLines",
                column: "JobPositionId",
                principalTable: "JobPositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBopLines_Workstations_WorkstationId",
                table: "ProductBopLines",
                column: "WorkstationId",
                principalTable: "Workstations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductBopLines_JobPositions_JobPositionId",
                table: "ProductBopLines");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductBopLines_Workstations_WorkstationId",
                table: "ProductBopLines");

            migrationBuilder.DropTable(
                name: "JobPositions");

            migrationBuilder.DropIndex(
                name: "IX_ProductBopLines_JobPositionId",
                table: "ProductBopLines");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "ProductBopLines");

            migrationBuilder.DropColumn(
                name: "JobPositionId",
                table: "ProductBopLines");

            migrationBuilder.AddColumn<decimal>(
                name: "OperatorRatePerHour",
                table: "Workstations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

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
    }
}
