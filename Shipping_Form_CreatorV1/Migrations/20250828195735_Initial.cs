using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shipping_Form_CreatorV1.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LineItemHeaders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LineItemNumber = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProductNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ProductDescription = table.Column<string>(type: "TEXT", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    PickOrShipQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    BackOrderQuantity = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineItemHeaders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReportHeaderId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LineItemHeaderId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReportModelId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineItems_LineItemHeaders_LineItemHeaderId",
                        column: x => x.LineItemHeaderId,
                        principalTable: "LineItemHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LineItems_ReportModels_ReportModelId",
                        column: x => x.ReportModelId,
                        principalTable: "ReportModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportHeaders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LogoImagePath = table.Column<string>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OrdEnterDate = table.Column<string>(type: "TEXT", nullable: false),
                    ShipDate = table.Column<string>(type: "TEXT", nullable: false),
                    SoldToCustNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ShipToCustNumber = table.Column<string>(type: "TEXT", nullable: false),
                    SoldToName = table.Column<string>(type: "TEXT", nullable: false),
                    SoldToCustAddressLine1 = table.Column<string>(type: "TEXT", nullable: false),
                    SoldToCustAddressLine2 = table.Column<string>(type: "TEXT", nullable: false),
                    SoldToCustAddressLine3 = table.Column<string>(type: "TEXT", nullable: false),
                    SoldToCity = table.Column<string>(type: "TEXT", nullable: false),
                    SoldToSt = table.Column<string>(type: "TEXT", nullable: false),
                    SoldToZipCode = table.Column<string>(type: "TEXT", nullable: false),
                    ShipToName = table.Column<string>(type: "TEXT", nullable: false),
                    ShipToCustAddressLine1 = table.Column<string>(type: "TEXT", nullable: false),
                    ShipToCustAddressLine2 = table.Column<string>(type: "TEXT", nullable: false),
                    ShipToCustAddressLine3 = table.Column<string>(type: "TEXT", nullable: false),
                    ShipToCity = table.Column<string>(type: "TEXT", nullable: false),
                    ShipToSt = table.Column<string>(type: "TEXT", nullable: false),
                    ShipToZipCode = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerPONumber = table.Column<string>(type: "TEXT", nullable: false),
                    DueDate = table.Column<string>(type: "TEXT", nullable: false),
                    SalesPerson = table.Column<string>(type: "TEXT", nullable: false),
                    CarrierName = table.Column<string>(type: "TEXT", nullable: false),
                    ShippingInstructions = table.Column<string>(type: "TEXT", nullable: true),
                    FreightTerms = table.Column<string>(type: "TEXT", nullable: false),
                    ReportModelId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportHeaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportHeaders_ReportModels_ReportModelId",
                        column: x => x.ReportModelId,
                        principalTable: "ReportModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineItemDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModelItem = table.Column<decimal>(type: "TEXT", nullable: false),
                    NoteSequenceNumber = table.Column<decimal>(type: "TEXT", nullable: false),
                    NoteText = table.Column<string>(type: "TEXT", nullable: true),
                    PackingListFlag = table.Column<string>(type: "TEXT", nullable: true),
                    BolFlag = table.Column<string>(type: "TEXT", nullable: true),
                    LineItemId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineItemDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineItemDetails_LineItems_LineItemId",
                        column: x => x.LineItemId,
                        principalTable: "LineItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineItemPackingUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CartonOrSkid = table.Column<string>(type: "TEXT", nullable: true),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeOfUnit = table.Column<string>(type: "TEXT", nullable: true),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    LineItemId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineItemPackingUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineItemPackingUnits_LineItems_LineItemId",
                        column: x => x.LineItemId,
                        principalTable: "LineItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LineItemDetails_LineItemId",
                table: "LineItemDetails",
                column: "LineItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LineItemPackingUnits_LineItemId",
                table: "LineItemPackingUnits",
                column: "LineItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LineItems_LineItemHeaderId",
                table: "LineItems",
                column: "LineItemHeaderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineItems_ReportModelId",
                table: "LineItems",
                column: "ReportModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportHeaders_ReportModelId",
                table: "ReportHeaders",
                column: "ReportModelId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineItemDetails");

            migrationBuilder.DropTable(
                name: "LineItemPackingUnits");

            migrationBuilder.DropTable(
                name: "ReportHeaders");

            migrationBuilder.DropTable(
                name: "LineItems");

            migrationBuilder.DropTable(
                name: "LineItemHeaders");

            migrationBuilder.DropTable(
                name: "ReportModels");
        }
    }
}
