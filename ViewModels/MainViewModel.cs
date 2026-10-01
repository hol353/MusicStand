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
            Brushes.Blue,
            Brushes.Orange,
            Brushes.Purple
        ];
        StampCollection =
        [
            new Stamp("Flat.svg")
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
            this.RaiseAndSetIfChanged(ref _selectedFile, value);
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
    /// The directory where the application stores settings/annotations.
    /// </summary>
    public string BaseDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), applicationName );

    /// <summary>
    /// The application settings
    /// </summary>
    public SettingsModel Settings { get; }

}
