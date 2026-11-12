using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;
using Dotnetstore.PersonalApp.Modules.Grades.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;

namespace Dotnetstore.PersonalApp.Modules.Grades.Tests;

public sealed class GradesViewModelTests : IDisposable
{
    private readonly GradesFixture _fixture = new();
    private readonly Localizer _localizer = new();
    private readonly Guid _userId = Guid.NewGuid();

    public GradesViewModelTests()
    {
        _localizer.AddResources(GradesStrings.ResourceManager);
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

    // ---- grade editor ----

    private GradeEditorViewModel GradeEditor(
        Guid? studentId,
        GradeDto? existing,
        IReadOnlyList<StudentReference> students,
        IReadOnlyList<SubjectReference> subjects,
        IReadOnlyList<GradeScaleDto> scales) =>
        new(_userId, studentId, existing, students, subjects, scales, _fixture.CreateGrade, _fixture.UpdateGrade, _localizer, _fixture.Clock);

    [Fact]
    public async Task ANewGradeEditorStartsOnTodayAndPreselectsTheOnlyScale()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var student = _fixture.Students.Add("Anna");

        var editor = GradeEditor(student.Id, null, [student], [], [scale]);

        editor.Title.ShouldBe(_localizer.Get("GradeEditor_New"));
        editor.Date.ShouldBe(_fixture.Clock.GetLocalNow().Date);
        editor.SelectedScale.ShouldBe(scale);
        editor.Items.Count.ShouldBe(6);
        editor.ShowStudentPicker.ShouldBeFalse();
    }

    [Fact]
    public async Task WithSeveralScalesNoneIsPreselectedAndPickingOneFillsTheGrades()
    {
        var swedish = await _fixture.AddSwedishScaleAsync(Ct);
        var other = (await _fixture.CreateScale.HandleAsync(new CreateGradeScale.Command("Pass/Fail", null, null, [new(null, "Pass", 1, null, true), new(null, "Fail", 0, null, false)]), Ct)).Value;
        var editor = GradeEditor(null, null, [], [], [swedish, other]);

        editor.SelectedScale.ShouldBeNull();
        editor.Items.ShouldBeEmpty();
        editor.ShowStudentPicker.ShouldBeTrue();

        editor.SelectedScale = other;
        editor.Items.Select(i => i.Label).ShouldBe(["Pass", "Fail"]);
        editor.SelectedItem = editor.Items[0];

        editor.SelectedScale = swedish;
        editor.SelectedItem.ShouldBeNull();
        editor.Items.Count.ShouldBe(6);
    }

    [Fact]
    public async Task SavingAFullGradeCreatesItAndRaisesSaved()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var student = _fixture.Students.Add("Anna");
        var subject = _fixture.Subjects.Add("Maths");
        var editor = GradeEditor(student.Id, null, [student], [subject], [scale]);
        editor.SelectedSubject = subject;
        editor.SelectedItem = editor.Items.Single(i => i.Label == "A");
        editor.Comment = "Well done";
        GradeDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().StudentId.ShouldBe(student.Id);
        saved.Comment.ShouldBe("Well done");
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task OnThePageTheStudentComesFromThePicker()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var student = _fixture.Students.Add("Anna");
        var subject = _fixture.Subjects.Add("Maths");
        var editor = GradeEditor(null, null, [student], [subject], [scale]);
        editor.SelectedStudent = student;
        editor.SelectedSubject = subject;
        editor.SelectedItem = editor.Items[0];
        GradeDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().StudentId.ShouldBe(student.Id);
    }

    [Fact]
    public async Task MissingStudentSubjectOrGradeAreEachReported()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var student = _fixture.Students.Add("Anna");
        var subject = _fixture.Subjects.Add("Maths");
        var editor = GradeEditor(null, null, [student], [subject], [scale]);
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("grade_student_required"));

        editor.SelectedStudent = student;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("grade_subject_required"));

        editor.SelectedSubject = subject;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("grade_item_required"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task AnExistingGradeFillsTheEditorAndSavingUpdatesIt()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var student = _fixture.Students.Add("Anna");
        var subject = _fixture.Subjects.Add("Maths");
        var c = scale.Items.Single(i => i.Label == "C");
        var grade = (await _fixture.CreateGrade.HandleAsync(
            new CreateGrade.Command(_userId, student.Id, subject.Id, c.Id, new DateOnly(2026, 10, 1), "OK"), Ct)).Value;

        var editor = GradeEditor(null, grade, [student], [subject], [scale]);

        editor.Title.ShouldBe(_localizer.Get("GradeEditor_Edit"));
        editor.ShowStudentPicker.ShouldBeFalse();
        editor.SelectedSubject.ShouldBe(subject);
        editor.SelectedScale.ShouldBe(scale);
        editor.SelectedItem!.Label.ShouldBe("C");
        editor.Comment.ShouldBe("OK");
        editor.Date.ShouldBe(new DateTime(2026, 10, 1));

        editor.SelectedItem = editor.Items.Single(i => i.Label == "B");
        GradeDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;
        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Id.ShouldBe(grade.Id);
    }

    [Fact]
    public async Task AGradeEditorCancelRaisesCancelled()
    {
        var editor = GradeEditor(null, null, [], [], []);
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    // ---- scales list ----

    private GradeScalesViewModel Scales() =>
        new(_fixture.ListScales, _fixture.CreateScale, _fixture.UpdateScale, _fixture.DeleteScale, _localizer);

    [Fact]
    public async Task TheScalesLoadWithTheirCount()
    {
        await _fixture.AddSwedishScaleAsync(Ct);
        var list = Scales();

        await list.LoadCommand.ExecuteAsync(null);

        list.Scales.Single().Name.ShouldBe("A-F");
        list.StatusMessage.ShouldBe(_localizer.Format("GradeScales_Count", 1));
    }

    [Fact]
    public async Task SelectingNewAndCancelOpenAndCloseTheScaleEditor()
    {
        await _fixture.AddSwedishScaleAsync(Ct);
        var list = Scales();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedScale = list.Scales.Single();
        list.Editor.ShouldNotBeNull();

        list.NewCommand.Execute(null);
        list.SelectedScale.ShouldBeNull();
        list.Editor.ShouldNotBeNull();

        list.Editor.CancelCommand.Execute(null);
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task DeletingAScaleNeedsConfirmationAndCanBeWithdrawn()
    {
        await _fixture.AddSwedishScaleAsync(Ct);
        var list = Scales();
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedScale = list.Scales.Single();

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeFalse();

        list.RequestDeleteCommand.Execute(null);
        await list.ConfirmDeleteCommand.ExecuteAsync(null);
        list.Scales.ShouldBeEmpty();
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task AScaleInUseCannotBeDeleted()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var student = _fixture.Students.Add("Anna");
        var subject = _fixture.Subjects.Add("Maths");
        await _fixture.CreateGrade.HandleAsync(new CreateGrade.Command(_userId, student.Id, subject.Id, scale.Items[0].Id, new DateOnly(2026, 10, 1), null), Ct);
        var list = Scales();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedScale = list.Scales.Single();

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(Error("grade_scale_in_use"));
        list.Scales.Count.ShouldBe(1);
    }

    // ---- test editor ----

    private TestEditorViewModel TestEditor(TestDto? existing = null) =>
        new(_userId, existing, _fixture.CreateTest, _fixture.UpdateTest, _localizer, _fixture.Clock);

    private async Task<TestDto> AddTestAsync(double maxScore = 40) =>
        (await _fixture.CreateTest.HandleAsync(new CreateTest.Command(_userId, "Algebra", new DateOnly(2026, 10, 15), maxScore), Ct)).Value;

    [Fact]
    public async Task ANewTestEditorStartsOnTodayAndAnExistingOneIsFilled()
    {
        var fresh = TestEditor();
        fresh.Title.ShouldBe(_localizer.Get("TestEditor_New"));
        fresh.Date.ShouldBe(_fixture.Clock.GetLocalNow().Date);
        fresh.MaxScoreText.ShouldBeEmpty();

        var existing = TestEditor(await AddTestAsync(40));
        existing.Title.ShouldBe(_localizer.Get("TestEditor_Edit"));
        existing.Name.ShouldBe("Algebra");
        existing.MaxScoreText.ShouldBe("40");
        existing.Date.ShouldBe(new DateTime(2026, 10, 15));
    }

    [Theory]
    [InlineData("40")]
    [InlineData("40.5")]
    [InlineData("40,5")]
    public async Task SavingATestAcceptsADecimalPointOrComma(string max)
    {
        var editor = TestEditor();
        editor.Name = "Algebra";
        editor.MaxScoreText = max;
        TestDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().MaxScore.ShouldBe(ScoreText.Parse(max));
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ABlankNameOrBadMaxScoreIsRejected()
    {
        var editor = TestEditor();
        editor.MaxScoreText = "40";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("test_name_required"));

        editor.Name = "Algebra";
        editor.MaxScoreText = "lots";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("test_max_score_invalid"));

        editor.MaxScoreText = "-5";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("test_max_score_invalid"));

        editor.MaxScoreText = string.Empty;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("test_max_score_invalid"));
    }

    [Fact]
    public async Task UpdatingATestToAMaxScoreBelowASavedResultIsRejected()
    {
        var test = await AddTestAsync(40);
        var student = _fixture.Students.Add("Anna");
        await _fixture.SaveTestResult.HandleAsync(new SaveTestResult.Command(test.Id, _userId, student.Id, 35, null), Ct);
        var editor = TestEditor(test);
        editor.MaxScoreText = "20";

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("test_max_score_below_results"));
    }

    [Fact]
    public async Task ATestEditorCancelRaisesCancelled()
    {
        var editor = TestEditor();
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    // ---- test results ----

    private TestResultsViewModel Results(TestDto test, IReadOnlyList<StudentReference> students, IReadOnlyList<GradeScaleDto> scales) =>
        new(_userId, test, students, scales, _fixture.ListTestResults, _fixture.SaveTestResult, _fixture.DeleteTestResult, _localizer);

    [Fact]
    public async Task TheResultsListsStudentsByNameWithScoreAndGrade()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var test = await AddTestAsync(40);
        var bo = _fixture.Students.Add("Bo");
        var anna = _fixture.Students.Add("Anna");
        await _fixture.SaveTestResult.HandleAsync(new SaveTestResult.Command(test.Id, _userId, bo.Id, 30, scale.Items[1].Id), Ct);
        await _fixture.SaveTestResult.HandleAsync(new SaveTestResult.Command(test.Id, _userId, anna.Id, 12.5, null), Ct);
        var results = Results(test, [anna, bo], [scale]);

        await results.LoadCommand.ExecuteAsync(null);

        results.Rows.Select(r => r.StudentName).ShouldBe(["Anna", "Bo"]);
        results.Rows[0].ScoreDisplay.ShouldBe($"{12.5.ToString("0.##", CultureInfo.CurrentCulture)} / 40");
        results.Rows[0].GradeDisplay.ShouldBe("-");
        results.Rows[1].GradeDisplay.ShouldBe("B");
        results.StatusMessage.ShouldBe(_localizer.Format("TestResults_Count", 2));
        results.ScoreLabel.ShouldBe(_localizer.Format("TestResultField_Score", "40"));
        results.SelectedScale.ShouldBe(scale);
    }

    [Fact]
    public async Task APickedRowFillsTheFormAndClearEmptiesIt()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var test = await AddTestAsync(40);
        var anna = _fixture.Students.Add("Anna");
        await _fixture.SaveTestResult.HandleAsync(new SaveTestResult.Command(test.Id, _userId, anna.Id, 33, scale.Items[0].Id), Ct);
        var results = Results(test, [anna], [scale]);
        await results.LoadCommand.ExecuteAsync(null);

        results.SelectedRow = results.Rows.Single();

        results.SelectedStudent.ShouldBe(anna);
        results.ScoreInput.ShouldBe("33");
        results.SelectedItem!.Label.ShouldBe("A");
        results.DeleteCommand.CanExecute(null).ShouldBeTrue();

        results.ClearCommand.Execute(null);
        results.SelectedRow.ShouldBeNull();
        results.SelectedStudent.ShouldBeNull();
        results.ScoreInput.ShouldBeEmpty();
        results.DeleteCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task SavingAResultListsItClearsTheFormAndRaisesChanged()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var test = await AddTestAsync(40);
        var anna = _fixture.Students.Add("Anna");
        var results = Results(test, [anna], [scale]);
        await results.LoadCommand.ExecuteAsync(null);
        var changed = false;
        results.Changed += (_, _) => changed = true;
        results.SelectedStudent = anna;
        results.ScoreInput = "27,5";
        results.SelectedItem = results.Items.Single(i => i.Label == "C");

        await results.SaveCommand.ExecuteAsync(null);

        results.StatusMessage.ShouldBe(_localizer.Format("TestResults_Saved", "Anna"));
        results.Rows.Single().Result.Score.ShouldBe(27.5);
        results.SelectedStudent.ShouldBeNull();
        results.ScoreInput.ShouldBeEmpty();
        changed.ShouldBeTrue();
    }

    [Fact]
    public async Task SavingWithoutAStudentOrAValidScoreIsRejected()
    {
        var test = await AddTestAsync(40);
        var anna = _fixture.Students.Add("Anna");
        var results = Results(test, [anna], []);
        await results.LoadCommand.ExecuteAsync(null);
        var changed = false;
        results.Changed += (_, _) => changed = true;

        results.ScoreInput = "10";
        await results.SaveCommand.ExecuteAsync(null);
        results.ErrorMessage.ShouldBe(Error("test_result_student_required"));

        results.SelectedStudent = anna;
        results.ScoreInput = "abc";
        await results.SaveCommand.ExecuteAsync(null);
        results.ErrorMessage.ShouldBe(Error("test_result_score_invalid"));

        results.ScoreInput = "41";
        await results.SaveCommand.ExecuteAsync(null);
        results.ErrorMessage.ShouldBe(Error("test_result_score_exceeds_max"));

        results.Rows.ShouldBeEmpty();
        changed.ShouldBeFalse();
    }

    [Fact]
    public async Task DeletingAResultRemovesItAndRaisesChanged()
    {
        var test = await AddTestAsync(40);
        var anna = _fixture.Students.Add("Anna");
        await _fixture.SaveTestResult.HandleAsync(new SaveTestResult.Command(test.Id, _userId, anna.Id, 10, null), Ct);
        var results = Results(test, [anna], []);
        await results.LoadCommand.ExecuteAsync(null);
        results.SelectedRow = results.Rows.Single();
        var changed = false;
        results.Changed += (_, _) => changed = true;

        await results.DeleteCommand.ExecuteAsync(null);

        results.Rows.ShouldBeEmpty();
        changed.ShouldBeTrue();
    }

    [Fact]
    public async Task DeletingAResultThatIsAlreadyGoneShowsTheError()
    {
        var test = await AddTestAsync(40);
        var anna = _fixture.Students.Add("Anna");
        var saved = (await _fixture.SaveTestResult.HandleAsync(new SaveTestResult.Command(test.Id, _userId, anna.Id, 10, null), Ct)).Value;
        var results = Results(test, [anna], []);
        await results.LoadCommand.ExecuteAsync(null);
        results.SelectedRow = results.Rows.Single();
        await _fixture.DeleteTestResult.HandleAsync(new DeleteTestResult.Command(saved.Id, _userId), Ct);

        await results.DeleteCommand.ExecuteAsync(null);

        results.StatusMessage.ShouldBe(Error("test_result_not_found"));
    }

    [Fact]
    public async Task AResultForAStudentWhoIsNoLongerKnownShowsAQuestionMark()
    {
        var test = await AddTestAsync(40);
        var ghost = _fixture.Students.Add("Ghost");
        await _fixture.SaveTestResult.HandleAsync(new SaveTestResult.Command(test.Id, _userId, ghost.Id, 10, null), Ct);
        var results = Results(test, [], []);

        await results.LoadCommand.ExecuteAsync(null);

        results.Rows.Single().StudentName.ShouldBe("?");
        await WaitUntilAsync(() => results.Rows.Count == 1);
    }

    [Fact]
    public async Task ScoreTextParsesCommasPointsAndGarbage()
    {
        ScoreText.Parse("12,5").ShouldBe(12.5);
        ScoreText.Parse(" 7 ").ShouldBe(7);
        double.IsNaN(ScoreText.Parse("")).ShouldBeTrue();
        double.IsNaN(ScoreText.Parse(null)).ShouldBeTrue();
        double.IsNaN(ScoreText.Parse("abc")).ShouldBeTrue();
        ScoreText.Format(12.5).ShouldBe(12.5.ToString("0.##", CultureInfo.CurrentCulture));
    }
}
