using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shipping_Form_CreatorV1.Migrations
{
    /// <inheritdoc />
    public partial class updatehandlingunit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HandlingUnit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportModelId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandlingUnit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandlingUnit_ReportModels_ReportModelId",
                        column: x => x.ReportModelId,
                        principalTable: "ReportModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LineItemPackingUnits_HandlingUnitId",
                table: "LineItemPackingUnits",
                column: "HandlingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnit_ReportModelId",
                table: "HandlingUnit",
                column: "ReportModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_LineItemPackingUnits_HandlingUnit_HandlingUnitId",
                table: "LineItemPackingUnits",
                column: "HandlingUnitId",
                principalTable: "HandlingUnit",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LineItemPackingUnits_HandlingUnit_HandlingUnitId",
                table: "LineItemPackingUnits");

            migrationBuilder.DropTable(
                name: "HandlingUnit");

            migrationBuilder.DropIndex(
                name: "IX_LineItemPackingUnits_HandlingUnitId",
                table: "LineItemPackingUnits");
        }
    }
}
