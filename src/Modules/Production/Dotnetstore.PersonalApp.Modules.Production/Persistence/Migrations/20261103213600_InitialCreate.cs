using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    CostRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    Availability = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    Description = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    Instructions = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScrapReasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapReasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitMeasures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, collation: "NOCASE"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitMeasures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductSubcategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSubcategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSubcategories_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    ProductNumber = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false, collation: "NOCASE"),
                    MakeFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    FinishedGoodsFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    SafetyStockLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ReorderPoint = table.Column<int>(type: "INTEGER", nullable: false),
                    StandardCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    ListPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Size = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    SizeUnitId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Weight = table.Column<decimal>(type: "TEXT", nullable: true),
                    WeightUnitId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DaysToManufacture = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductLine = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Class = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Style = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SubcategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SellStartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SellEndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DiscontinuedDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ProductModels_ModelId",
                        column: x => x.ModelId,
                        principalTable: "ProductModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_ProductSubcategories_SubcategoryId",
                        column: x => x.SubcategoryId,
                        principalTable: "ProductSubcategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_UnitMeasures_SizeUnitId",
                        column: x => x.SizeUnitId,
                        principalTable: "UnitMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_UnitMeasures_WeightUnitId",
                        column: x => x.WeightUnitId,
                        principalTable: "UnitMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillOfMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductAssemblyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ComponentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    UnitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BomLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    PerAssemblyQty = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillOfMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_Products_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_Products_ProductAssemblyId",
                        column: x => x.ProductAssemblyId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_UnitMeasures_UnitId",
                        column: x => x.UnitId,
                        principalTable: "UnitMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductCostHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    StandardCost = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCostHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCostHistories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Shelf = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Bin = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductInventories_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductInventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductListPriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ListPrice = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductListPriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductListPriceHistories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReviewerName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReviewDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Comments = table.Column<string>(type: "TEXT", maxLength: 3850, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReviews_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReferenceOrder = table.Column<string>(type: "TEXT", maxLength: 25, nullable: true),
                    TransactionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualCost = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionHistories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false, collation: "NOCASE"),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderQty = table.Column<int>(type: "INTEGER", nullable: false),
                    ScrappedQty = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ScrapReasonId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_ScrapReasons_ScrapReasonId",
                        column: x => x.ScrapReasonId,
                        principalTable: "ScrapReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderRoutings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OperationSequence = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScheduledStartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ScheduledEndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ActualStartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ActualEndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ActualResourceHrs = table.Column<decimal>(type: "TEXT", nullable: true),
                    PlannedCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    ActualCost = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderRoutings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderRoutings_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderRoutings_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "Availability", "CostRate", "Name" },
                values: new object[,]
                {
                    { new Guid("696e197e-1eaa-399e-211e-f36f7a93e7a7"), 0m, 0m, "Paint Shop" },
                    { new Guid("7e19bdc3-5285-acdf-2a98-b9de07ddf418"), 0m, 0m, "Sheet Metal Racks" },
                    { new Guid("d4dda2c5-f2e8-f13b-2e99-dfc3124de1a3"), 120m, 12.25m, "Final Assembly" },
                    { new Guid("e8e47b31-08d5-6dc6-5689-80a56c60c750"), 0m, 0m, "Tool Crib" }
                });

            migrationBuilder.InsertData(
                table: "ScrapReasons",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("3faf40d0-3956-936d-bf06-2bd56bb1bed9"), "Gouge in metal" },
                    { new Guid("4380c3dc-ad99-a4e2-30a3-2cf4005b6015"), "Drill size too small" },
                    { new Guid("a111462e-3b4b-417f-caf6-d6d8e66f2ef1"), "Drill size too large" },
                    { new Guid("b189ff67-9dd6-4764-9fe4-645916b3dcba"), "Wrong size" },
                    { new Guid("d5bf1b19-533f-aefd-3bd7-bed272e65259"), "Paint process failed" }
                });

            migrationBuilder.InsertData(
                table: "UnitMeasures",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { new Guid("220fff24-c7ac-1f8e-a8f4-778bc719fc2f"), "CM", "Centimeter" },
                    { new Guid("45a20254-bf77-cd80-1242-3e9bd5b52be8"), "KG", "Kilogram" },
                    { new Guid("5904109a-6c4b-19bf-8b3a-03fcc443edcf"), "L", "Liter" },
                    { new Guid("6154457f-6237-dcea-fc73-82b5889974a1"), "PK", "Package" },
                    { new Guid("6d49e26d-53bb-ec2b-753a-60351414f54b"), "M", "Meter" },
                    { new Guid("bc9464ee-0014-8262-8b17-441da4501275"), "G", "Gram" },
                    { new Guid("c427806f-49e5-00e2-3f68-246f245f95d8"), "EA", "Each" },
                    { new Guid("f82bd140-eb35-9024-0a4d-b28f65f26491"), "HR", "Hour" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_ComponentId",
                table: "BillOfMaterials",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_ProductAssemblyId",
                table: "BillOfMaterials",
                column: "ProductAssemblyId");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_UnitId",
                table: "BillOfMaterials",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Name",
                table: "Locations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Name",
                table: "ProductCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCostHistories_ProductId",
                table: "ProductCostHistories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductInventories_LocationId",
                table: "ProductInventories",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductInventories_ProductId_LocationId",
                table: "ProductInventories",
                columns: new[] { "ProductId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductListPriceHistories_ProductId",
                table: "ProductListPriceHistories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductModels_Name",
                table: "ProductModels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_ProductId",
                table: "ProductReviews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ModelId",
                table: "Products",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductNumber",
                table: "Products",
                column: "ProductNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SizeUnitId",
                table: "Products",
                column: "SizeUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SubcategoryId",
                table: "Products",
                column: "SubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_WeightUnitId",
                table: "Products",
                column: "WeightUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubcategories_CategoryId_Name",
                table: "ProductSubcategories",
                columns: new[] { "CategoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScrapReasons_Name",
                table: "ScrapReasons",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionHistories_ProductId",
                table: "TransactionHistories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitMeasures_Code",
                table: "UnitMeasures",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitMeasures_Name",
                table: "UnitMeasures",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderRoutings_LocationId",
                table: "WorkOrderRoutings",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderRoutings_WorkOrderId_OperationSequence",
                table: "WorkOrderRoutings",
                columns: new[] { "WorkOrderId", "OperationSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_Number",
                table: "WorkOrders",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ProductId",
                table: "WorkOrders",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ScrapReasonId",
                table: "WorkOrders",
                column: "ScrapReasonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillOfMaterials");

            migrationBuilder.DropTable(
                name: "ProductCostHistories");

            migrationBuilder.DropTable(
                name: "ProductInventories");

            migrationBuilder.DropTable(
                name: "ProductListPriceHistories");

            migrationBuilder.DropTable(
                name: "ProductReviews");

            migrationBuilder.DropTable(
                name: "TransactionHistories");

            migrationBuilder.DropTable(
                name: "WorkOrderRoutings");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ScrapReasons");

            migrationBuilder.DropTable(
                name: "ProductModels");

            migrationBuilder.DropTable(
                name: "ProductSubcategories");

            migrationBuilder.DropTable(
                name: "UnitMeasures");

            migrationBuilder.DropTable(
                name: "ProductCategories");
        }
    }
}
