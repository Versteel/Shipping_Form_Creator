using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shipping_Form_CreatorV1.Migrations
{
    /// <inheritdoc />
    public partial class lineitemtrucknum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TruckNumber",
                table: "LineItemPackingUnits");

            migrationBuilder.AddColumn<string>(
                name: "TruckNumber",
                table: "LineItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TruckNumber",
                table: "LineItems");

            migrationBuilder.AddColumn<string>(
                name: "TruckNumber",
                table: "LineItemPackingUnits",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
