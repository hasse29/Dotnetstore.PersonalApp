using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductModels;

internal sealed partial class ProductModelsViewModel(
    ListProductModels.Handler list,
    CreateProductModel.Handler create,
    UpdateProductModel.Handler update,
    DeleteProductModel.Handler delete,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<ProductModelDto> ProductModels { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ProductModelDto? SelectedProductModel { get; set; }

    [ObservableProperty]
    public partial ProductModelEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedProductModelChanged(ProductModelDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new ProductModelEditorViewModel(value, create, update, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListProductModels.Query(Search), cancellationToken);

        ProductModels.Clear();
        foreach (var item in response.Items)
        {
            ProductModels.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("ProductModels_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("ProductModels_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedProductModel = null;
        OpenEditor(new ProductModelEditorViewModel(null, create, update, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedProductModel is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteProductModel.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedProductModel is not null;

    private void OpenEditor(ProductModelEditorViewModel editor)
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
        SelectedProductModel = null;
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
    private async void OnEditorSaved(object? sender, ProductModelDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("ProductModels_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}