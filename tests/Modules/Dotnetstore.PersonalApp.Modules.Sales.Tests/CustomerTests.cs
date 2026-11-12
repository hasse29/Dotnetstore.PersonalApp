using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class CustomerTests
{
    [Fact]
    public async Task CreatesACustomerAndTrimsTheName()
    {
        using var fixture = new SalesFixture();

        var result = await fixture.CreateCustomer.HandleAsync(
            new CreateCustomer.Command(SalesFixture.Customer("  Acme School  ")), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Acme School");
    }

    [Fact]
    public async Task RejectsADuplicateNameIgnoringCase()
    {
        using var fixture = new SalesFixture();
        await fixture.NewCustomerAsync("Acme School");

        var result = await fixture.CreateCustomer.HandleAsync(
            new CreateCustomer.Command(SalesFixture.Customer("ACME SCHOOL")), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("customer_name_conflict");
    }

    [Fact]
    public async Task DeletesACustomerWithoutOffers()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();

        var result = await fixture.DeleteCustomer.HandleAsync(new DeleteCustomer.Command(customer.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await fixture.ListCustomers.HandleAsync(new ListCustomers.Query(), CancellationToken.None)).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task KeepsACustomerThatHasOffers()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());

        var result = await fixture.DeleteCustomer.HandleAsync(new DeleteCustomer.Command(customer.Id), CancellationToken.None);

        result.Error.Code.ShouldBe("customer_in_use");
    }

    [Fact]
    public async Task ACustomerIsKeptWhenAnotherModuleFailsToCleanUpAfterIt()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var handler = new DeleteCustomer.Handler(fixture.DbFactory, new FailingPublisher());

        await Should.ThrowAsync<InvalidOperationException>(() => handler.HandleAsync(new DeleteCustomer.Command(customer.Id), CancellationToken.None));

        var customers = await fixture.ListCustomers.HandleAsync(new ListCustomers.Query(), CancellationToken.None);
        customers.Items.ShouldContain(c => c.Id == customer.Id);
    }

    private sealed class FailingPublisher : Dotnetstore.PersonalApp.Shared.Events.IEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
            where TEvent : notnull => throw new InvalidOperationException("A consumer failed.");
    }
}
