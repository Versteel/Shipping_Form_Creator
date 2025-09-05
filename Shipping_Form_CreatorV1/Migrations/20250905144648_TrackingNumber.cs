using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shipping_Form_CreatorV1.Migrations
{
    /// <inheritdoc />
    public partial class TrackingNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShippingInstructions",
                table: "ReportHeaders",
                newName: "TrackingNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TrackingNumber",
                table: "ReportHeaders",
                newName: "ShippingInstructions");
        }
    }
}
