using Dotnetstore.PersonalApp.Modules.Sales.Features.CountryRegionCurrencies;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Currencies;
using Dotnetstore.PersonalApp.Modules.Sales.Features.CurrencyRates;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderReasons;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPersonQuotaHistories;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesReasons;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTaxRates;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritoryHistories;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOfferProducts;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOffers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class SalesHandlerTests : IDisposable
{
    private readonly SalesFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task SalesTerritoryCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeSalesTerritoryAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateSalesTerritory.Handler>()
            .HandleAsync(new UpdateSalesTerritory.Command(created.Id, created.Name, created.CountryRegionCode, created.RegionGroup, created.SalesYtd, created.SalesLastYear, created.CostYtd, created.CostLastYear), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListSalesTerritories.Handler>()
            .HandleAsync(new ListSalesTerritories.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteSalesTerritory.Handler>()
            .HandleAsync(new DeleteSalesTerritory.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteSalesTerritory.Handler>()
            .HandleAsync(new DeleteSalesTerritory.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("sales_territory_not_found");
    }

    [Fact]
    public async Task SalesTerritoryRejectsAMissingName()
    {
        var command = await _fixture.SalesTerritoryCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateSalesTerritory.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("sales_territory_name_required");
    }

    [Fact]
    public async Task SalesTerritoryRejectsADuplicate()
    {
        var command = await _fixture.SalesTerritoryCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateSalesTerritory.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("sales_territory_conflict");
    }

    [Fact]
    public async Task SalesTerritoryInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeSalesTerritoryAsync(Ct);
        var childCommand = await _fixture.SalesPersonCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateSalesPerson.Handler>()
            .HandleAsync(childCommand with { TerritoryId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteSalesTerritory.Handler>()
            .HandleAsync(new DeleteSalesTerritory.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("sales_territory_in_use");
    }

    [Fact]
    public async Task SalesPersonCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeSalesPersonAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateSalesPerson.Handler>()
            .HandleAsync(new UpdateSalesPerson.Command(created.Id, created.Name, created.UserId, created.TerritoryId, created.SalesQuota, created.Bonus, created.CommissionPct, created.SalesYtd, created.SalesLastYear), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListSalesPeople.Handler>()
            .HandleAsync(new ListSalesPeople.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteSalesPerson.Handler>()
            .HandleAsync(new DeleteSalesPerson.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteSalesPerson.Handler>()
            .HandleAsync(new DeleteSalesPerson.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("sales_person_not_found");
    }

    [Fact]
    public async Task SalesPersonRejectsAMissingName()
    {
        var command = await _fixture.SalesPersonCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateSalesPerson.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("sales_person_name_required");
    }

    [Fact]
    public async Task SalesPersonRejectsADuplicate()
    {
        var command = await _fixture.SalesPersonCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateSalesPerson.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("sales_person_conflict");
    }

    [Fact]
    public async Task SalesPersonInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeSalesPersonAsync(Ct);
        var childCommand = await _fixture.StoreCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateStore.Handler>()
            .HandleAsync(childCommand with { SalesPersonId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteSalesPerson.Handler>()
            .HandleAsync(new DeleteSalesPerson.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("sales_person_in_use");
    }

    [Fact]
    public async Task SalesPersonQuotaHistoryCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeSalesPersonQuotaHistoryAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateSalesPersonQuotaHistory.Handler>()
            .HandleAsync(new UpdateSalesPersonQuotaHistory.Command(created.Id, created.SalesPersonId, created.QuotaDate, created.SalesQuota), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListSalesPersonQuotaHistories.Handler>()
            .HandleAsync(new ListSalesPersonQuotaHistories.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteSalesPersonQuotaHistory.Handler>()
            .HandleAsync(new DeleteSalesPersonQuotaHistory.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteSalesPersonQuotaHistory.Handler>()
            .HandleAsync(new DeleteSalesPersonQuotaHistory.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("sales_person_quota_history_not_found");
    }

    [Fact]
    public async Task SalesPersonQuotaHistoryRejectsADuplicate()
    {
        var command = await _fixture.SalesPersonQuotaHistoryCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateSalesPersonQuotaHistory.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("sales_person_quota_history_conflict");
    }

    [Fact]
    public async Task SalesTerritoryHistoryCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeSalesTerritoryHistoryAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateSalesTerritoryHistory.Handler>()
            .HandleAsync(new UpdateSalesTerritoryHistory.Command(created.Id, created.SalesPersonId, created.TerritoryId, created.StartDate, created.EndDate), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListSalesTerritoryHistories.Handler>()
            .HandleAsync(new ListSalesTerritoryHistories.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteSalesTerritoryHistory.Handler>()
            .HandleAsync(new DeleteSalesTerritoryHistory.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteSalesTerritoryHistory.Handler>()
            .HandleAsync(new DeleteSalesTerritoryHistory.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("sales_territory_history_not_found");
    }

    [Fact]
    public async Task SalesTaxRateCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeSalesTaxRateAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateSalesTaxRate.Handler>()
            .HandleAsync(new UpdateSalesTaxRate.Command(created.Id, created.Name, created.StateProvince, created.TaxType, created.TaxRate), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListSalesTaxRates.Handler>()
            .HandleAsync(new ListSalesTaxRates.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteSalesTaxRate.Handler>()
            .HandleAsync(new DeleteSalesTaxRate.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteSalesTaxRate.Handler>()
            .HandleAsync(new DeleteSalesTaxRate.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("sales_tax_rate_not_found");
    }

    [Fact]
    public async Task SalesTaxRateRejectsAMissingName()
    {
        var command = await _fixture.SalesTaxRateCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateSalesTaxRate.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("sales_tax_rate_name_required");
    }

    [Fact]
    public async Task SalesTaxRateRejectsADuplicate()
    {
        var command = await _fixture.SalesTaxRateCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateSalesTaxRate.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("sales_tax_rate_conflict");
    }

    [Fact]
    public async Task CurrencyCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeCurrencyAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateCurrency.Handler>()
            .HandleAsync(new UpdateCurrency.Command(created.Id, created.Code, created.Name), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListCurrencies.Handler>()
            .HandleAsync(new ListCurrencies.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteCurrency.Handler>()
            .HandleAsync(new DeleteCurrency.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteCurrency.Handler>()
            .HandleAsync(new DeleteCurrency.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("currency_not_found");
    }

    [Fact]
    public async Task CurrencyRejectsAMissingCode()
    {
        var command = await _fixture.CurrencyCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateCurrency.Handler>()
            .HandleAsync(command with { Code = "  " }, Ct);

        result.Error.Code.ShouldBe("currency_code_required");
    }

    [Fact]
    public async Task CurrencyRejectsADuplicate()
    {
        var command = await _fixture.CurrencyCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateCurrency.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("currency_conflict");
    }

    [Fact]
    public async Task CurrencyInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeCurrencyAsync(Ct);
        var childCommand = await _fixture.CurrencyRateCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateCurrencyRate.Handler>()
            .HandleAsync(childCommand with { FromCurrencyId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteCurrency.Handler>()
            .HandleAsync(new DeleteCurrency.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("currency_in_use");
    }

    [Fact]
    public async Task CurrencyRateCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeCurrencyRateAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateCurrencyRate.Handler>()
            .HandleAsync(new UpdateCurrencyRate.Command(created.Id, created.RateDate, created.FromCurrencyId, created.ToCurrencyId, created.AverageRate, created.EndOfDayRate), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListCurrencyRates.Handler>()
            .HandleAsync(new ListCurrencyRates.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteCurrencyRate.Handler>()
            .HandleAsync(new DeleteCurrencyRate.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteCurrencyRate.Handler>()
            .HandleAsync(new DeleteCurrencyRate.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("currency_rate_not_found");
    }

    [Fact]
    public async Task CurrencyRateRejectsADuplicate()
    {
        var command = await _fixture.CurrencyRateCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateCurrencyRate.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("currency_rate_conflict");
    }

    [Fact]
    public async Task CountryRegionCurrencyCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeCountryRegionCurrencyAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateCountryRegionCurrency.Handler>()
            .HandleAsync(new UpdateCountryRegionCurrency.Command(created.Id, created.CountryRegion, created.CurrencyId), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListCountryRegionCurrencies.Handler>()
            .HandleAsync(new ListCountryRegionCurrencies.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteCountryRegionCurrency.Handler>()
            .HandleAsync(new DeleteCountryRegionCurrency.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteCountryRegionCurrency.Handler>()
            .HandleAsync(new DeleteCountryRegionCurrency.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("country_region_currency_not_found");
    }

    [Fact]
    public async Task CountryRegionCurrencyRejectsAMissingCountryRegion()
    {
        var command = await _fixture.CountryRegionCurrencyCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateCountryRegionCurrency.Handler>()
            .HandleAsync(command with { CountryRegion = "  " }, Ct);

        result.Error.Code.ShouldBe("country_region_currency_country_region_required");
    }

    [Fact]
    public async Task CountryRegionCurrencyRejectsADuplicate()
    {
        var command = await _fixture.CountryRegionCurrencyCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateCountryRegionCurrency.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("country_region_currency_conflict");
    }

    [Fact]
    public async Task SalesReasonCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeSalesReasonAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateSalesReason.Handler>()
            .HandleAsync(new UpdateSalesReason.Command(created.Id, created.Name, created.ReasonType), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListSalesReasons.Handler>()
            .HandleAsync(new ListSalesReasons.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteSalesReason.Handler>()
            .HandleAsync(new DeleteSalesReason.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteSalesReason.Handler>()
            .HandleAsync(new DeleteSalesReason.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("sales_reason_not_found");
    }

    [Fact]
    public async Task SalesReasonRejectsAMissingName()
    {
        var command = await _fixture.SalesReasonCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateSalesReason.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("sales_reason_name_required");
    }

    [Fact]
    public async Task SalesReasonRejectsADuplicate()
    {
        var command = await _fixture.SalesReasonCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateSalesReason.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("sales_reason_conflict");
    }

    [Fact]
    public async Task SalesReasonInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeSalesReasonAsync(Ct);
        var childCommand = await _fixture.SalesOrderReasonCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateSalesOrderReason.Handler>()
            .HandleAsync(childCommand with { SalesReasonId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteSalesReason.Handler>()
            .HandleAsync(new DeleteSalesReason.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("sales_reason_in_use");
    }

    [Fact]
    public async Task SpecialOfferCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeSpecialOfferAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateSpecialOffer.Handler>()
            .HandleAsync(new UpdateSpecialOffer.Command(created.Id, created.Description, created.DiscountPct, created.OfferType, created.Category, created.StartDate, created.EndDate, created.MinQty, created.MaxQty), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListSpecialOffers.Handler>()
            .HandleAsync(new ListSpecialOffers.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteSpecialOffer.Handler>()
            .HandleAsync(new DeleteSpecialOffer.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteSpecialOffer.Handler>()
            .HandleAsync(new DeleteSpecialOffer.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("special_offer_not_found");
    }

    [Fact]
    public async Task SpecialOfferRejectsAMissingDescription()
    {
        var command = await _fixture.SpecialOfferCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateSpecialOffer.Handler>()
            .HandleAsync(command with { Description = "  " }, Ct);

        result.Error.Code.ShouldBe("special_offer_description_required");
    }

    [Fact]
    public async Task SpecialOfferRejectsADuplicate()
    {
        var command = await _fixture.SpecialOfferCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateSpecialOffer.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("special_offer_conflict");
    }

    [Fact]
    public async Task SpecialOfferInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeSpecialOfferAsync(Ct);
        var childCommand = await _fixture.SalesOrderDetailCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateSalesOrderDetail.Handler>()
            .HandleAsync(childCommand with { SpecialOfferId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteSpecialOffer.Handler>()
            .HandleAsync(new DeleteSpecialOffer.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("special_offer_in_use");
    }

    [Fact]
    public async Task SpecialOfferProductCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeSpecialOfferProductAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateSpecialOfferProduct.Handler>()
            .HandleAsync(new UpdateSpecialOfferProduct.Command(created.Id, created.SpecialOfferId, created.ProductId), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListSpecialOfferProducts.Handler>()
            .HandleAsync(new ListSpecialOfferProducts.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteSpecialOfferProduct.Handler>()
            .HandleAsync(new DeleteSpecialOfferProduct.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteSpecialOfferProduct.Handler>()
            .HandleAsync(new DeleteSpecialOfferProduct.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("special_offer_product_not_found");
    }

    [Fact]
    public async Task SpecialOfferProductRejectsADuplicate()
    {
        var command = await _fixture.SpecialOfferProductCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateSpecialOfferProduct.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("special_offer_product_conflict");
    }

    [Fact]
    public async Task StoreCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeStoreAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateStore.Handler>()
            .HandleAsync(new UpdateStore.Command(created.Id, created.Name, created.SalesPersonId), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListStores.Handler>()
            .HandleAsync(new ListStores.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteStore.Handler>()
            .HandleAsync(new DeleteStore.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteStore.Handler>()
            .HandleAsync(new DeleteStore.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("store_not_found");
    }

    [Fact]
    public async Task StoreRejectsAMissingName()
    {
        var command = await _fixture.StoreCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateStore.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("store_name_required");
    }

    [Fact]
    public async Task StoreRejectsADuplicate()
    {
        var command = await _fixture.StoreCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateStore.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("store_conflict");
    }

    [Fact]
    public async Task StoreInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeStoreAsync(Ct);
        var childCommand = await _fixture.CustomerCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateCustomer.Handler>()
            .HandleAsync(childCommand with { StoreId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteStore.Handler>()
            .HandleAsync(new DeleteStore.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("store_in_use");
    }

    [Fact]
    public async Task CustomerCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeCustomerAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateCustomer.Handler>()
            .HandleAsync(new UpdateCustomer.Command(created.Id, created.Number, created.Name, created.ClientId, created.StoreId, created.TerritoryId), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListCustomers.Handler>()
            .HandleAsync(new ListCustomers.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteCustomer.Handler>()
            .HandleAsync(new DeleteCustomer.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteCustomer.Handler>()
            .HandleAsync(new DeleteCustomer.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("customer_not_found");
    }

    [Fact]
    public async Task CustomerRejectsAMissingName()
    {
        var command = await _fixture.CustomerCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateCustomer.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("customer_name_required");
    }

    [Fact]
    public async Task CustomerRejectsADuplicate()
    {
        var command = await _fixture.CustomerCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateCustomer.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("customer_conflict");
    }

    [Fact]
    public async Task CustomerInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeCustomerAsync(Ct);
        var childCommand = await _fixture.SalesOrderCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateSalesOrder.Handler>()
            .HandleAsync(childCommand with { CustomerId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteCustomer.Handler>()
            .HandleAsync(new DeleteCustomer.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("customer_in_use");
    }

    [Fact]
    public async Task SalesOrderCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeSalesOrderAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateSalesOrder.Handler>()
            .HandleAsync(new UpdateSalesOrder.Command(created.Id, created.Number, created.Status, created.OnlineOrderFlag, created.OrderDate, created.DueDate, created.ShipDate, created.PurchaseOrderNumber, created.CustomerId, created.SalesPersonId, created.TerritoryId, created.ShipMethodId, created.TaxAmt, created.Freight, created.Comment), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListSalesOrders.Handler>()
            .HandleAsync(new ListSalesOrders.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteSalesOrder.Handler>()
            .HandleAsync(new DeleteSalesOrder.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteSalesOrder.Handler>()
            .HandleAsync(new DeleteSalesOrder.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("sales_order_not_found");
    }

    [Fact]
    public async Task SalesOrderRejectsADuplicate()
    {
        var command = await _fixture.SalesOrderCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateSalesOrder.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("sales_order_conflict");
    }

    [Fact]
    public async Task SalesOrderDetailCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeSalesOrderDetailAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateSalesOrderDetail.Handler>()
            .HandleAsync(new UpdateSalesOrderDetail.Command(created.Id, created.SalesOrderId, created.ProductId, created.CarrierTrackingNumber, created.OrderQty, created.SpecialOfferId, created.UnitPrice, created.UnitPriceDiscount), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListSalesOrderDetails.Handler>()
            .HandleAsync(new ListSalesOrderDetails.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteSalesOrderDetail.Handler>()
            .HandleAsync(new DeleteSalesOrderDetail.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteSalesOrderDetail.Handler>()
            .HandleAsync(new DeleteSalesOrderDetail.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("sales_order_detail_not_found");
    }

    [Fact]
    public async Task SalesOrderReasonCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeSalesOrderReasonAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateSalesOrderReason.Handler>()
            .HandleAsync(new UpdateSalesOrderReason.Command(created.Id, created.SalesOrderId, created.SalesReasonId), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListSalesOrderReasons.Handler>()
            .HandleAsync(new ListSalesOrderReasons.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteSalesOrderReason.Handler>()
            .HandleAsync(new DeleteSalesOrderReason.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteSalesOrderReason.Handler>()
            .HandleAsync(new DeleteSalesOrderReason.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("sales_order_reason_not_found");
    }

    [Fact]
    public async Task SalesOrderReasonRejectsADuplicate()
    {
        var command = await _fixture.SalesOrderReasonCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateSalesOrderReason.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("sales_order_reason_conflict");
    }
}