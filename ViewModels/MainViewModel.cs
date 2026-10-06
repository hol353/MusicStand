using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Media;
using ReactiveUI;

namespace MusicStand;

public class MainViewModel : ReactiveObject
{
    /// <summary>Name of application.</summary>
    private string applicationName;

    /// <summary>Is the app in pen mode?</summary>
    private bool _isPenMode;

    /// <summary>Is the app in eraser mode?</summary>
    private bool _isEraserMode;

    /// <summary>Is the app in highlighter mode?</summary>
    private bool _isHighlighterMode;

    /// <summary>Is the app in stamp mode?</summary>
    private bool _isStampMode;

    /// <summary>Is file select mode enabled?</summary>    
    private bool _isFileSelectMode;

    /// <summary>Is the current setlist being edited?</summary>
    private bool _isSetListEditing;

    /// <summary>
    /// Is the toolbar visible?
    /// </summary>    
    private bool _isToolbarVisible = true;

    /// <summary>
    /// The currently selected file.
    /// </summary>
    private FileItem _selectedFile;

    /// <summary>
    /// The currently selected colour.
    /// </summary>
    private ISolidColorBrush _color = Brushes.Red;
    private Stamp _selectedStamp;
    private string _filterLetter;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MainViewModel()
    {
        SolidColorBrushCollection =
        [
            Brushes.Red,
            Brushes.Yellow,
            Brushes.Black,
            Brushes.Green,
            Brushes.Cyan,
            Brushes.Orange,
            Brushes.Purple
        ];
        StampCollection =
        [
            new Stamp("sharp.svg"),
            new Stamp("flat.svg"),
            new Stamp("natural.svg"),
            new Stamp("ppp.svg"),
            new Stamp("pp.svg"),
            new Stamp("p.svg"),
            new Stamp("mp.svg"),
            new Stamp("mf.svg"),
            new Stamp("f.svg"),
            new Stamp("ff.svg"),
            new Stamp("fff.svg"),
            Stamp.CreateCrescendo(),
            Stamp.CreateDecrescendo()
        ];
        this.applicationName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
        Settings = SettingsModel.Create(BaseDirectory);
        _selectedStamp = StampCollection[Settings.SelectedStampIndex];

        MusicLibrary = new(this);
    }

    /// <summary>
    /// The music library instance.
    /// </summary>
    public MusicLibrary MusicLibrary { get; }

    /// <summary>
    /// Is the toolbar visible?
    /// </summary>
    public bool IsToolbarVisible
    {
        get => _isToolbarVisible;
        set
        {
            this.RaiseAndSetIfChanged(ref _isToolbarVisible, value);
            if (!IsToolbarVisible)
            {
                IsPenMode = false;
                IsEraserMode = false;
                IsHighlighterMode = false;
                IsStampMode = false;
                IsFileSelectMode = false;
            }
        }
    }

    /// <summary>
    /// Is pen mode enabled?
    /// </summary>
    public bool IsPenMode 
    {
        get => _isPenMode;
        set
        {
            if (value)
            {
                IsEraserMode = false;
                IsHighlighterMode = false;
                IsStampMode = false;
                IsFileSelectMode = false;
                SelectedBrush = SolidColorBrushCollection[Settings.SelectedPenColorIndex];
            }
            else if (value != _isPenMode)
                Settings.SelectedPenColorIndex = SolidColorBrushCollection.IndexOf(SelectedBrush);
            this.RaiseAndSetIfChanged(ref _isPenMode, value);
        }
    }

    /// <summary>
    /// Is eraser mode enabled?
    /// </summary>
    public bool IsEraserMode 
    {
        get => _isEraserMode;
        set
        {
            if (value)
            {
                IsPenMode = false;
                IsHighlighterMode = false;
                IsStampMode = false;
                IsFileSelectMode = false;
            }
            this.RaiseAndSetIfChanged(ref _isEraserMode, value);
        }
    }

    /// <summary>
    /// Is highlighter mode enabled?
    /// </summary>
    public bool IsHighlighterMode
    {
        get => _isHighlighterMode;
        set
        {
            if (value)
            {
                IsPenMode = false;
                IsEraserMode = false;
                IsStampMode = false;
                IsFileSelectMode = false;
                SelectedBrush = SolidColorBrushCollection[Settings.SelectedHighlighterColorIndex];
            }
            else if (value != _isHighlighterMode)
                Settings.SelectedHighlighterColorIndex = SolidColorBrushCollection.IndexOf(SelectedBrush);
            this.RaiseAndSetIfChanged(ref _isHighlighterMode, value);
        }
    }

    /// <summary>
    /// Is stamp mode enabled?
    /// </summary>
    public bool IsStampMode
    {
        get => _isStampMode;
        set
        {
            if (value)
            {
                IsPenMode = false;
                IsEraserMode = false;
                IsHighlighterMode = false;
                IsFileSelectMode = false;
            }
            this.RaiseAndSetIfChanged(ref _isStampMode, value);
        }
    }


    /// <summary>
    /// Is file select mode enabled?
    /// </summary>
    public bool IsFileSelectMode 
    {
        get => _isFileSelectMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isFileSelectMode, value);
            if (IsFileSelectMode)
            {
                IsPenMode = false;
                IsEraserMode = false;
                IsHighlighterMode = false;
            }
        }
    }

    /// <summary>
    /// Whether the selected file can be edited as a setlist.
    /// </summary>
    public bool CanEditSetList =>
        !_isSetListEditing &&
        _selectedFile != null &&
        string.Equals(Path.GetExtension(_selectedFile.AbsolutePath), ".txt", StringComparison.OrdinalIgnoreCase) &&
        File.Exists(_selectedFile.AbsolutePath);

    /// <summary>
    /// Whether the setlist editor is currently displayed.
    /// </summary>
    public bool IsSetListEditing
    {
        get => _isSetListEditing;
        private set
        {
            if (_isSetListEditing != value)
            {
                this.RaiseAndSetIfChanged(ref _isSetListEditing, value);
                this.RaisePropertyChanged(nameof(CanEditSetList));
                this.RaisePropertyChanged(nameof(IsPDFCanvasVisible));
            }
        }
    }

    /// <summary>
    /// Whether the PDF canvas should be displayed.
    /// </summary>
    public bool IsPDFCanvasVisible => !_isSetListEditing;

    /// <summary>
    /// The ordered files in the setlist being edited.
    /// </summary>
    public ObservableCollection<FileItem> SetListFiles { get; } = new();

    /// <summary>
    /// The currently selected directory (relative to BasePath).
    /// </summary>
    public string SelectedDirectory 
    { 
        get => Settings.SelectedDirectory; 
        set 
        { 
            if (value != Settings.SelectedDirectory)
            {
                this.RaisePropertyChanging("SelectedDirectory");
                Settings.SelectedDirectory = value;
                MusicLibrary.ReadFiles(); 
                this.RaisePropertyChanged("SelectedDirectory");
            }
        } 
    }

    /// <summary>
    /// The currently selected file (relative to SelectedDirectory).
    /// </summary>
    public FileItem SelectedFile 
    { 
        get => _selectedFile; 
        set 
        {
            if (value?.IsCreateSetListCommand == true)
            {
                this.RaisePropertyChanged(nameof(SelectedFile));
                return;
            }

            if (_isSetListEditing)
            {
                if (value != null)
                    AddFileToSetList(value);
                this.RaisePropertyChanged(nameof(SelectedFile));
                return;
            }

            this.RaiseAndSetIfChanged(ref _selectedFile, value);
            this.RaisePropertyChanged(nameof(CanEditSetList));
            if (value != null)
                IsToolbarVisible = false;
        }
    }    

    /// <summary>
    /// The currently selected stamp.
    /// </summary>
    public Stamp SelectedStamp
    { 
        get => _selectedStamp; 
        set 
        {
            this.RaiseAndSetIfChanged(ref _selectedStamp, value);
            if (value != null)
                Settings.SelectedStampIndex = StampCollection.IndexOf(SelectedStamp);
        }
    } 

    /// <summary>
    /// Collection of colour brushes/
    /// </summary>
    public ObservableCollection<ISolidColorBrush> SolidColorBrushCollection { get; }

    /// <summary>
    /// The currently selected colour.
    /// </summary>
    public ISolidColorBrush SelectedBrush
    {
        get => _color;
        set => this.RaiseAndSetIfChanged(ref _color, value);
    }

    /// <summary>
    /// Collection of stamps
    /// </summary>
    public ObservableCollection<Stamp> StampCollection { get; }

    /// <summary>
    /// A letter (A-Z) to filter the file list.
    /// </summary>
    public string FilterLetter
    {
        get => _filterLetter;
        set => this.RaiseAndSetIfChanged(ref _filterLetter, value);
    }

    /// <summary>
    /// The directory where the application stores settings/annotations.
    /// </summary>
    public string BaseDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), applicationName );

    /// <summary>
    /// Loads the selected setlist into the editor.
    /// </summary>
    public void BeginSetListEdit()
    {
        if (!CanEditSetList)
            return;

        IsPenMode = false;
        IsEraserMode = false;
        IsHighlighterMode = false;
        IsStampMode = false;
        IsFileSelectMode = false;

        SetListFiles.Clear();
        foreach (string filePath in File.ReadAllLines(_selectedFile.AbsolutePath))
            if (!string.IsNullOrWhiteSpace(filePath))
                SetListFiles.Add(new FileItem(filePath, MusicLibrary.BasePath));

        IsSetListEditing = true;
    }

    /// <summary>
    /// Saves the edited order and returns to the PDF canvas.
    /// </summary>
    public void FinishSetListEdit()
    {
        if (!IsSetListEditing)
            return;

        SaveSetList();
        IsSetListEditing = false;
        this.RaisePropertyChanged(nameof(SelectedFile));
    }

    /// <summary>
    /// Saves the current setlist if it is being edited.
    /// </summary>
    public void SaveSetList()
    {
        if (IsSetListEditing)
            File.WriteAllLines(_selectedFile.AbsolutePath, SetListFiles.Select(file => file.AbsolutePath));
    }

    /// <summary>
    /// Creates a setlist in the Set Lists directory and opens it in the editor.
    /// </summary>
    public bool TryCreateSetList(string fileName, out string error)
    {
        error = null;
        fileName = fileName?.Trim();
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            fileName.IndexOfAny("<>:\"/\\|?*".ToCharArray()) >= 0 ||
            !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal) ||
            Path.HasExtension(fileName))
        {
            error = "Enter a valid filename without an extension.";
            return false;
        }

        string directory = Path.Combine(MusicLibrary.BasePath, MusicLibrary.SetListsDirectoryName);
        string path = Path.Combine(directory, fileName + ".txt");
        if (File.Exists(path))
        {
            error = "A setlist with that filename already exists.";
            return false;
        }

        SaveSetList();
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, string.Empty);
        IsSetListEditing = false;

        SelectedDirectory = MusicLibrary.SetListsDirectoryName;
        MusicLibrary.ReadFiles();
        SelectedFile = new FileItem(path, MusicLibrary.BasePath);
        BeginSetListEdit();
        return true;
    }

    /// <summary>
    /// Adds a PDF from the music library to the current setlist.
    /// </summary>
    public void AddFileToSetList(FileItem file)
    {
        if (IsSetListEditing &&
            string.Equals(Path.GetExtension(file.AbsolutePath), ".pdf", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(file.AbsolutePath))
        {
            SetListFiles.Add(file);
        }
    }

    /// <summary>
    /// Moves a setlist entry to a new position.
    /// </summary>
    public void MoveSetListFile(int oldIndex, int newIndex)
    {
        if (oldIndex >= 0 && oldIndex < SetListFiles.Count &&
            newIndex >= 0 && newIndex < SetListFiles.Count &&
            oldIndex != newIndex)
        {
            SetListFiles.Move(oldIndex, newIndex);
        }
    }

    /// <summary>
    /// The application settings
    /// </summary>
    public SettingsModel Settings { get; }

}
