using Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Students;

namespace Dotnetstore.PersonalApp.Modules.Grades.Tests;

public sealed class TestsTests : IDisposable
{
    private readonly GradesFixture _fixture = new();
    private readonly Guid _teacher = Guid.NewGuid();
    private readonly StudentReference _anna;
    private readonly StudentReference _bo;

    public TestsTests()
    {
        _anna = _fixture.Students.Add("Anna Berg");
        _bo = _fixture.Students.Add("Bo Carlsson");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<TestDto> AddTestAsync(string name = "Chapter 3", int day = 10, double max = 20, Guid? teacher = null) =>
        (await _fixture.CreateTest.HandleAsync(new CreateTest.Command(teacher ?? _teacher, name, new DateOnly(2026, 10, day), max), Ct)).Value;

    private async Task<TestResultDto> ScoreAsync(TestDto test, StudentReference student, double score, Guid? itemId = null) =>
        (await _fixture.SaveTestResult.HandleAsync(new SaveTestResult.Command(test.Id, _teacher, student.Id, score, itemId), Ct)).Value;

    private TestsViewModel BuildViewModel() => new(
        _teacher,
        _fixture.ListTests,
        _fixture.CreateTest,
        _fixture.UpdateTest,
        _fixture.DeleteTest,
        _fixture.ListTestResults,
        _fixture.SaveTestResult,
        _fixture.DeleteTestResult,
        _fixture.ListScales,
        _fixture.Students,
        new Localizer(),
        _fixture.Clock);

    [Fact]
    public async Task ATestIsStoredWithItsNameDateAndMaximumScore()
    {
        var test = (await _fixture.CreateTest.HandleAsync(new CreateTest.Command(_teacher, "  Chapter 3 quiz  ", new DateOnly(2026, 10, 10), 24.5), Ct)).Value;

        test.Name.ShouldBe("Chapter 3 quiz");
        test.Date.ShouldBe(new DateOnly(2026, 10, 10));
        test.MaxScore.ShouldBe(24.5);
        test.ResultCount.ShouldBe(0);
    }

    [Fact]
    public async Task TestsAreListedLatestFirstWithTheirResultCount()
    {
        var early = await AddTestAsync("Early", 3);
        var late = await AddTestAsync("Late", 20);
        await ScoreAsync(late, _anna, 15);
        await ScoreAsync(late, _bo, 12);

        var tests = await _fixture.ListTests.HandleAsync(new ListTests.Query(_teacher), Ct);

        tests.Select(t => t.Name).ShouldBe(["Late", "Early"]);
        tests[0].ResultCount.ShouldBe(2);
        tests[1].Id.ShouldBe(early.Id);
        tests[1].ResultCount.ShouldBe(0);
    }

    [Fact]
    public async Task InvalidTestsAreRejectedWithTheMatchingCode()
    {
        async Task<string> CodeAsync(string name, double max) =>
            (await _fixture.CreateTest.HandleAsync(new CreateTest.Command(_teacher, name, new DateOnly(2026, 10, 10), max), Ct)).Error.Code;

        (await CodeAsync(" ", 10)).ShouldBe("test_name_required");
        (await CodeAsync(new string('x', 101), 10)).ShouldBe("test_name_too_long");
        (await CodeAsync("Quiz", 0)).ShouldBe("test_max_score_invalid");
        (await CodeAsync("Quiz", -5)).ShouldBe("test_max_score_invalid");
        (await CodeAsync("Quiz", double.NaN)).ShouldBe("test_max_score_invalid");
    }

    [Fact]
    public async Task ATestCanBeCorrectedButNotBelowAScoreAlreadyGiven()
    {
        var test = await AddTestAsync(max: 20);
        await ScoreAsync(test, _anna, 18);

        var tooLow = await _fixture.UpdateTest.HandleAsync(new UpdateTest.Command(test.Id, _teacher, "Chapter 3", new DateOnly(2026, 10, 10), 15), Ct);
        var corrected = (await _fixture.UpdateTest.HandleAsync(new UpdateTest.Command(test.Id, _teacher, "  Chapter 4  ", new DateOnly(2026, 10, 12), 30), Ct)).Value;

        tooLow.Error.Code.ShouldBe("test_max_score_below_results");
        corrected.Name.ShouldBe("Chapter 4");
        corrected.MaxScore.ShouldBe(30);
        corrected.ResultCount.ShouldBe(1);
    }

    [Fact]
    public async Task AResultHasAScoreAndOptionallyAGrade()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var test = await AddTestAsync();
        var b = scale.Items.Single(i => i.Label == "B");

        var scoredOnly = await ScoreAsync(test, _anna, 14.5);
        var graded = await ScoreAsync(test, _bo, 17, b.Id);

        scoredOnly.GradeLabel.ShouldBeNull();
        scoredOnly.ScaleItemId.ShouldBeNull();
        graded.GradeLabel.ShouldBe("B");
        graded.ScaleId.ShouldBe(scale.Id);
        graded.IsPassing.ShouldBe(true);

        var results = (await _fixture.ListTestResults.HandleAsync(new ListTestResults.Query(test.Id, _teacher), Ct)).Value;
        results.Select(r => r.StudentId).ShouldBe([_bo.Id, _anna.Id]);
    }

    [Fact]
    public async Task SavingAgainReplacesTheStudentsEarlierResult()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var test = await AddTestAsync();
        var first = await ScoreAsync(test, _anna, 10);

        var second = await ScoreAsync(test, _anna, 16, scale.Items.Single(i => i.Label == "C").Id);

        second.Id.ShouldBe(first.Id);
        var results = (await _fixture.ListTestResults.HandleAsync(new ListTestResults.Query(test.Id, _teacher), Ct)).Value;
        var only = results.ShouldHaveSingleItem();
        only.Score.ShouldBe(16);
        only.GradeLabel.ShouldBe("C");
    }

    [Fact]
    public async Task InvalidResultsAreRejectedWithTheMatchingCode()
    {
        var test = await AddTestAsync(max: 20);

        async Task<string> CodeAsync(Guid testId, Guid studentId, double score, Guid? itemId = null, Guid? teacher = null) =>
            (await _fixture.SaveTestResult.HandleAsync(new SaveTestResult.Command(testId, teacher ?? _teacher, studentId, score, itemId), Ct)).Error.Code;

        (await CodeAsync(test.Id, Guid.Empty, 5)).ShouldBe("test_result_student_required");
        (await CodeAsync(test.Id, _anna.Id, -1)).ShouldBe("test_result_score_invalid");
        (await CodeAsync(test.Id, _anna.Id, double.NaN)).ShouldBe("test_result_score_invalid");
        (await CodeAsync(test.Id, _anna.Id, 20.5)).ShouldBe("test_result_score_exceeds_max");
        (await CodeAsync(test.Id, Guid.NewGuid(), 5)).ShouldBe("test_result_student_not_found");
        (await CodeAsync(test.Id, _anna.Id, 5, Guid.NewGuid())).ShouldBe("grade_item_not_found");
        (await CodeAsync(Guid.NewGuid(), _anna.Id, 5)).ShouldBe("test_not_found");
        (await CodeAsync(test.Id, _anna.Id, 5, teacher: Guid.NewGuid())).ShouldBe("test_not_found");

        // The full score is allowed.
        (await _fixture.SaveTestResult.HandleAsync(new SaveTestResult.Command(test.Id, _teacher, _anna.Id, 20, null), Ct)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ATeacherOnlySeesAndChangesTheirOwnTests()
    {
        var other = Guid.NewGuid();
        var mine = await AddTestAsync("Mine");
        await AddTestAsync("Theirs", teacher: other);

        (await _fixture.ListTests.HandleAsync(new ListTests.Query(_teacher), Ct)).ShouldHaveSingleItem().Id.ShouldBe(mine.Id);
        (await _fixture.ListTestResults.HandleAsync(new ListTestResults.Query(mine.Id, other), Ct)).Error.Code.ShouldBe("test_not_found");
        (await _fixture.UpdateTest.HandleAsync(new UpdateTest.Command(mine.Id, other, "Hijacked", new DateOnly(2026, 10, 10), 10), Ct)).Error.Code.ShouldBe("test_not_found");
        (await _fixture.DeleteTest.HandleAsync(new DeleteTest.Command(mine.Id, other), Ct)).Error.Code.ShouldBe("test_not_found");
    }

    [Fact]
    public async Task DeletingATestDeletesItsResultsAndDeletingAResultKeepsTheTest()
    {
        var test = await AddTestAsync();
        var annaResult = await ScoreAsync(test, _anna, 10);
        await ScoreAsync(test, _bo, 12);

        (await _fixture.DeleteTestResult.HandleAsync(new DeleteTestResult.Command(annaResult.Id, _teacher), Ct)).IsSuccess.ShouldBeTrue();
        (await _fixture.DeleteTestResult.HandleAsync(new DeleteTestResult.Command(annaResult.Id, _teacher), Ct)).Error.Code.ShouldBe("test_result_not_found");
        (await _fixture.ListTestResults.HandleAsync(new ListTestResults.Query(test.Id, _teacher), Ct)).Value.ShouldHaveSingleItem();

        (await _fixture.DeleteTest.HandleAsync(new DeleteTest.Command(test.Id, _teacher), Ct)).IsSuccess.ShouldBeTrue();
        (await _fixture.ListTests.HandleAsync(new ListTests.Query(_teacher), Ct)).ShouldBeEmpty();
        (await _fixture.ListTestResults.HandleAsync(new ListTestResults.Query(test.Id, _teacher), Ct)).Error.Code.ShouldBe("test_not_found");
    }

    [Fact]
    public async Task AGradeUsedByATestResultCannotBeRemovedFromItsScale()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var test = await AddTestAsync();
        await ScoreAsync(test, _anna, 17, scale.Items.Single(i => i.Label == "B").Id);

        var withoutB = await _fixture.UpdateScale.HandleAsync(
            new UpdateGradeScale.Command(scale.Id, "A-F", "Sweden", null, [.. scale.Items.Where(i => i.Label != "B").Select(i => new GradeScaleItemInput(i.Id, i.Label, i.Rank, i.Description, i.IsPassing))]),
            Ct);
        var deleted = await _fixture.DeleteScale.HandleAsync(new DeleteGradeScale.Command(scale.Id), Ct);

        withoutB.Error.Code.ShouldBe("grade_item_in_use");
        deleted.Error.Code.ShouldBe("grade_scale_in_use");
    }

    [Fact]
    public async Task ADeletedTeacherTakesTheirTestsAndADeletedStudentTheirResults()
    {
        var test = await AddTestAsync();
        await ScoreAsync(test, _anna, 10);
        await ScoreAsync(test, _bo, 12);

        await _fixture.DeleteGradesFor.HandleAsync(new DeleteGradesFor.Command(StudentId: _anna.Id), Ct);
        (await _fixture.ListTestResults.HandleAsync(new ListTestResults.Query(test.Id, _teacher), Ct)).Value
            .ShouldHaveSingleItem().StudentId.ShouldBe(_bo.Id);

        await _fixture.DeleteGradesFor.HandleAsync(new DeleteGradesFor.Command(UserId: _teacher), Ct);
        (await _fixture.ListTests.HandleAsync(new ListTests.Query(_teacher), Ct)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("17", 17)]
    [InlineData(" 17.5 ", 17.5)]
    [InlineData("17,5", 17.5)]
    public void ScoresAcceptEitherADecimalPointOrAComma(string text, double expected)
    {
        ScoreText.Parse(text).ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData(null)]
    public void TextThatIsNotANumberIsNotAScore(string? text)
    {
        double.IsNaN(ScoreText.Parse(text)).ShouldBeTrue();
    }

    [Fact]
    public async Task TheEditorCreatesATestThatIsThenSelectedForEnteringResults()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var viewModel = BuildViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.NewCommand.Execute(null);
        var editor = viewModel.Editor.ShouldNotBeNull();
        viewModel.Results.ShouldBeNull();
        editor.Name = "Chapter 3 quiz";
        editor.MaxScoreText = "20";
        await editor.SaveCommand.ExecuteAsync(null);

        // The page reacts to the editor's Saved event on its own, so wait for it to select the new test.
        for (var attempt = 0; attempt < 100 && viewModel.Results is null; attempt++)
        {
            await Task.Delay(50, Ct);
        }

        viewModel.Rows.ShouldHaveSingleItem().Name.ShouldBe("Chapter 3 quiz");
        viewModel.SelectedTest.ShouldNotBeNull().MaxScore.ShouldBe(20);
        var results = viewModel.Results.ShouldNotBeNull();
        await results.LoadCommand.ExecuteAsync(null);

        results.SelectedStudent = _anna;
        results.ScoreInput = "17,5";
        results.SelectedScale.ShouldNotBeNull().Id.ShouldBe(scale.Id);
        results.SelectedItem = results.Items.Single(i => i.Label == "B");
        await results.SaveCommand.ExecuteAsync(null);

        var row = results.Rows.ShouldHaveSingleItem();
        row.StudentName.ShouldBe("Anna Berg");
        row.GradeDisplay.ShouldBe("B");
        row.Result.Score.ShouldBe(17.5);
        results.ScoreInput.ShouldBeEmpty();
    }

    [Fact]
    public async Task ThePanelShowsAnErrorForAScoreAboveTheMaximumAndLoadsAChosenRowIntoTheForm()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var test = await AddTestAsync(max: 20);
        await ScoreAsync(test, _bo, 12, scale.Items.Single(i => i.Label == "D").Id);
        var viewModel = BuildViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedTest = viewModel.Rows.Single();
        var results = viewModel.Results.ShouldNotBeNull();
        await results.LoadCommand.ExecuteAsync(null);

        results.SelectedRow = results.Rows.Single();

        results.SelectedStudent.ShouldBe(_bo);
        results.ScoreInput.ShouldBe("12");
        results.SelectedItem.ShouldNotBeNull().Label.ShouldBe("D");

        results.ScoreInput = "25";
        await results.SaveCommand.ExecuteAsync(null);

        results.ErrorMessage.ShouldBe("Score is higher than the maximum score.");
        results.Rows.Single().Result.Score.ShouldBe(12);
    }

    [Fact]
    public async Task DeletingATestFromThePageRemovesItFromTheList()
    {
        var test = await AddTestAsync();
        await ScoreAsync(test, _anna, 10);
        var viewModel = BuildViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedTest = viewModel.Rows.Single();

        viewModel.RequestDeleteCommand.Execute(null);
        viewModel.IsConfirmingDelete.ShouldBeTrue();
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        viewModel.Rows.ShouldBeEmpty();
        viewModel.Editor.ShouldBeNull();
        viewModel.Results.ShouldBeNull();
    }
}
