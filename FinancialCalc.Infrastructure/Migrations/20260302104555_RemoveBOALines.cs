using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBOALines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductBoaLines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductBoaLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductBomLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductBopLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBoaLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBoaLines_ProductBomLines_ProductBomLineId",
                        column: x => x.ProductBomLineId,
                        principalTable: "ProductBomLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductBoaLines_ProductBopLines_ProductBopLineId",
                        column: x => x.ProductBopLineId,
                        principalTable: "ProductBopLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBoaLines_ProductBomLineId_ProductBopLineId",
                table: "ProductBoaLines",
                columns: new[] { "ProductBomLineId", "ProductBopLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBoaLines_ProductBopLineId",
                table: "ProductBoaLines",
                column: "ProductBopLineId");
        }
    }
}
