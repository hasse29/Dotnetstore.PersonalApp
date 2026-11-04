using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, collation: "NOCASE"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesReasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    ReasonType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesTaxRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    StateProvince = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TaxType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TaxRate = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesTaxRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesTerritories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    CountryRegionCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    RegionGroup = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SalesYtd = table.Column<decimal>(type: "TEXT", nullable: false),
                    SalesLastYear = table.Column<decimal>(type: "TEXT", nullable: false),
                    CostYtd = table.Column<decimal>(type: "TEXT", nullable: false),
                    CostLastYear = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesTerritories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecialOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false, collation: "NOCASE"),
                    DiscountPct = table.Column<decimal>(type: "TEXT", nullable: false),
                    OfferType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MinQty = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxQty = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CountryRegionCurrencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CountryRegion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryRegionCurrencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CountryRegionCurrencies_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RateDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    FromCurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToCurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AverageRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    EndOfDayRate = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyRates_Currencies_FromCurrencyId",
                        column: x => x.FromCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyRates_Currencies_ToCurrencyId",
                        column: x => x.ToCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesPeople",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TerritoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SalesQuota = table.Column<decimal>(type: "TEXT", nullable: true),
                    Bonus = table.Column<decimal>(type: "TEXT", nullable: false),
                    CommissionPct = table.Column<decimal>(type: "TEXT", nullable: false),
                    SalesYtd = table.Column<decimal>(type: "TEXT", nullable: false),
                    SalesLastYear = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesPeople", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesPeople_SalesTerritories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "SalesTerritories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpecialOfferProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SpecialOfferId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialOfferProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialOfferProducts_SpecialOffers_SpecialOfferId",
                        column: x => x.SpecialOfferId,
                        principalTable: "SpecialOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesPersonQuotaHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SalesPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuotaDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SalesQuota = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesPersonQuotaHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesPersonQuotaHistories_SalesPeople_SalesPersonId",
                        column: x => x.SalesPersonId,
                        principalTable: "SalesPeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesTerritoryHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SalesPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TerritoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesTerritoryHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesTerritoryHistories_SalesPeople_SalesPersonId",
                        column: x => x.SalesPersonId,
                        principalTable: "SalesPeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesTerritoryHistories_SalesTerritories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "SalesTerritories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    SalesPersonId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stores_SalesPeople_SalesPersonId",
                        column: x => x.SalesPersonId,
                        principalTable: "SalesPeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false, collation: "NOCASE"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StoreId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TerritoryId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_SalesTerritories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "SalesTerritories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customers_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false, collation: "NOCASE"),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OnlineOrderFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ShipDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PurchaseOrderNumber = table.Column<string>(type: "TEXT", maxLength: 25, nullable: true),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SalesPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TerritoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ShipMethodId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TaxAmt = table.Column<decimal>(type: "TEXT", nullable: false),
                    Freight = table.Column<decimal>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalDue = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_SalesPeople_SalesPersonId",
                        column: x => x.SalesPersonId,
                        principalTable: "SalesPeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_SalesTerritories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "SalesTerritories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CarrierTrackingNumber = table.Column<string>(type: "TEXT", maxLength: 25, nullable: true),
                    OrderQty = table.Column<int>(type: "INTEGER", nullable: false),
                    SpecialOfferId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPriceDiscount = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderDetails_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrderDetails_SpecialOffers_SpecialOfferId",
                        column: x => x.SpecialOfferId,
                        principalTable: "SpecialOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderReasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SalesReasonId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderReasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderReasons_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrderReasons_SalesReasons_SalesReasonId",
                        column: x => x.SalesReasonId,
                        principalTable: "SalesReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { new Guid("1918ecc6-44b5-84af-8139-697d8473a3e9"), "SEK", "Swedish krona" },
                    { new Guid("374bdb9e-e8ab-d0ea-41e2-8469a7b8248a"), "DKK", "Danish krone" },
                    { new Guid("4cd461f3-c5cf-9f69-b7c5-5ae24f4eeebf"), "USD", "US dollar" },
                    { new Guid("5a118829-8c41-357a-1f3a-450d7db1a6f5"), "EUR", "Euro" },
                    { new Guid("6cf2c02d-1039-f9f0-3bca-cb3b98ac2847"), "CNY", "Yuan renminbi" },
                    { new Guid("d00b4a10-f7f6-e190-ec4e-96ce04efaf52"), "NOK", "Norwegian krone" },
                    { new Guid("e9f6ae11-d9c8-0341-05da-7aed24c3cc84"), "GBP", "Pound sterling" }
                });

            migrationBuilder.InsertData(
                table: "SalesReasons",
                columns: new[] { "Id", "Name", "ReasonType" },
                values: new object[,]
                {
                    { new Guid("3523bb57-6f80-2491-bc3a-be499d9b56ac"), "Price", "O" },
                    { new Guid("401c4639-8946-3b6e-2b8a-c36946b8ed0a"), "Magazine advertisement", "M" },
                    { new Guid("6105de8d-013e-972b-543f-dfeb45fe6334"), "Quality", "O" },
                    { new Guid("8435cc86-4e64-0c58-5596-37f4c5ff64f0"), "Review", "O" },
                    { new Guid("962c1314-03d2-7f46-6ce2-0f8efd152dca"), "Other", "O" },
                    { new Guid("d4f1595c-c150-a1aa-37f1-65ac410700bd"), "On promotion", "P" },
                    { new Guid("d96f4fa3-020f-b872-fd91-2af0b37bc2ce"), "Television advertisement", "M" }
                });

            migrationBuilder.InsertData(
                table: "SpecialOffers",
                columns: new[] { "Id", "Category", "Description", "DiscountPct", "EndDate", "MaxQty", "MinQty", "OfferType", "StartDate" },
                values: new object[] { new Guid("55d06aaf-8cfb-eb50-cfa0-ed338f3b879a"), "No discount", "No discount", 0m, new DateOnly(2099, 12, 31), null, 0, "No discount", new DateOnly(2020, 1, 1) });

            migrationBuilder.CreateIndex(
                name: "IX_CountryRegionCurrencies_CountryRegion_CurrencyId",
                table: "CountryRegionCurrencies",
                columns: new[] { "CountryRegion", "CurrencyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CountryRegionCurrencies_CurrencyId",
                table: "CountryRegionCurrencies",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Name",
                table: "Currencies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_FromCurrencyId",
                table: "CurrencyRates",
                column: "FromCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_RateDate_FromCurrencyId_ToCurrencyId",
                table: "CurrencyRates",
                columns: new[] { "RateDate", "FromCurrencyId", "ToCurrencyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_ToCurrencyId",
                table: "CurrencyRates",
                column: "ToCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Number",
                table: "Customers",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_StoreId",
                table: "Customers",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TerritoryId",
                table: "Customers",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderDetails_SalesOrderId",
                table: "SalesOrderDetails",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderDetails_SpecialOfferId",
                table: "SalesOrderDetails",
                column: "SpecialOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderReasons_SalesOrderId_SalesReasonId",
                table: "SalesOrderReasons",
                columns: new[] { "SalesOrderId", "SalesReasonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderReasons_SalesReasonId",
                table: "SalesOrderReasons",
                column: "SalesReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId",
                table: "SalesOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Number",
                table: "SalesOrders",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalesPersonId",
                table: "SalesOrders",
                column: "SalesPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TerritoryId",
                table: "SalesOrders",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesPeople_Name",
                table: "SalesPeople",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesPeople_TerritoryId",
                table: "SalesPeople",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesPersonQuotaHistories_SalesPersonId_QuotaDate",
                table: "SalesPersonQuotaHistories",
                columns: new[] { "SalesPersonId", "QuotaDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReasons_Name",
                table: "SalesReasons",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesTaxRates_Name",
                table: "SalesTaxRates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesTerritories_Name",
                table: "SalesTerritories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesTerritoryHistories_SalesPersonId",
                table: "SalesTerritoryHistories",
                column: "SalesPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTerritoryHistories_TerritoryId",
                table: "SalesTerritoryHistories",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialOfferProducts_SpecialOfferId_ProductId",
                table: "SpecialOfferProducts",
                columns: new[] { "SpecialOfferId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecialOffers_Description",
                table: "SpecialOffers",
                column: "Description",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stores_Name",
                table: "Stores",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stores_SalesPersonId",
                table: "Stores",
                column: "SalesPersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CountryRegionCurrencies");

            migrationBuilder.DropTable(
                name: "CurrencyRates");

            migrationBuilder.DropTable(
                name: "SalesOrderDetails");

            migrationBuilder.DropTable(
                name: "SalesOrderReasons");

            migrationBuilder.DropTable(
                name: "SalesPersonQuotaHistories");

            migrationBuilder.DropTable(
                name: "SalesTaxRates");

            migrationBuilder.DropTable(
                name: "SalesTerritoryHistories");

            migrationBuilder.DropTable(
                name: "SpecialOfferProducts");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "SalesReasons");

            migrationBuilder.DropTable(
                name: "SpecialOffers");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Stores");

            migrationBuilder.DropTable(
                name: "SalesPeople");

            migrationBuilder.DropTable(
                name: "SalesTerritories");
        }
    }
}
