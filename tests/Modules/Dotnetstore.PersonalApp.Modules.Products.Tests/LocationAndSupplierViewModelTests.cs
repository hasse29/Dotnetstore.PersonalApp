using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;
using Dotnetstore.PersonalApp.Modules.Products.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class LocationAndSupplierViewModelTests : IDisposable
{
    private readonly ProductsFixture _fixture = new();
    private readonly Localizer _localizer = new();

    public LocationAndSupplierViewModelTests()
    {
        _localizer.AddResources(ProductsStrings.ResourceManager);
        _localizer.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10, Ct);
        }

        condition().ShouldBeTrue();
    }

    private string Error(string code) => _localizer.Get("Error_" + code);

    // ---- storage locations ----

    private StorageLocationsViewModel Locations() =>
        new(
            new ListStorageLocations.Handler(_fixture.DbFactory),
            _fixture.CreateLocation,
            _fixture.UpdateLocation,
            _fixture.DeleteLocation,
            _localizer);

    private StorageLocationEditorViewModel LocationEditor(StorageLocationDto? existing = null) =>
        new(existing, _fixture.CreateLocation, _fixture.UpdateLocation, _localizer);

    private async Task<StorageLocationDto> AddLocationAsync(string name, bool active = true) =>
        (await _fixture.CreateLocation.HandleAsync(new CreateStorageLocation.Command(new StorageLocationFields(name, "Shelf", active)), Ct)).Value;

    [Fact]
    public async Task ALocationEditorIsEmptyWhenNewAndFilledWhenExisting()
    {
        var fresh = LocationEditor();
        fresh.Title.ShouldBe(_localizer.Get("StorageLocationEditor_New"));
        fresh.IsActive.ShouldBeTrue();

        var existing = LocationEditor(await AddLocationAsync("Warehouse A", active: false));
        existing.Title.ShouldBe(_localizer.Get("StorageLocationEditor_Edit"));
        existing.Name.ShouldBe("Warehouse A");
        existing.Description.ShouldBe("Shelf");
        existing.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task SavingNewAndExistingLocationsRaiseSaved()
    {
        var editor = LocationEditor();
        editor.Name = "Warehouse A";
        StorageLocationDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;
        await editor.SaveCommand.ExecuteAsync(null);
        var id = saved.ShouldNotBeNull().Id;

        var again = LocationEditor(saved);
        again.Name = "Warehouse B";
        again.IsActive = false;
        again.Saved += (_, dto) => saved = dto;
        await again.SaveCommand.ExecuteAsync(null);

        saved.Id.ShouldBe(id);
        saved.Name.ShouldBe("Warehouse B");
        saved.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task ABlankOrDuplicateLocationNameIsRejected()
    {
        await AddLocationAsync("Warehouse A");
        var editor = LocationEditor();
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("storage_location_name_required"));

        editor.Name = "Warehouse A";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("storage_location_conflict"));
        raised.ShouldBeFalse();

        editor.Name = "Warehouse C";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBeNull();
        raised.ShouldBeTrue();
    }

    [Fact]
    public async Task ALocationEditorCancelRaisesCancelled()
    {
        var editor = LocationEditor();
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task TheLocationListShowsActiveAndInactiveAndSearches()
    {
        await AddLocationAsync("Warehouse A");
        await AddLocationAsync("Old Cellar", active: false);
        var list = Locations();
        await list.LoadCommand.ExecuteAsync(null);
        var baseline = list.Locations.Count;

        list.Locations.Single(l => l.Location.Name == "Warehouse A").StatusText.ShouldBe(_localizer.Get("StorageLocations_Active"));
        list.Locations.Single(l => l.Location.Name == "Old Cellar").StatusText.ShouldBe(_localizer.Get("StorageLocations_Inactive"));
        list.StatusMessage.ShouldBe(_localizer.Format("StorageLocations_Count", baseline));

        list.Search = "Cellar";
        await list.LoadCommand.ExecuteAsync(null);
        list.Locations.Single().Location.Name.ShouldBe("Old Cellar");
    }

    [Fact]
    public async Task SelectingNewSavingAndCancellingTheLocationEditorWork()
    {
        await AddLocationAsync("Warehouse A");
        var list = Locations();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedLocation = list.Locations.Single(l => l.Location.Name == "Warehouse A");
        list.Editor.ShouldNotBeNull().Name.ShouldBe("Warehouse A");

        list.NewCommand.Execute(null);
        list.SelectedLocation.ShouldBeNull();
        list.Editor!.Name = "Warehouse Z";
        await list.Editor.SaveCommand.ExecuteAsync(null);
        await WaitUntilAsync(() => list.StatusMessage == _localizer.Format("StorageLocations_Saved", "Warehouse Z"));
        list.Editor.ShouldBeNull();
        list.Locations.ShouldContain(l => l.Location.Name == "Warehouse Z");

        list.NewCommand.Execute(null);
        list.Editor!.CancelCommand.Execute(null);
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task DeletingALocationNeedsConfirmationAndHandlesAMissingOne()
    {
        var gone = await AddLocationAsync("Gone");
        await AddLocationAsync("Warehouse A");
        var list = Locations();
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedLocation = list.Locations.Single(l => l.Location.Name == "Warehouse A");

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeFalse();

        list.SelectedLocation = list.Locations.Single(l => l.Location.Id == gone.Id);
        await _fixture.DeleteLocation.HandleAsync(new DeleteStorageLocation.Command(gone.Id), Ct);
        await list.ConfirmDeleteCommand.ExecuteAsync(null);
        list.StatusMessage.ShouldBe(Error("storage_location_not_found"));

        list.SelectedLocation = list.Locations.Single(l => l.Location.Name == "Warehouse A");
        await list.ConfirmDeleteCommand.ExecuteAsync(null);
        list.Locations.ShouldNotContain(l => l.Location.Name == "Warehouse A");
        list.Editor.ShouldBeNull();
    }

    // ---- suppliers ----

    private SuppliersViewModel Suppliers() =>
        new(_fixture.ListSuppliers, _fixture.CreateSupplier, _fixture.UpdateSupplier, _fixture.DeleteSupplier, _localizer);

    private SupplierEditorViewModel SupplierEditor(SupplierDto? existing = null) =>
        new(existing, _fixture.CreateSupplier, _fixture.UpdateSupplier, _localizer);

    private async Task<SupplierDto> AddSupplierAsync(string name, bool active = true) =>
        (await _fixture.CreateSupplier.HandleAsync(
            new CreateSupplier.Command(new SupplierFields(name, "556677-8899", "sales@acme.example", "+46 8 123", "https://acme.example", "Notes", active)),
            Ct)).Value;

    [Fact]
    public async Task ASupplierEditorIsEmptyWhenNewAndFilledWhenExisting()
    {
        var fresh = SupplierEditor();
        fresh.Title.ShouldBe(_localizer.Get("SupplierEditor_New"));
        fresh.IsActive.ShouldBeTrue();

        var existing = SupplierEditor(await AddSupplierAsync("Acme"));
        existing.Title.ShouldBe(_localizer.Get("SupplierEditor_Edit"));
        existing.Name.ShouldBe("Acme");
        existing.Email.ShouldBe("sales@acme.example");
        existing.Website.ShouldBe("https://acme.example");
    }

    [Fact]
    public async Task SavingNewAndExistingSuppliersRaiseSaved()
    {
        var editor = SupplierEditor();
        editor.Name = "Acme";
        SupplierDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;
        await editor.SaveCommand.ExecuteAsync(null);
        var id = saved.ShouldNotBeNull().Id;

        var again = SupplierEditor(saved);
        again.Name = "Acme Group";
        again.Saved += (_, dto) => saved = dto;
        await again.SaveCommand.ExecuteAsync(null);

        saved.Id.ShouldBe(id);
        saved.Name.ShouldBe("Acme Group");
    }

    [Fact]
    public async Task ABlankDuplicateOrBadlyAddressedSupplierIsRejected()
    {
        await AddSupplierAsync("Acme");
        var editor = SupplierEditor();

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("supplier_name_required"));

        editor.Name = "Acme";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("supplier_name_conflict"));

        editor.Name = "Other";
        editor.Email = "not-an-email";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("supplier_email_invalid"));

        editor.Email = string.Empty;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ASupplierEditorCancelRaisesCancelled()
    {
        var editor = SupplierEditor();
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task TheSupplierListShowsActiveAndInactiveAndSearches()
    {
        await AddSupplierAsync("Acme");
        await AddSupplierAsync("Dormant", active: false);
        var list = Suppliers();

        await list.LoadCommand.ExecuteAsync(null);
        list.Suppliers.Count.ShouldBe(2);
        list.Suppliers.Single(s => s.Supplier.Name == "Acme").StatusText.ShouldBe(_localizer.Get("Suppliers_Active"));
        list.Suppliers.Single(s => s.Supplier.Name == "Dormant").StatusText.ShouldBe(_localizer.Get("Suppliers_Inactive"));
        list.StatusMessage.ShouldBe(_localizer.Format("Suppliers_Count", 2));

        list.Search = "Dorm";
        await list.LoadCommand.ExecuteAsync(null);
        list.Suppliers.Single().Supplier.Name.ShouldBe("Dormant");
    }

    [Fact]
    public async Task SelectingNewSavingAndCancellingTheSupplierEditorWork()
    {
        await AddSupplierAsync("Acme");
        var list = Suppliers();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedSupplier = list.Suppliers.Single();
        list.Editor.ShouldNotBeNull().Name.ShouldBe("Acme");

        list.NewCommand.Execute(null);
        list.SelectedSupplier.ShouldBeNull();
        list.Editor!.Name = "Zeta";
        await list.Editor.SaveCommand.ExecuteAsync(null);
        await WaitUntilAsync(() => list.StatusMessage == _localizer.Format("Suppliers_Saved", "Zeta"));
        list.Editor.ShouldBeNull();
        list.Suppliers.Count.ShouldBe(2);

        list.NewCommand.Execute(null);
        list.Editor!.CancelCommand.Execute(null);
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task DeletingASupplierNeedsConfirmationAndHandlesAMissingOne()
    {
        var gone = await AddSupplierAsync("Gone");
        await AddSupplierAsync("Acme");
        var list = Suppliers();
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedSupplier = list.Suppliers.Single(s => s.Supplier.Name == "Acme");

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeFalse();

        list.SelectedSupplier = list.Suppliers.Single(s => s.Supplier.Id == gone.Id);
        await _fixture.DeleteSupplier.HandleAsync(new DeleteSupplier.Command(gone.Id), Ct);
        await list.ConfirmDeleteCommand.ExecuteAsync(null);
        list.StatusMessage.ShouldBe(Error("supplier_not_found"));

        list.SelectedSupplier = list.Suppliers.Single(s => s.Supplier.Name == "Acme");
        await list.ConfirmDeleteCommand.ExecuteAsync(null);
        list.Suppliers.ShouldBeEmpty();
    }
}
