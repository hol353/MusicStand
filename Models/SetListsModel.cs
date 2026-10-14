using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using ReactiveUI;

namespace MusicStand;

public class SetListsModel : ReactiveObject
{
    private MainViewModel model;
    private string _selectedName;
    private List<string> selectedSetListFilePaths = new();
    private string baseDirectory;
    private ObservableCollection<string> _selectedSetListTitles = new();
    private bool _isFileListVisible;
    private string _newSetListName;
    private bool _isCreatingNewSetList;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetListsModel"/> class.
    /// </summary>
    /// <param name="model">The main view model.</param>
    public SetListsModel(MainViewModel model)
    {
        this.model = model; 
        _selectedName = model.Settings.SelectedSetList; 
        baseDirectory = Path.Combine(model.Settings.MusicLibraryBaseDirectory, "Set Lists");
        Directory.CreateDirectory(baseDirectory);
        Refresh();
        LoadSelectedFilePaths();
    }


    /// <summary>
    /// The ordered names of set lists.
    /// </summary>
    public ObservableCollection<string> SetListNames { get; } = new();

    /// <summary>
    /// The currently selected setlist name.
    /// </summary>
    public string SelectedName 
    { 
        get => _selectedName; 
        set 
        {
            if (_selectedName != value)
            {
                this.RaiseAndSetIfChanged(ref _selectedName, value);
                model.Settings.SelectedSetList = value;
                LoadSelectedFilePaths();
            }
        }
    } 


    /// <summary>
    /// The names of the files in the currently selected setlist (without extensions).
    /// </summary>
    public ObservableCollection<string> SelectedSetListTitles => _selectedSetListTitles;
    
    /// <summary>
    /// The currently selected setlist absolute file path.
    /// </summary>
    private string SelectedPath => Path.Combine(baseDirectory, $"{SelectedName}.txt");

    /// <summary>
    /// Indicates whether the file list is visible in the UI.
    /// </summary>
    public bool IsFileListVisible
    {
        get => _isFileListVisible;
        set 
        {
            this.RaiseAndSetIfChanged(ref _isFileListVisible, value);
        }
    } 

    /// <summary>
    /// Indicates whether a new setlist is being created.
    /// </summary>
    public bool IsCreatingNewSetList 
    { 
        get => _isCreatingNewSetList;
        set => this.RaiseAndSetIfChanged(ref _isCreatingNewSetList, value);
    }

    /// <summary>
    /// The name of the new setlist being created. Setting this property will create a new setlist 
    /// if the name is valid and does not already exist.
    /// </summary>
    public string NewSetListName 
    { 
        get => _newSetListName;
        set
        {
          this.RaiseAndSetIfChanged(ref _newSetListName, value);  
          CreateNewSetList(_newSetListName);
        } 
    }
    
    /// <summary>
    /// Moves a title within the selected setlist from one index to another and saves the updated setlist.
    /// </summary>
    /// <param name="oldIndex"></param>
    /// <param name="newIndex"></param>
    public void MoveTitle(int oldIndex, int newIndex)
    {
        // Move the path in selectedSetListFilePaths
        if (oldIndex >= 0 && oldIndex < selectedSetListFilePaths.Count && newIndex >= 0 && newIndex < selectedSetListFilePaths.Count)
        {
            var item = selectedSetListFilePaths[oldIndex];
            selectedSetListFilePaths.RemoveAt(oldIndex);
            selectedSetListFilePaths.Insert(newIndex, item);    
        }
        RefreshSelectedSetListTitles();
        SaveSelectedFilePaths();
    }

    /// <summary>
    /// Adds a file to the currently selected setlist if it is not already present, refreshes the titles, and saves the updated setlist.
    /// </summary>
    /// <param name="fileItem"></param>
    public void AddToSetList(FileItem fileItem)
    {
        if (fileItem != null && !selectedSetListFilePaths.Contains(fileItem.AbsolutePath))
        {
            selectedSetListFilePaths.Add(fileItem.AbsolutePath);
            RefreshSelectedSetListTitles();
            SaveSelectedFilePaths();
        }
    }

    /// <summary>
    /// Deletes a file from the currently selected setlist at the specified index, refreshes the titles, and saves the updated setlist.
    /// If the index is out of bounds, no action is taken.
    /// </summary>
    /// <param name="index"></param>
    public void DeleteFile(int index)
    {
        if (index >= 0 && index < selectedSetListFilePaths.Count)
        {
            selectedSetListFilePaths.RemoveAt(index);
            RefreshSelectedSetListTitles();
            SaveSelectedFilePaths();
        }
    }

    /// <summary>
    /// Shows the setlist music to the user. 
    /// </summary>
    public void ShowMusic()
    {
        model.SelectedFile = new FileItem(SelectedPath, model.MusicLibrary.BasePath);
        model.IsSetListMode = false;
    }

    /// <summary>
    /// Shows a single title from the setlist to the user. 
    /// </summary>
    public void ShowMusicTitle(int titleIndex)
    {
        if (titleIndex >= 0 && titleIndex < selectedSetListFilePaths.Count)
        {
            model.SelectedFile = new FileItem(selectedSetListFilePaths[titleIndex], model.MusicLibrary.BasePath);
            model.IsSetListMode = false;
        }
    }

    /// <summary>
    /// Refreshes the list of set lists.
    /// </summary>
    private void Refresh()
    {
        // Clear the existing names before refreshing
        SetListNames.Clear();
        foreach (var dir in Directory.GetFiles(baseDirectory).OrderDescending())
            SetListNames.Add(Path.GetFileNameWithoutExtension(dir));
    }

    /// <summary>
    /// Refreshes the list of file paths for the currently selected setlist.
    /// </summary>
    private void LoadSelectedFilePaths()
    {
        selectedSetListFilePaths.Clear();
        if (string.IsNullOrEmpty(SelectedName))
            return;
        
        if (File.Exists(SelectedPath))
        {
            var fileNames = File.ReadAllLines(SelectedPath);
            foreach (var fileName in fileNames)
                selectedSetListFilePaths.Add(fileName);
        }
        RefreshSelectedSetListTitles();
    }

    /// <summary>
    /// Refreshes the list of titles for the currently selected setlist.
    /// </summary>
    private void RefreshSelectedSetListTitles()
    {
        SelectedSetListTitles.Clear();
        foreach (var filePath in selectedSetListFilePaths)
            SelectedSetListTitles.Add(Path.GetFileNameWithoutExtension(filePath));
    }

    /// <summary>
    /// Creates a new setlist with the specified name if it is valid and does not already exist.
    /// </summary>
    /// <param name="newSetListName"></param>
    private void CreateNewSetList(string newSetListName)
    {
        if (!string.IsNullOrWhiteSpace(newSetListName))
        {
            string newSetListPath = Path.Combine(baseDirectory, $"{newSetListName}.txt");
            if (!File.Exists(newSetListPath))
            {
                File.WriteAllText(newSetListPath, string.Empty);
                Refresh();
                SelectedName = newSetListName;
                IsCreatingNewSetList = false;
            }
        }
    }

    /// <summary>
    /// Saves the current setlist if it is being edited.
    /// </summary>
    private void SaveSelectedFilePaths()
    {
        File.WriteAllLines(SelectedPath, selectedSetListFilePaths);
    }    
}