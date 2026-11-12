using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class CustomerViewModelTests : IDisposable
{
    private readonly ScreenHarness _harness = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _harness.Dispose();

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10, Ct);
        }

        condition().ShouldBeTrue();
    }

    private CustomersViewModel List() =>
        new(_harness.Fixture.ListCustomers, _harness.Fixture.CreateCustomer, _harness.Fixture.UpdateCustomer, _harness.Fixture.DeleteCustomer, _harness.Localizer);

    private CustomerEditorViewModel Editor(CustomerDto? existing = null) =>
        new(existing, _harness.Fixture.CreateCustomer, _harness.Fixture.UpdateCustomer, _harness.Localizer);

    private string Error(string code) => _harness.Localizer.Get("Error_" + code);

    // ---- editor ----

    [Fact]
    public async Task ANewEditorIsEmptyAndActive()
    {
        var editor = Editor();

        editor.Title.ShouldBe(_harness.Localizer.Get("CustomerEditor_New"));
        editor.Name.ShouldBeEmpty();
        editor.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task AnExistingCustomerFillsTheEditor()
    {
        var customer = await _harness.Fixture.NewCustomerAsync("Acme School");

        var editor = Editor(customer);

        editor.Title.ShouldBe(_harness.Localizer.Get("CustomerEditor_Edit"));
        editor.Name.ShouldBe("Acme School");
        editor.OrganizationNumber.ShouldBe("556677-8899");
    }

    [Fact]
    public async Task SavingANewCustomerRaisesSavedWithTheStoredCustomer()
    {
        var editor = Editor();
        editor.Name = "Beta AB";
        CustomerDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Name.ShouldBe("Beta AB");
        saved.Id.ShouldNotBe(Guid.Empty);
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SavingAnExistingCustomerChangesItInPlace()
    {
        var customer = await _harness.Fixture.NewCustomerAsync("Acme School");
        var editor = Editor(customer);
        editor.Name = "Acme Academy";
        editor.IsActive = false;
        CustomerDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Id.ShouldBe(customer.Id);
        saved.Name.ShouldBe("Acme Academy");
        saved.IsActive.ShouldBeFalse();
        (await _harness.Fixture.ListCustomers.HandleAsync(new ListCustomers.Query(), Ct)).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task ABlankNameIsRejectedAndNothingIsSaved()
    {
        var editor = Editor();
        editor.Name = "   ";
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("customer_name_required"));
        raised.ShouldBeFalse();
        (await _harness.Fixture.ListCustomers.HandleAsync(new ListCustomers.Query(), Ct)).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task ATooLongNameIsRejected()
    {
        var editor = Editor();
        editor.Name = new string('x', 500);

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("customer_name_too_long"));
    }

    [Fact]
    public async Task ACorrectedNameClearsThePreviousError()
    {
        var editor = Editor();
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldNotBeNull();

        editor.Name = "Gamma";
        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task CancelTellsTheListToCloseTheEditor()
    {
        var editor = Editor();
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    // ---- detail ----

    [Fact]
    public async Task DetailShowsTheCustomerAndHasNoOptionalPanelsWithoutFactories()
    {
        var customer = await _harness.Fixture.NewCustomerAsync("Acme School");
        var detail = new CustomerDetailViewModel(customer, "Active");
        var closed = 0;
        var edit = 0;
        detail.Closed += (_, _) => closed++;
        detail.EditRequested += (_, _) => edit++;

        detail.BackCommand.Execute(null);
        detail.EditCommand.Execute(null);

        detail.Customer.ShouldBe(customer);
        detail.StatusText.ShouldBe("Active");
        detail.Photo.ShouldBeNull();
        detail.Addresses.ShouldBeNull();
        detail.EmailAddresses.ShouldBeNull();
        detail.PhoneNumbers.ShouldBeNull();
        closed.ShouldBe(1);
        edit.ShouldBe(1);
    }

    // ---- list ----

    [Fact]
    public async Task TheListShowsEveryCustomerWithActiveOrInactiveText()
    {
        var active = await _harness.Fixture.NewCustomerAsync("Active Co");
        var inactive = await _harness.Fixture.NewCustomerAsync("Inactive Co");
        await _harness.Fixture.UpdateCustomer.HandleAsync(new UpdateCustomer.Command(inactive.Id, SalesFixture.Customer("Inactive Co") with { IsActive = false }), Ct);
        var list = List();

        await list.LoadCommand.ExecuteAsync(null);

        list.Customers.Single(c => c.Customer.Id == active.Id).StatusText.ShouldBe(_harness.Localizer.Get("Customers_Active"));
        list.Customers.Single(c => c.Customer.Id == inactive.Id).StatusText.ShouldBe(_harness.Localizer.Get("Customers_Inactive"));
        list.StatusMessage.ShouldBe(_harness.Localizer.Format("Customers_Count", 2));
    }

    [Fact]
    public async Task SearchNarrowsTheList()
    {
        await _harness.Fixture.NewCustomerAsync("Alpha");
        await _harness.Fixture.NewCustomerAsync("Beta");
        var list = List();
        list.Search = "Bet";

        await list.LoadCommand.ExecuteAsync(null);

        list.Customers.Single().Customer.Name.ShouldBe("Beta");
    }

    [Fact]
    public async Task NewOpensAnEditorAndDeselects()
    {
        await _harness.Fixture.NewCustomerAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCustomer = list.Customers.Single();

        list.NewCommand.Execute(null);

        list.SelectedCustomer.ShouldBeNull();
        list.Editor.ShouldNotBeNull().Name.ShouldBeEmpty();
    }

    [Fact]
    public async Task SelectingACustomerOpensItsEditor()
    {
        await _harness.Fixture.NewCustomerAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedCustomer = list.Customers.Single();

        list.Editor.ShouldNotBeNull().Name.ShouldBe("Alpha");
        list.OpenDetailCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task SavingFromTheEditorClosesItReloadsAndSaysWhatWasSaved()
    {
        var list = List();
        list.NewCommand.Execute(null);
        list.Editor!.Name = "Gamma";

        await list.Editor.SaveCommand.ExecuteAsync(null);

        await WaitUntilAsync(() => list.StatusMessage == _harness.Localizer.Format("Customers_Saved", "Gamma"));
        list.Editor.ShouldBeNull();
        list.Customers.Single().Customer.Name.ShouldBe("Gamma");
    }

    [Fact]
    public async Task CancellingTheEditorClosesItWithoutChangingAnything()
    {
        await _harness.Fixture.NewCustomerAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCustomer = list.Customers.Single();

        list.Editor!.CancelCommand.Execute(null);

        list.Editor.ShouldBeNull();
        list.SelectedCustomer.ShouldBeNull();
        list.Customers.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DetailOpensForTheSelectionAndBackReturnsToTheList()
    {
        await _harness.Fixture.NewCustomerAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCustomer = list.Customers.Single();

        list.OpenDetailCommand.Execute(null);
        list.Detail.ShouldNotBeNull().Customer.Name.ShouldBe("Alpha");
        list.Detail.StatusText.ShouldBe(_harness.Localizer.Get("Customers_Active"));

        list.Detail.BackCommand.Execute(null);
        list.Detail.ShouldBeNull();
        list.Customers.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DetailCannotBeOpenedWithoutASelection()
    {
        var list = List();

        list.OpenDetailCommand.CanExecute(null).ShouldBeFalse();
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        list.ConfirmDeleteCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task DeletingNeedsAConfirmationThatCanBeWithdrawn()
    {
        await _harness.Fixture.NewCustomerAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCustomer = list.Customers.Single();

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);

        list.IsConfirmingDelete.ShouldBeFalse();
        (await _harness.Fixture.ListCustomers.HandleAsync(new ListCustomers.Query(), Ct)).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task ConfirmingTheDeleteRemovesTheCustomerAndClosesTheEditor()
    {
        await _harness.Fixture.NewCustomerAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCustomer = list.Customers.Single();
        list.RequestDeleteCommand.Execute(null);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.Customers.ShouldBeEmpty();
        list.Editor.ShouldBeNull();
        list.SelectedCustomer.ShouldBeNull();
        list.IsConfirmingDelete.ShouldBeFalse();
        list.StatusMessage.ShouldBe(_harness.Localizer.Format("Customers_Count", 0));
    }

    [Fact]
    public async Task ACustomerWithOffersCannotBeDeletedAndTheListSaysWhy()
    {
        var customer = await _harness.Fixture.NewCustomerAsync("Alpha");
        await _harness.Fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCustomer = list.Customers.Single();
        list.RequestDeleteCommand.Execute(null);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(Error("customer_in_use"));
        list.Customers.Count.ShouldBe(1);
    }
}
