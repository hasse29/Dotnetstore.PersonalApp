using Dotnetstore.PersonalApp.Modules.Schedule.Consumers;
using Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Tests;

public sealed class ScheduleEntryTests : IDisposable
{
    private readonly ScheduleFixture _fixture = new();
    private readonly Guid _teacher = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ATeacherCanTeachMathematicsEveryMondayAtEight()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var school = _fixture.Schools.Add("Northside School");

        var result = await _fixture.Create.HandleAsync(
            new CreateScheduleEntry.Command(_teacher, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(8, 45), math.Id, school.Id, "  B204 ", " ", []),
            Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Day.ShouldBe(DayOfWeek.Monday);
        result.Value.StartTime.ShouldBe(new TimeOnly(8, 0));
        result.Value.Room.ShouldBe("B204");
        result.Value.Notes.ShouldBeNull();
    }

    [Fact]
    public async Task ListIsInWeekOrderStartingOnMondayAndOnlyForThatTeacher()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var school = _fixture.Schools.Add("Northside School");
        await _fixture.CreateAsync(_teacher, DayOfWeek.Sunday, "10:00", "11:00", math, school, Ct);
        await _fixture.CreateAsync(_teacher, DayOfWeek.Wednesday, "13:00", "14:00", math, school, Ct);
        await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "10:00", "11:00", math, school, Ct);
        await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "09:00", math, school, Ct);
        await _fixture.CreateAsync(Guid.NewGuid(), DayOfWeek.Tuesday, "08:00", "09:00", math, school, Ct);

        var entries = await _fixture.List.HandleAsync(new ListScheduleEntries.Query(_teacher), Ct);

        entries.Select(e => (e.Day, e.StartTime.Hour)).ShouldBe(
            [(DayOfWeek.Monday, 8), (DayOfWeek.Monday, 10), (DayOfWeek.Wednesday, 13), (DayOfWeek.Sunday, 10)]);
    }

    [Theory]
    [InlineData("09:00", "09:00")]
    [InlineData("10:00", "09:00")]
    public async Task ALessonMustEndAfterItStarts(string start, string end)
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var school = _fixture.Schools.Add("Northside School");

        var result = await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, start, end, math, school, Ct);

        result.Error.Code.ShouldBe("schedule_end_before_start");
    }

    [Fact]
    public async Task TheSubjectAndTheSchoolMustExist()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var school = _fixture.Schools.Add("Northside School");
        var ghostSubject = new Dotnetstore.PersonalApp.Shared.Subjects.SubjectReference(Guid.NewGuid(), "Ghost");
        var ghostSchool = new Dotnetstore.PersonalApp.Shared.Clients.ClientReference(Guid.NewGuid(), "Ghost");

        var noSubject = await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "09:00", ghostSubject, school, Ct);
        var noSchool = await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "09:00", math, ghostSchool, Ct);

        noSubject.Error.Code.ShouldBe("schedule_subject_not_found");
        noSchool.Error.Code.ShouldBe("schedule_school_not_found");
    }

    [Fact]
    public async Task ATeacherCannotBeInTwoLessonsAtOnceButBackToBackIsFine()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var school = _fixture.Schools.Add("Northside School");
        var other = _fixture.Schools.Add("Southside School");
        await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "09:00", math, school, Ct);

        var overlapping = await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:30", "09:30", math, other, Ct);
        var backToBack = await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "09:00", "10:00", math, other, Ct);
        var otherDay = await _fixture.CreateAsync(_teacher, DayOfWeek.Tuesday, "08:30", "09:30", math, other, Ct);
        var otherTeacher = await _fixture.CreateAsync(Guid.NewGuid(), DayOfWeek.Monday, "08:30", "09:30", math, other, Ct);

        overlapping.Error.Code.ShouldBe("schedule_overlap");
        backToBack.IsSuccess.ShouldBeTrue();
        otherDay.IsSuccess.ShouldBeTrue();
        otherTeacher.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateChangesTheLessonWithoutClashingWithItself()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var physics = _fixture.Subjects.Add("Physics");
        var school = _fixture.Schools.Add("Northside School");
        var entry = (await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "09:00", math, school, Ct)).Value;

        var moved = await _fixture.Update.HandleAsync(
            new UpdateScheduleEntry.Command(entry.Id, _teacher, DayOfWeek.Monday, new TimeOnly(8, 30), new TimeOnly(9, 30), physics.Id, school.Id, "A1", "Bring lab coats", []),
            Ct);
        var stranger = await _fixture.Update.HandleAsync(
            new UpdateScheduleEntry.Command(entry.Id, Guid.NewGuid(), DayOfWeek.Monday, new TimeOnly(8, 30), new TimeOnly(9, 30), physics.Id, school.Id, null, null, []),
            Ct);

        moved.IsSuccess.ShouldBeTrue();
        moved.Value.SubjectId.ShouldBe(physics.Id);
        moved.Value.Notes.ShouldBe("Bring lab coats");
        stranger.Error.Code.ShouldBe("schedule_entry_not_found");
    }

    [Fact]
    public async Task OnlyTheOwnerCanDeleteALesson()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var school = _fixture.Schools.Add("Northside School");
        var entry = (await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "09:00", math, school, Ct)).Value;

        var stranger = await _fixture.Delete.HandleAsync(new DeleteScheduleEntry.Command(entry.Id, Guid.NewGuid()), Ct);
        var owner = await _fixture.Delete.HandleAsync(new DeleteScheduleEntry.Command(entry.Id, _teacher), Ct);

        stranger.Error.Code.ShouldBe("schedule_entry_not_found");
        owner.IsSuccess.ShouldBeTrue();
        (await _fixture.List.HandleAsync(new ListScheduleEntries.Query(_teacher), Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task LessonsDisappearWithTheirTeacherSchoolOrSubject()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var physics = _fixture.Subjects.Add("Physics");
        var north = _fixture.Schools.Add("Northside School");
        var south = _fixture.Schools.Add("Southside School");
        var other = Guid.NewGuid();
        await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "09:00", math, north, Ct);
        await _fixture.CreateAsync(_teacher, DayOfWeek.Tuesday, "08:00", "09:00", physics, north, Ct);
        await _fixture.CreateAsync(_teacher, DayOfWeek.Wednesday, "08:00", "09:00", physics, south, Ct);
        await _fixture.CreateAsync(other, DayOfWeek.Monday, "08:00", "09:00", math, south, Ct);

        await new SubjectDeletedConsumer(_fixture.DeleteFor).ConsumeAsync(new SubjectDeleted(math.Id), Ct);
        (await _fixture.List.HandleAsync(new ListScheduleEntries.Query(_teacher), Ct)).Count.ShouldBe(2);
        (await _fixture.List.HandleAsync(new ListScheduleEntries.Query(other), Ct)).ShouldBeEmpty();

        await new ClientDeletedConsumer(_fixture.DeleteFor).ConsumeAsync(new ClientDeleted(north.Id), Ct);
        (await _fixture.List.HandleAsync(new ListScheduleEntries.Query(_teacher), Ct)).ShouldHaveSingleItem().Day.ShouldBe(DayOfWeek.Wednesday);

        await new UserDeletedConsumer(_fixture.DeleteFor).ConsumeAsync(new UserDeleted(_teacher), Ct);
        (await _fixture.List.HandleAsync(new ListScheduleEntries.Query(_teacher), Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task TheScreenShowsNamedLessonsAndAddsOneThroughTheEditor()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var school = _fixture.Schools.Add("Northside School");
        await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "08:45", math, school, Ct);
        var screen = new ScheduleViewModel(
            _teacher, _fixture.List, _fixture.Create, _fixture.Update, _fixture.Delete, _fixture.Subjects, _fixture.Schools, _fixture.Students, new Localizer());

        await screen.LoadCommand.ExecuteAsync(null);

        var row = screen.Rows.ShouldHaveSingleItem();
        row.DayName.ShouldBe("Monday");
        row.TimeRange.ShouldBe("08:00–08:45");
        row.SubjectName.ShouldBe("Mathematics");
        row.SchoolName.ShouldBe("Northside School");

        screen.NewCommand.Execute(null);
        var editor = screen.Editor.ShouldNotBeNull();
        editor.SelectedDay = editor.Days.First(d => d.Day == DayOfWeek.Friday);
        editor.SelectedSubject = editor.Subjects[0];
        editor.SelectedSchool = editor.Schools[0];
        await editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(100, Ct);

        screen.Rows.Select(r => r.DayName).ShouldBe(["Monday", "Friday"]);
    }

    [Fact]
    public async Task StudentsOfTheSchoolCanAttendALessonAndBeChangedLater()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var school = _fixture.Schools.Add("Northside School");
        var anna = _fixture.Students.Add(school, "Anna Berg", "7A");
        var olle = _fixture.Students.Add(school, "Olle Dahl", "7A");
        var created = await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "09:00", math, school, Ct, [anna.Id, anna.Id]);

        created.Value.StudentIds.ShouldBe([anna.Id]);

        var updated = await _fixture.Update.HandleAsync(
            new UpdateScheduleEntry.Command(created.Value.Id, _teacher, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(9, 0), math.Id, school.Id, null, null, [olle.Id]),
            Ct);

        updated.Value.StudentIds.ShouldBe([olle.Id]);
        (await _fixture.List.HandleAsync(new ListScheduleEntries.Query(_teacher), Ct)).ShouldHaveSingleItem().StudentIds.ShouldBe([olle.Id]);
    }

    [Fact]
    public async Task OnlyStudentsOfTheLessonsSchoolCanAttend()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var north = _fixture.Schools.Add("Northside School");
        var south = _fixture.Schools.Add("Southside School");
        var southStudent = _fixture.Students.Add(south, "Sara Ek");

        var wrongSchool = await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "09:00", math, north, Ct, [southStudent.Id]);
        var unknown = await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "09:00", math, north, Ct, [Guid.NewGuid()]);

        wrongSchool.Error.Code.ShouldBe("schedule_student_not_found");
        unknown.Error.Code.ShouldBe("schedule_student_not_found");
    }

    [Fact]
    public async Task ADeletedStudentLeavesTheLessonButNotTheOthers()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var school = _fixture.Schools.Add("Northside School");
        var anna = _fixture.Students.Add(school, "Anna Berg");
        var olle = _fixture.Students.Add(school, "Olle Dahl");
        await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:00", "09:00", math, school, Ct, [anna.Id, olle.Id]);

        await new StudentDeletedConsumer(_fixture.RemoveStudent).ConsumeAsync(new StudentDeleted(anna.Id), Ct);

        (await _fixture.List.HandleAsync(new ListScheduleEntries.Query(_teacher), Ct)).ShouldHaveSingleItem().StudentIds.ShouldBe([olle.Id]);
    }

    [Fact]
    public async Task TheEditorListsTheSchoolsStudentsAndSavesTheTickedOnes()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var north = _fixture.Schools.Add("Northside School");
        var south = _fixture.Schools.Add("Southside School");
        var anna = _fixture.Students.Add(north, "Anna Berg", "7A");
        _fixture.Students.Add(south, "Sara Ek");
        var editor = new ScheduleEditorViewModel(
            null, _teacher, DayOption.All(new Localizer()), [math], [north, south], _fixture.Students, _fixture.Create, _fixture.Update, new Localizer());
        var saved = new List<ScheduleEntryDto>();
        editor.Saved += (_, entry) => saved.Add(entry);

        editor.SelectedSubject = math;
        editor.SelectedSchool = north;
        await editor.LoadStudentsCommand.ExecuteAsync(null);

        editor.Students.Select(s => s.Name).ShouldBe(["Anna Berg (7A)"]);

        editor.Students[0].IsSelected = true;
        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldHaveSingleItem().StudentIds.ShouldBe([anna.Id]);
    }

    [Fact]
    public async Task TheEditorExplainsWhatIsMissing()
    {
        var math = _fixture.Subjects.Add("Mathematics");
        var school = _fixture.Schools.Add("Northside School");
        var editor = new ScheduleEditorViewModel(
            null, _teacher, DayOption.All(new Localizer()), [math], [school], _fixture.Students, _fixture.Create, _fixture.Update, new Localizer());

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe("A subject is required.");
    }
}
