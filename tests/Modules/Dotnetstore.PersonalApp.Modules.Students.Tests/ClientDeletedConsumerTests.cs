using Dotnetstore.PersonalApp.Modules.Students.Consumers;
using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;

namespace Dotnetstore.PersonalApp.Modules.Students.Tests;

public sealed class ClientDeletedConsumerTests : IDisposable
{
    private readonly StudentsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ADeletedSchoolTakesItsStudentsWithIt()
    {
        var school = Guid.NewGuid();
        var student = (await _fixture.Create.HandleAsync(new CreateStudent.Command(school, "Anna", "Svensson", null, null), Ct)).Value;

        await new ClientDeletedConsumer(_fixture.DeleteForClient).ConsumeAsync(new ClientDeleted(school), Ct);

        (await _fixture.List.HandleAsync(new ListStudents.Query(school), Ct)).ShouldBeEmpty();
        _fixture.Events.Published.ShouldHaveSingleItem().ShouldBe(new StudentDeleted(student.Id));
    }
}
