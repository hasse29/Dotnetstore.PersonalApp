using Dotnetstore.PersonalApp.Host;
using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.Cvs;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.TimeReports;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class CompositionRootTests
{
    [Fact]
    public void ServicesBuildAndResolveMainWindowViewModel()
    {
        using var services = CompositionRoot.BuildServices();

        (services.GetRequiredService<MainWindowViewModel>()).ShouldNotBeNull();
    }

    [Fact]
    public void AddressesModuleProvidesTheAddressesPanelForOtherModules()
    {
        using var services = CompositionRoot.BuildServices();

        var panel = services.GetRequiredService<IObjectAddressesPanelFactory>().Create(Guid.NewGuid());

        panel.ShouldNotBeNull();
    }

    [Fact]
    public void EmailAddressesModuleProvidesTheEmailAddressesPanelForOtherModules()
    {
        using var services = CompositionRoot.BuildServices();

        var panel = services.GetRequiredService<IObjectEmailAddressesPanelFactory>().Create(Guid.NewGuid());

        panel.ShouldNotBeNull();
    }

    [Fact]
    public void PhoneNumbersModuleProvidesThePhoneNumbersPanelForOtherModules()
    {
        using var services = CompositionRoot.BuildServices();

        var panel = services.GetRequiredService<IObjectPhoneNumbersPanelFactory>().Create(Guid.NewGuid());

        panel.ShouldNotBeNull();
    }

    [Fact]
    public void CvsModuleProvidesTheCvsPanelForOtherModules()
    {
        using var services = CompositionRoot.BuildServices();

        var panel = services.GetRequiredService<IUserCvsPanelFactory>().Create(Guid.NewGuid());

        panel.ShouldNotBeNull();
    }

    [Fact]
    public void TimeReportsModuleProvidesTheTimeReportsPanelForOtherModules()
    {
        using var services = CompositionRoot.BuildServices();

        var panel = services.GetRequiredService<ITimeReportsPanelFactory>().CreateForClient(Guid.NewGuid());

        panel.ShouldNotBeNull();
    }

    [Fact]
    public void EmailModuleAddsItsAccountsPageToTheShell()
    {
        using var services = CompositionRoot.BuildServices();

        services.GetServices<IModulePage>().ShouldContain(page => page.ModuleKey == "email");
    }

    [Fact]
    public void EveryModulePageCreatesItsViewModel()
    {
        using var services = CompositionRoot.BuildServices();

        foreach (var page in services.GetServices<IModulePage>())
        {
            page.CreateViewModel().ShouldNotBeNull();
        }
    }
}
