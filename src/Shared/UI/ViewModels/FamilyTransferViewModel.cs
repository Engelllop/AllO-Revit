using System.Collections.ObjectModel;
using System.Windows.Input;
using AllO.Core;
using AllO.Models;
using AllO.Services;

namespace AllO.UI.ViewModels;

public class FamilyTransferViewModel : ViewModelBase
{
    private readonly IRevitService _service;

    public string DocumentName { get; }

    public ObservableCollection<LinkDocumentInfo> Links { get; } = new();
    public ObservableCollection<LinkedCategoryInfo> Categories { get; } = new();
    public ObservableCollection<LinkFamilyTypeInfo> FamilyTypes { get; } = new();

    private LinkDocumentInfo? _selectedLink;
    public LinkDocumentInfo? SelectedLink
    {
        get => _selectedLink;
        set
        {
            if (SetProperty(ref _selectedLink, value))
            {
                Categories.Clear();
                FamilyTypes.Clear();
                SelectedCategory = null;
                SelectedFamilyType = null;
                if (value != null && value.IsLoaded)
                    LoadCategories();
                OnPropertyChanged(nameof(CanCopy));
                OnPropertyChanged(nameof(CategoriesEnabled));
                OnPropertyChanged(nameof(FamilyTypesEnabled));
            }
        }
    }

    private LinkedCategoryInfo? _selectedCategory;
    public LinkedCategoryInfo? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                FamilyTypes.Clear();
                SelectedFamilyType = null;
                if (value != null && SelectedLink != null && SelectedLink.IsLoaded)
                    LoadFamilyTypes();
                OnPropertyChanged(nameof(CanCopy));
                OnPropertyChanged(nameof(FamilyTypesEnabled));
            }
        }
    }

    private LinkFamilyTypeInfo? _selectedFamilyType;
    public LinkFamilyTypeInfo? SelectedFamilyType
    {
        get => _selectedFamilyType;
        set
        {
            if (SetProperty(ref _selectedFamilyType, value))
                OnPropertyChanged(nameof(CanCopy));
        }
    }

    public bool CanCopy =>
        SelectedLink != null
        && SelectedLink.IsLoaded
        && SelectedCategory != null
        && SelectedFamilyType != null;

    public bool CategoriesEnabled => SelectedLink != null && SelectedLink.IsLoaded;
    public bool FamilyTypesEnabled => CategoriesEnabled && SelectedCategory != null;

    private string _statusMessage = "Select a linked model, category, and family type.";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand CopyCommand { get; }
    public ICommand CloseCommand { get; }
    public Action? CloseAction { get; set; }

    public FamilyTransferViewModel(IRevitService service)
    {
        _service = service;
        DocumentName = _service.GetDocumentName();

        foreach (var l in _service.GetLinkedDocuments().Where(x => x.IsLoaded))
            Links.Add(l);

        CopyCommand = new RelayCommand(_ => ExecuteCopy());

        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());

        if (Links.Count == 0)
            StatusMessage = "No loaded linked models. Add Revit links and ensure they are loaded (not unloaded).";
    }

    private void LoadCategories()
    {
        if (SelectedLink == null) return;
        foreach (var c in _service.GetCategoriesInLink(SelectedLink.LinkInstanceId))
            Categories.Add(c);
        StatusMessage = Categories.Count == 0
            ? "No loadable families (non–in-place) found in the link."
            : $"{Categories.Count} categor(ies) in link.";
    }

    private void LoadFamilyTypes()
    {
        if (SelectedLink == null || SelectedCategory == null) return;
        foreach (var t in _service.GetFamilyTypesInLinkCategory(SelectedLink.LinkInstanceId, SelectedCategory.CategoryId))
            FamilyTypes.Add(t);
        StatusMessage = FamilyTypes.Count == 0
            ? "No family types in this category."
            : $"{FamilyTypes.Count} type(s). Click Copy to place instances in the host.";
    }

    private void ExecuteCopy()
    {
        if (!CanCopy || SelectedLink == null || SelectedFamilyType == null || SelectedCategory == null) return;
        try
        {
            int n = _service.CopyFamilyInstancesFromLinkToHost(SelectedLink.LinkInstanceId, SelectedFamilyType.FamilySymbolId);
            StatusMessage = n == 0
                ? "Nothing was copied (no instances of this type in the link, or operation skipped)."
                : $"Copied {n} instance(s) into the host model at matching positions.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}
