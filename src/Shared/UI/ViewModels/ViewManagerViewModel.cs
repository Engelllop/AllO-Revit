using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AllO.Core;

namespace AllO.UI.ViewModels;

public class LevelOption : ViewModelBase
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    private bool _isSelected = true;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}

public class NamedOption
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public override string ToString() => Name;
}

public class ViewManagerViewModel : ViewModelBase
{
    public ObservableCollection<LevelOption> Levels { get; } = new();
    public ObservableCollection<NamedOption> Templates { get; } = new();
    public ObservableCollection<NamedOption> TitleBlocks { get; } = new();
    public ObservableCollection<NamedOption> ScopeBoxes { get; } = new();

    public ViewManagerViewModel(
        IEnumerable<LevelOption> levels,
        IEnumerable<NamedOption> templates,
        IEnumerable<NamedOption> titleBlocks,
        IEnumerable<NamedOption> scopeBoxes)
    {
        foreach (var l in levels) Levels.Add(l);
        foreach (var t in templates) Templates.Add(t);
        foreach (var tb in titleBlocks) TitleBlocks.Add(tb);
        foreach (var sb in scopeBoxes) ScopeBoxes.Add(sb);
        SelectedTemplate = Templates.FirstOrDefault();
        SelectedTitleBlock = TitleBlocks.FirstOrDefault();
        SelectedScopeBox = ScopeBoxes.FirstOrDefault();

        SelectAllLevelsCommand = new RelayCommand(_ => SetLevels(true));
        SelectNoneLevelsCommand = new RelayCommand(_ => SetLevels(false));
        CreateCommand = new RelayCommand(_ => { DialogResult = true; CloseRequested?.Invoke(); });
        CancelCommand = new RelayCommand(_ => { DialogResult = false; CloseRequested?.Invoke(); });
    }

    // ── View kind (radio buttons, mutually exclusive via GroupName) ──
    private bool _isFloor = true;
    public bool IsFloor { get => _isFloor; set { if (SetProperty(ref _isFloor, value)) OnPropertyChanged(nameof(NamePreview)); RaiseLevelDependentChanged(); } }
    private bool _isCeiling;
    public bool IsCeiling { get => _isCeiling; set { if (SetProperty(ref _isCeiling, value)) OnPropertyChanged(nameof(NamePreview)); RaiseLevelDependentChanged(); } }
    private bool _isSection;
    public bool IsSection { get => _isSection; set { if (SetProperty(ref _isSection, value)) OnPropertyChanged(nameof(NamePreview)); RaiseLevelDependentChanged(); } }
    private bool _isElevation;
    public bool IsElevation { get => _isElevation; set { if (SetProperty(ref _isElevation, value)) OnPropertyChanged(nameof(NamePreview)); RaiseLevelDependentChanged(); } }
    private bool _is3D;
    public bool Is3D { get => _is3D; set { if (SetProperty(ref _is3D, value)) OnPropertyChanged(nameof(NamePreview)); RaiseLevelDependentChanged(); } }

    /// <summary>Las plantas usan niveles; sección/elevación/3D no.</summary>
    public bool UsesLevels => IsFloor || IsCeiling;
    private void RaiseLevelDependentChanged() => OnPropertyChanged(nameof(UsesLevels));

    private NamedOption? _selectedTemplate;
    public NamedOption? SelectedTemplate { get => _selectedTemplate; set => SetProperty(ref _selectedTemplate, value); }

    private NamedOption? _selectedTitleBlock;
    public NamedOption? SelectedTitleBlock { get => _selectedTitleBlock; set => SetProperty(ref _selectedTitleBlock, value); }

    private NamedOption? _selectedScopeBox;
    public NamedOption? SelectedScopeBox { get => _selectedScopeBox; set => SetProperty(ref _selectedScopeBox, value); }

    private string _prefix = "";
    public string Prefix { get => _prefix; set { if (SetProperty(ref _prefix, value)) OnPropertyChanged(nameof(NamePreview)); } }
    private string _suffix = "";
    public string Suffix { get => _suffix; set { if (SetProperty(ref _suffix, value)) OnPropertyChanged(nameof(NamePreview)); } }

    private bool _createSheets;
    public bool CreateSheets { get => _createSheets; set => SetProperty(ref _createSheets, value); }

    private string _baseSheetNumber = "VM-01";
    public string BaseSheetNumber { get => _baseSheetNumber; set => SetProperty(ref _baseSheetNumber, value); }

    public string NamePreview
    {
        get
        {
            string mid = IsFloor || IsCeiling ? "Level 1"
                : IsSection ? "Section"
                : IsElevation ? "North"
                : "3D";
            return $"{Prefix}{mid}{Suffix}";
        }
    }

    private void SetLevels(bool v) { foreach (var l in Levels) l.IsSelected = v; }

    public IEnumerable<long> SelectedLevelIds => Levels.Where(l => l.IsSelected).Select(l => l.Id);

    public bool? DialogResult { get; private set; }
    public event Action? CloseRequested;

    public ICommand SelectAllLevelsCommand { get; }
    public ICommand SelectNoneLevelsCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }
}
