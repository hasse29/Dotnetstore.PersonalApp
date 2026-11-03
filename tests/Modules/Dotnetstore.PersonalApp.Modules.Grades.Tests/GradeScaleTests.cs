using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Grades.Tests;

public sealed class GradeScaleTests : IDisposable
{
    private readonly GradesFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AScaleIsStoredWithItsGradesBestFirst()
    {
        var created = await _fixture.AddSwedishScaleAsync(Ct);

        created.DisplayName.ShouldBe("A-F (Sweden)");
        created.Items.Select(i => i.Label).ShouldBe(["A", "B", "C", "D", "E", "F"]);
        created.Summary.ShouldBe("A, B, C, D, E, F");
        created.Items.Single(i => i.Label == "F").IsPassing.ShouldBeFalse();

        var listed = (await _fixture.ListScales.HandleAsync(new ListGradeScales.Query(), Ct)).ShouldHaveSingleItem();
        listed.Items.Select(i => i.Label).ShouldBe(["A", "B", "C", "D", "E", "F"]);
    }

    [Fact]
    public async Task ScalesFromDifferentCountriesLiveSideBySide()
    {
        await _fixture.AddSwedishScaleAsync(Ct);
        await _fixture.CreateScale.HandleAsync(
            new CreateGradeScale.Command("1-6", "Germany", null, [new(null, "1", 6, null, true), new(null, "2", 5, null, true), new(null, "6", 1, null, false)]),
            Ct);

        var scales = await _fixture.ListScales.HandleAsync(new ListGradeScales.Query(), Ct);

        scales.Select(s => s.DisplayName).ShouldBe(["1-6 (Germany)", "A-F (Sweden)"]);
    }

    [Fact]
    public async Task TextIsTrimmedAndEmptyOptionalFieldsBecomeNull()
    {
        var created = (await _fixture.CreateScale.HandleAsync(
            new CreateGradeScale.Command("  Pass/Fail  ", "  ", "", [new(null, "  Pass ", 1, " ", true)]), Ct)).Value;

        created.Name.ShouldBe("Pass/Fail");
        created.Country.ShouldBeNull();
        created.Description.ShouldBeNull();
        created.Items.ShouldHaveSingleItem().Label.ShouldBe("Pass");
        created.Items[0].Description.ShouldBeNull();
    }

    [Fact]
    public async Task InvalidScalesAreRejectedWithTheMatchingCode()
    {
        var item = new GradeScaleItemInput(null, "A", 1, null, true);

        async Task<string> CodeAsync(string name, IReadOnlyList<GradeScaleItemInput> items) =>
            (await _fixture.CreateScale.HandleAsync(new CreateGradeScale.Command(name, null, null, items), Ct)).Error.Code;

        (await CodeAsync(" ", [item])).ShouldBe("grade_scale_name_required");
        (await CodeAsync(new string('x', 101), [item])).ShouldBe("grade_scale_name_too_long");
        (await CodeAsync("Scale", [])).ShouldBe("grade_scale_items_required");
        (await CodeAsync("Scale", [item with { Label = " " }])).ShouldBe("grade_item_label_required");
        (await CodeAsync("Scale", [item with { Label = new string('x', 21) }])).ShouldBe("grade_item_label_too_long");
        (await CodeAsync("Scale", [item, item with { Label = " a " }])).ShouldBe("grade_item_label_duplicate");
        (await CodeAsync("Scale", [.. Enumerable.Range(0, 51).Select(i => item with { Label = i.ToString(System.Globalization.CultureInfo.InvariantCulture) })]))
            .ShouldBe("grade_scale_too_many_items");
    }

    [Fact]
    public async Task UpdatingKeepsExistingGradesAddsNewOnesAndRemovesUnusedOnes()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var keptA = scale.Items.Single(i => i.Label == "A");
        var keptB = scale.Items.Single(i => i.Label == "B");

        var updated = (await _fixture.UpdateScale.HandleAsync(
            new UpdateGradeScale.Command(
                scale.Id,
                "Swedish",
                "Sweden",
                null,
                [new(keptA.Id, "A+", 6, null, true), new(keptB.Id, "B", 4, null, true), new(null, "F", 0, null, false)]),
            Ct)).Value;

        updated.Name.ShouldBe("Swedish");
        updated.Items.Select(i => i.Label).ShouldBe(["A+", "B", "F"]);
        updated.Items.Single(i => i.Label == "A+").Id.ShouldBe(keptA.Id);
        updated.Items.Single(i => i.Label == "B").Id.ShouldBe(keptB.Id);
    }

    [Fact]
    public async Task AGradeInUseCannotBeRemovedFromTheScale()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);
        var student = _fixture.Students.Add("Anna");
        var subject = _fixture.Subjects.Add("Mathematics");
        var a = scale.Items.Single(i => i.Label == "A");
        await _fixture.CreateGrade.HandleAsync(new Modules.Grades.Features.Grades.CreateGrade.Command(Guid.NewGuid(), student.Id, subject.Id, a.Id, new DateOnly(2026, 10, 1), null), Ct);

        var result = await _fixture.UpdateScale.HandleAsync(
            new UpdateGradeScale.Command(scale.Id, "A-F", "Sweden", null, [.. scale.Items.Where(i => i.Label != "A").Select(i => new GradeScaleItemInput(i.Id, i.Label, i.Rank, i.Description, i.IsPassing))]),
            Ct);

        result.Error.Code.ShouldBe("grade_item_in_use");
        (await _fixture.ListScales.HandleAsync(new ListGradeScales.Query(), Ct)).ShouldHaveSingleItem().Items.Count.ShouldBe(6);
    }

    [Fact]
    public async Task UpdatingAnUnknownScaleOrGradeFails()
    {
        var scale = await _fixture.AddSwedishScaleAsync(Ct);

        var unknownScale = await _fixture.UpdateScale.HandleAsync(new UpdateGradeScale.Command(Guid.NewGuid(), "X", null, null, GradesFixture.SwedishSteps), Ct);
        var unknownItem = await _fixture.UpdateScale.HandleAsync(
            new UpdateGradeScale.Command(scale.Id, "X", null, null, [new(Guid.NewGuid(), "A", 1, null, true)]), Ct);

        unknownScale.Error.Code.ShouldBe("grade_scale_not_found");
        unknownItem.Error.Code.ShouldBe("grade_item_not_found");
    }

    [Fact]
    public async Task AnUnusedScaleCanBeDeletedButAScaleWithGradesCannot()
    {
        var unused = await _fixture.AddSwedishScaleAsync(Ct);
        (await _fixture.DeleteScale.HandleAsync(new DeleteGradeScale.Command(unused.Id), Ct)).IsSuccess.ShouldBeTrue();
        (await _fixture.ListScales.HandleAsync(new ListGradeScales.Query(), Ct)).ShouldBeEmpty();
        (await _fixture.DeleteScale.HandleAsync(new DeleteGradeScale.Command(unused.Id), Ct)).Error.Code.ShouldBe("grade_scale_not_found");

        var used = await _fixture.AddSwedishScaleAsync(Ct);
        var student = _fixture.Students.Add("Anna");
        var subject = _fixture.Subjects.Add("Mathematics");
        await _fixture.CreateGrade.HandleAsync(
            new Modules.Grades.Features.Grades.CreateGrade.Command(Guid.NewGuid(), student.Id, subject.Id, used.Items[0].Id, new DateOnly(2026, 10, 1), null), Ct);

        (await _fixture.DeleteScale.HandleAsync(new DeleteGradeScale.Command(used.Id), Ct)).Error.Code.ShouldBe("grade_scale_in_use");
    }

    [Fact]
    public async Task TheEditorSavesANewScaleWithItsRows()
    {
        var editor = new GradeScaleEditorViewModel(null, _fixture.CreateScale, _fixture.UpdateScale, new Localizer());
        var saved = new List<GradeScaleDto>();
        editor.Saved += (_, scale) => saved.Add(scale);

        editor.Name = "Pass/Fail";
        editor.AddItemCommand.Execute(null);
        editor.Items[0].Label = "Pass";
        editor.AddItemCommand.Execute(null);
        editor.Items[1].Label = "Fail";
        editor.Items[1].IsPassing = false;
        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldHaveSingleItem().Items.Select(i => i.Label).ShouldBe(["Pass", "Fail"]);
    }

    [Fact]
    public async Task TheEditorRemovesARowAndShowsErrors()
    {
        var editor = new GradeScaleEditorViewModel(null, _fixture.CreateScale, _fixture.UpdateScale, new Localizer());
        editor.Name = "Scale";
        editor.AddItemCommand.Execute(null);
        editor.Items[0].RemoveCommand.Execute(null);

        editor.Items.ShouldBeEmpty();

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe("Add at least one grade.");
    }
}
