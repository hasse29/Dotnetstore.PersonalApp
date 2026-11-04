using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOffers;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOfferProducts;

internal sealed partial class SpecialOfferProductsViewModel(
    ListSpecialOfferProducts.Handler list,
    CreateSpecialOfferProduct.Handler create,
    UpdateSpecialOfferProduct.Handler update,
    DeleteSpecialOfferProduct.Handler delete,
    ListSpecialOfferOptions.Handler listSpecialOfferOptions,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _specialOfferOptions = [];
    private IReadOnlyList<OptionDto> _productOptions = [];

    public ObservableCollection<SpecialOfferProductDto> SpecialOfferProducts { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SpecialOfferProductDto? SelectedSpecialOfferProduct { get; set; }

    [ObservableProperty]
    public partial SpecialOfferProductEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSpecialOfferProductChanged(SpecialOfferProductDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SpecialOfferProductEditorViewModel(value, create, update, _specialOfferOptions, _productOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _specialOfferOptions = await listSpecialOfferOptions.HandleAsync(cancellationToken);
        _productOptions = await references.ProductOptionsAsync(cancellationToken);

        var response = await list.HandleAsync(new ListSpecialOfferProducts.Query(Search), cancellationToken);

        SpecialOfferProducts.Clear();
        foreach (var item in response.Items)
        {
            SpecialOfferProducts.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("SpecialOfferProducts_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("SpecialOfferProducts_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSpecialOfferProduct = null;
        OpenEditor(new SpecialOfferProductEditorViewModel(null, create, update, _specialOfferOptions, _productOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSpecialOfferProduct is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSpecialOfferProduct.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedSpecialOfferProduct is not null;

    private void OpenEditor(SpecialOfferProductEditorViewModel editor)
    {
        DetachEditor();
        Editor = editor;
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
    }

    private void CloseEditor()
    {
        DetachEditor();
        Editor = null;
        SelectedSpecialOfferProduct = null;
        IsConfirmingDelete = false;
    }

    private void DetachEditor()
    {
        if (Editor is { } editor)
        {
            editor.Saved -= OnEditorSaved;
            editor.Cancelled -= OnEditorCancelled;
        }
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnEditorSaved(object? sender, SpecialOfferProductDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("SpecialOfferProducts_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}