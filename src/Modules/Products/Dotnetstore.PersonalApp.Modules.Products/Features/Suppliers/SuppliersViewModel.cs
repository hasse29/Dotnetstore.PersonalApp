using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;

internal sealed partial class SuppliersViewModel(
    ListSuppliers.Handler list,
    CreateSupplier.Handler create,
    UpdateSupplier.Handler update,
    DeleteSupplier.Handler delete,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<SupplierRow> Suppliers { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SupplierRow? SelectedSupplier { get; set; }

    [ObservableProperty]
    public partial SupplierEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSupplierChanged(SupplierRow? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SupplierEditorViewModel(value.Supplier, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListSuppliers.Query(Search), cancellationToken);

        Suppliers.Clear();
        foreach (var supplier in response.Items)
        {
            Suppliers.Add(new SupplierRow(supplier, localization.Get(supplier.IsActive ? "Suppliers_Active" : "Suppliers_Inactive")));
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Suppliers_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Suppliers_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSupplier = null;
        OpenEditor(new SupplierEditorViewModel(null, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSupplier is not { } row)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSupplier.Command(row.Supplier.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedSupplier is not null;

    private void OpenEditor(SupplierEditorViewModel editor)
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
        SelectedSupplier = null;
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
    private async void OnEditorSaved(object? sender, SupplierDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("Suppliers_Saved", saved.Name);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
