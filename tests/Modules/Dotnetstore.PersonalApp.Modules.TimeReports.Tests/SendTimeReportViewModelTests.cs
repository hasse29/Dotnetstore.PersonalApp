using Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;
using Dotnetstore.PersonalApp.Modules.TimeReports.Resources;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Email;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

public sealed class SendTimeReportViewModelTests : IDisposable
{
    private readonly TimeReportsFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();
    private readonly ClientReference _acme;
    private readonly FakeMailSender _mailSender = new();
    private readonly FakePeople _people = new();
    private readonly FakeEmails _emails = new();

    public SendTimeReportViewModelTests()
    {
        _acme = _fixture.Clients.Add("Acme AB");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<SendTimeReportViewModel> OpenAsync(bool withReport = true)
    {
        if (withReport)
        {
            await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(_user, _acme.Id) with { Date = new DateOnly(2026, 10, 30) }, Ct);
        }

        var localizer = new Localizer();
        localizer.AddResources(TimeReportsStrings.ResourceManager);
        localizer.SetCulture("en");

        var viewModel = new SendTimeReportViewModel(_user, _acme.Id, _acme.Name, 2026, _fixture.Export, _mailSender, _people, [_emails], localizer);
        await viewModel.InitializeAsync(Ct);
        return viewModel;
    }

    [Fact]
    public async Task ASingleAddressIsChosenAutomatically()
    {
        _emails.Add(_acme.Id, "Work", "info@acme.example");

        var viewModel = await OpenAsync();

        Assert.Equal("info@acme.example", viewModel.SelectedAddress?.Address);
        Assert.False(viewModel.HasNoAddresses);
    }

    [Fact]
    public async Task SeveralAddressesAreLeftForTheUserToChoose()
    {
        _emails.Add(_acme.Id, "Work", "info@acme.example");
        _emails.Add(_acme.Id, "Billing", "billing@acme.example");

        var viewModel = await OpenAsync();

        Assert.Equal(2, viewModel.Addresses.Count);
        Assert.Null(viewModel.SelectedAddress);
    }

    [Fact]
    public async Task NoAddressesIsReportedAndCannotBeSentTo()
    {
        var viewModel = await OpenAsync();

        await viewModel.SendCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasNoAddresses);
        Assert.NotNull(viewModel.ErrorMessage);
        Assert.Empty(_mailSender.Sent);
    }

    [Fact]
    public async Task RecipientsAreTheClientFollowedByItsContactPersons()
    {
        var anna = _people.Add(_acme.Id, "Anna Svensson");
        _people.Add(Guid.NewGuid(), "Someone Else");

        var viewModel = await OpenAsync();

        Assert.Equal([_acme.Name, "Anna Svensson"], viewModel.Recipients.Select(r => r.Name));
        Assert.Equal(_acme.Id, viewModel.SelectedRecipient?.Id);
        Assert.Contains(anna, viewModel.Recipients.Select(r => r.Id));
    }

    [Fact]
    public async Task ChoosingAContactPersonShowsThatPersonsAddresses()
    {
        var anna = _people.Add(_acme.Id, "Anna Svensson");
        _emails.Add(_acme.Id, "Work", "info@acme.example");
        _emails.Add(_acme.Id, "Billing", "billing@acme.example");
        _emails.Add(anna, "Work", "anna@acme.example");

        var viewModel = await OpenAsync();
        viewModel.SelectedRecipient = viewModel.Recipients.Single(r => r.Id == anna);

        Assert.Equal(["anna@acme.example"], viewModel.Addresses.Select(a => a.Address));
        Assert.Equal("anna@acme.example", viewModel.SelectedAddress?.Address);
    }

    [Fact]
    public async Task SendMailsTheExportToTheChosenAddress()
    {
        _emails.Add(_acme.Id, "Work", "info@acme.example");
        _emails.Add(_acme.Id, "Billing", "billing@acme.example");
        var viewModel = await OpenAsync();
        viewModel.SelectedAddress = viewModel.Addresses.Single(a => a.Address == "billing@acme.example");
        string? sentTo = null;
        viewModel.Sent += (_, address) => sentTo = address;

        await viewModel.SendCommand.ExecuteAsync(null);

        var mail = Assert.Single(_mailSender.Sent);
        Assert.Equal("billing@acme.example", mail.To);
        Assert.Equal("Time reports Acme AB 2026", mail.Subject);
        var attachment = Assert.Single(mail.Attachments);
        Assert.Equal("TimeReports Acme AB 2026.xlsx", attachment.FileName);
        Assert.False(attachment.Content.IsEmpty);
        Assert.Equal("billing@acme.example", sentTo);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task NothingIsSentWhenThereAreNoReportsToExport()
    {
        _emails.Add(_acme.Id, "Work", "info@acme.example");
        var viewModel = await OpenAsync(withReport: false);

        await viewModel.SendCommand.ExecuteAsync(null);

        Assert.Empty(_mailSender.Sent);
        Assert.NotNull(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task AMailFailureIsShownAndNothingIsReportedAsSent()
    {
        _emails.Add(_acme.Id, "Work", "info@acme.example");
        _mailSender.Failure = Error.NotFound("email_no_system_smtp", "No system SMTP account is registered.");
        var viewModel = await OpenAsync();
        var sent = false;
        viewModel.Sent += (_, _) => sent = true;

        await viewModel.SendCommand.ExecuteAsync(null);

        Assert.False(sent);
        Assert.NotNull(viewModel.ErrorMessage);
    }

    private sealed class FakeMailSender : ISystemMailSender
    {
        public List<SystemMail> Sent { get; } = [];

        public Error? Failure { get; set; }

        public Task<Result<int>> SendAsync(SystemMail mail, CancellationToken cancellationToken)
        {
            if (Failure is not null)
            {
                return Task.FromResult<Result<int>>(Failure);
            }

            Sent.Add(mail);
            return Task.FromResult<Result<int>>(1);
        }
    }

    private sealed class FakePeople : IContactPersonLookup
    {
        private readonly List<(Guid ClientId, ContactPersonReference Person)> _people = [];

        public Guid Add(Guid clientId, string name)
        {
            var id = Guid.NewGuid();
            _people.Add((clientId, new ContactPersonReference(id, name)));
            return id;
        }

        public Task<IReadOnlyList<ContactPersonReference>> ListAsync(Guid clientId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ContactPersonReference>>([.. _people.Where(p => p.ClientId == clientId).Select(p => p.Person)]);
    }

    private sealed class FakeEmails : IContactInfoSource
    {
        private readonly List<(Guid ObjectId, ContactEntry Entry)> _entries = [];

        public void Add(Guid objectId, string type, string email) =>
            _entries.Add((objectId, new ContactEntry(Guid.NewGuid(), ContactKind.Email, type, email)));

        public Task<IReadOnlyList<ContactEntry>> ListAsync(Guid objectId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ContactEntry>>([.. _entries.Where(e => e.ObjectId == objectId).Select(e => e.Entry)]);
    }
}
