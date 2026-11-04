using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShipMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    ShipBase = table.Column<decimal>(type: "TEXT", nullable: false),
                    ShipRate = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false, collation: "NOCASE"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    CreditRating = table.Column<int>(type: "INTEGER", nullable: false),
                    PreferredVendorStatus = table.Column<bool>(type: "INTEGER", nullable: false),
                    ActiveFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    PurchasingWebServiceUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductVendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AverageLeadTime = table.Column<int>(type: "INTEGER", nullable: false),
                    StandardPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    LastReceiptCost = table.Column<decimal>(type: "TEXT", nullable: true),
                    LastReceiptDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    MinOrderQty = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxOrderQty = table.Column<int>(type: "INTEGER", nullable: false),
                    OnOrderQty = table.Column<int>(type: "INTEGER", nullable: true),
                    UnitId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVendors_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false, collation: "NOCASE"),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BuyerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShipMethodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ShipDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    TaxAmt = table.Column<decimal>(type: "TEXT", nullable: false),
                    Freight = table.Column<decimal>(type: "TEXT", nullable: false),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalDue = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_ShipMethods_ShipMethodId",
                        column: x => x.ShipMethodId,
                        principalTable: "ShipMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    OrderQty = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReceivedQty = table.Column<decimal>(type: "TEXT", nullable: false),
                    RejectedQty = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    StockedQty = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetails_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ShipMethods",
                columns: new[] { "Id", "Name", "ShipBase", "ShipRate" },
                values: new object[,]
                {
                    { new Guid("949cd9ad-9003-67be-5adf-d4e688d54c28"), "Overnight", 21.95m, 2.99m },
                    { new Guid("b789b452-1410-2d31-347f-d5704c573e86"), "Standard", 3.95m, 0.99m },
                    { new Guid("d1547001-fd74-cce0-9c7c-3fb456bb945f"), "Express", 9.95m, 1.99m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVendors_ProductId_VendorId",
                table: "ProductVendors",
                columns: new[] { "ProductId", "VendorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVendors_VendorId",
                table: "ProductVendors",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_PurchaseOrderId",
                table: "PurchaseOrderDetails",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Number",
                table: "PurchaseOrders",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ShipMethodId",
                table: "PurchaseOrders",
                column: "ShipMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_VendorId",
                table: "PurchaseOrders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipMethods_Name",
                table: "ShipMethods",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_AccountNumber",
                table: "Vendors",
                column: "AccountNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Name",
                table: "Vendors",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVendors");

            migrationBuilder.DropTable(
                name: "PurchaseOrderDetails");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "ShipMethods");

            migrationBuilder.DropTable(
                name: "Vendors");
        }
    }
}
