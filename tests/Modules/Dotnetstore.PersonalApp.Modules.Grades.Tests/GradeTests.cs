using Dotnetstore.PersonalApp.Modules.Grades.Consumers;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;

namespace Dotnetstore.PersonalApp.Modules.Grades.Tests;

public sealed class GradeTests : IDisposable
{
    private readonly GradesFixture _fixture = new();
    private readonly Guid _teacher = Guid.NewGuid();
    private readonly StudentReference _anna;
    private readonly SubjectReference _math;

    public GradeTests()
    {
        _anna = _fixture.Students.Add("Anna Berg");
        _math = _fixture.Subjects.Add("Mathematics");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<Result<GradeDto>> GiveAsync(GradeScaleDto scale, string label, int day, string? comment = null, Guid? teacher = null, StudentReference? student = null) =>
        await _fixture.CreateGrade.HandleAsync(
            new CreateGrade.Command(
                teacher ?? _teacher,
                (student ?? _anna).Id,
                _math.Id,
                scale.Items.Single(i => i.Label == label).Id,
                new DateOnly(2026, 10, day),
                comment),
            Ct);

    private GradesViewModel BuildViewModel(Guid? studentId) => new(
        _teacher,
        studentId,
        _fixture.ListGrades,
        _fixture.CreateGrade,
        _fixture.UpdateGrade,
        _fixture.DeleteGrade,
        _fixture.ListScales,
        _fixture.Students,
        _fixture.Subjects,
        new Localizer(),
        _fixture.Clock);

    [Fact]
    public async Task AGradeIsStoredWithTheScaleStepItPointsAt()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);

        var grade = (await GiveAsync(scale, "B", 5, "  Good progress  ")).Value;

        grade.GradeLabel.ShouldBe("B");
        grade.ScaleName.ShouldBe("A-F");
        grade.IsPassing.ShouldBeTrue();
        grade.Comment.ShouldBe("Good progress");
        grade.Date.ShouldBe(new DateOnly(2026, 10, 5));
        grade.CreatedAt.ShouldBe(_fixture.Clock.GetUtcNow());
    }

    [Fact]
    public async Task GradesAreListedLatestFirstAndScopedToTheTeacherAndStudent()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var olle = _fixture.Students.Add("Olle Dahl");
        await GiveAsync(scale, "C", 5);
        await GiveAsync(scale, "A", 20);
        await GiveAsync(scale, "F", 1);
        await GiveAsync(scale, "B", 7, student: olle);
        await GiveAsync(scale, "E", 9, teacher: Guid.NewGuid());

        var forAnna = await _fixture.ListGrades.HandleAsync(new ListGrades.Query(_teacher, _anna.Id), Ct);
        var forEveryone = await _fixture.ListGrades.HandleAsync(new ListGrades.Query(_teacher, null), Ct);

        forAnna.Select(g => g.GradeLabel).ShouldBe(["A", "C", "F"]);
        forEveryone.Select(g => g.GradeLabel).ShouldBe(["A", "B", "C", "F"]);
    }

    [Fact]
    public async Task AGradeNeedsAnExistingStudentSubjectAndScaleStep()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var itemId = scale.Items[0].Id;
        var date = new DateOnly(2026, 10, 5);

        async Task<string> CodeAsync(Guid student, Guid subject, Guid item) =>
            (await _fixture.CreateGrade.HandleAsync(new CreateGrade.Command(_teacher, student, subject, item, date, null), Ct)).Error.Code;

        (await CodeAsync(Guid.Empty, _math.Id, itemId)).ShouldBe("grade_student_required");
        (await CodeAsync(_anna.Id, Guid.Empty, itemId)).ShouldBe("grade_subject_required");
        (await CodeAsync(_anna.Id, _math.Id, Guid.Empty)).ShouldBe("grade_item_required");
        (await CodeAsync(Guid.NewGuid(), _math.Id, itemId)).ShouldBe("grade_student_not_found");
        (await CodeAsync(_anna.Id, Guid.NewGuid(), itemId)).ShouldBe("grade_subject_not_found");
        (await CodeAsync(_anna.Id, _math.Id, Guid.NewGuid())).ShouldBe("grade_item_not_found");
        (await GiveAsync(scale, "A", 5, new string('x', 501))).Error.Code.ShouldBe("grade_comment_too_long");
    }

    [Fact]
    public async Task AGradeCanBeCorrectedButOnlyByTheTeacherWhoGaveIt()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var grade = (await GiveAsync(scale, "C", 5)).Value;
        var b = scale.Items.Single(i => i.Label == "B");

        var other = await _fixture.UpdateGrade.HandleAsync(new UpdateGrade.Command(grade.Id, Guid.NewGuid(), _math.Id, b.Id, new DateOnly(2026, 10, 9), null), Ct);
        var updated = await _fixture.UpdateGrade.HandleAsync(new UpdateGrade.Command(grade.Id, _teacher, _math.Id, b.Id, new DateOnly(2026, 10, 9), "Retest"), Ct);

        other.Error.Code.ShouldBe("grade_not_found");
        updated.Value.GradeLabel.ShouldBe("B");
        updated.Value.Date.ShouldBe(new DateOnly(2026, 10, 9));
        updated.Value.Comment.ShouldBe("Retest");
        updated.Value.StudentId.ShouldBe(_anna.Id);
    }

    [Fact]
    public async Task AGradeCanBeDeletedOnlyByTheTeacherWhoGaveIt()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var grade = (await GiveAsync(scale, "C", 5)).Value;

        (await _fixture.DeleteGrade.HandleAsync(new DeleteGrade.Command(grade.Id, Guid.NewGuid()), Ct)).Error.Code.ShouldBe("grade_not_found");
        (await _fixture.DeleteGrade.HandleAsync(new DeleteGrade.Command(grade.Id, _teacher), Ct)).IsSuccess.ShouldBeTrue();

        (await _fixture.ListGrades.HandleAsync(new ListGrades.Query(_teacher, null), Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task DeletingATeacherAStudentOrASubjectDeletesTheirGrades()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var olle = _fixture.Students.Add("Olle Dahl");
        var otherTeacher = Guid.NewGuid();
        await GiveAsync(scale, "A", 1);
        await GiveAsync(scale, "B", 2, student: olle);
        await GiveAsync(scale, "C", 3, teacher: otherTeacher);

        await new StudentDeletedConsumer(_fixture.DeleteGradesFor).ConsumeAsync(new StudentDeleted(olle.Id), Ct);
        (await _fixture.ListGrades.HandleAsync(new ListGrades.Query(_teacher, null), Ct)).ShouldHaveSingleItem().GradeLabel.ShouldBe("A");

        await new UserDeletedConsumer(_fixture.DeleteGradesFor).ConsumeAsync(new UserDeleted(otherTeacher), Ct);
        (await _fixture.ListGrades.HandleAsync(new ListGrades.Query(otherTeacher, null), Ct)).ShouldBeEmpty();

        await new SubjectDeletedConsumer(_fixture.DeleteGradesFor).ConsumeAsync(new SubjectDeleted(_math.Id), Ct);
        (await _fixture.ListGrades.HandleAsync(new ListGrades.Query(_teacher, null), Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task TheStudentTabListsOnlyThatStudentsGradesAndAddsOneForThem()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var olle = _fixture.Students.Add("Olle Dahl");
        await GiveAsync(scale, "A", 1);
        await GiveAsync(scale, "B", 2, student: olle);
        var viewModel = BuildViewModel(_anna.Id);

        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.IsPageLayout.ShouldBeFalse();
        viewModel.Rows.ShouldHaveSingleItem().SubjectName.ShouldBe("Mathematics");

        viewModel.NewCommand.Execute(null);
        var editor = viewModel.Editor.ShouldNotBeNull();
        editor.ShowStudentPicker.ShouldBeFalse();
        editor.SelectedSubject = _math;
        editor.SelectedItem.ShouldBeNull();
        editor.Items.Select(i => i.Label).ShouldBe(["A", "B", "C", "D", "E", "F"]);
        editor.SelectedItem = editor.Items.Single(i => i.Label == "C");
        await editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(100, Ct);

        viewModel.Editor.ShouldBeNull();
        viewModel.Rows.Select(r => r.GradeLabel).ShouldBe(["C", "A"]);
        viewModel.Rows.ShouldAllBe(r => r.Grade.StudentId == _anna.Id);
    }

    [Fact]
    public async Task ThePageListsEveryStudentAndTheEditorAsksWhichOne()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var olle = _fixture.Students.Add("Olle Dahl");
        await GiveAsync(scale, "A", 1);
        await GiveAsync(scale, "B", 2, student: olle);
        var viewModel = BuildViewModel(null);

        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.IsPageLayout.ShouldBeTrue();
        viewModel.Rows.Select(r => r.StudentName).ShouldBe(["Olle Dahl", "Anna Berg"]);

        viewModel.NewCommand.Execute(null);
        var editor = viewModel.Editor.ShouldNotBeNull();
        editor.ShowStudentPicker.ShouldBeTrue();
        editor.SelectedSubject = _math;
        editor.SelectedItem = editor.Items[0];
        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe("Choose a student.");
    }

    [Fact]
    public async Task SelectingARowOpensItsGradeForEditingAndDeletingNeedsConfirmation()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        await GiveAsync(scale, "C", 5);
        var viewModel = BuildViewModel(_anna.Id);
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedRow = viewModel.Rows[0];
        var editor = viewModel.Editor.ShouldNotBeNull();
        editor.SelectedScale.ShouldNotBeNull().Name.ShouldBe("A-F");
        editor.SelectedItem.ShouldNotBeNull().Label.ShouldBe("C");
        editor.SelectedSubject.ShouldBe(_math);

        viewModel.RequestDeleteCommand.Execute(null);
        viewModel.IsConfirmingDelete.ShouldBeTrue();
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        viewModel.Rows.ShouldBeEmpty();
        viewModel.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task NewPointsAtSubjectsAndScalesWhenThereAreNone()
    {
        var viewModel = new GradesViewModel(
            _teacher, _anna.Id, _fixture.ListGrades, _fixture.CreateGrade, _fixture.UpdateGrade, _fixture.DeleteGrade, _fixture.ListScales,
            _fixture.Students, new GradesFixture.FakeSubjects(), new Localizer(), _fixture.Clock);
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.NewCommand.Execute(null);

        viewModel.Editor.ShouldBeNull();
        viewModel.StatusMessage.ShouldNotBeNullOrEmpty();
    }
}
