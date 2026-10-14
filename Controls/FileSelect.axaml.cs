using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MusicStand;

/// <summary>
/// Represents the main view of the application.
/// </summary>
public partial class FileSelect : UserControl
{
    /// <summary>
    /// Constructor
    /// </summary>
    public FileSelect()
    {
        InitializeComponent();
    }

    /// <summary>
    /// User has clicked a navigation button.
    /// </summary>
    private void OnNavigationButtonClicked(object sender, RoutedEventArgs e)
    {
        string letter = (sender as Button).Content.ToString();
        object selectedItem = FileListBox.Items.Cast<FileItem>().FirstOrDefault(file => file.StartsWith(letter));
        if (selectedItem != null)
        {
            FileListBox.ScrollIntoView(FileListBox.Items.Last());
            FileListBox.ScrollIntoView(selectedItem);
        }
    }

    /// <summary>
    /// User has clicked a file in the file list. If in file select mode, the selected file is set. If in setlist mode, the file is added to the current setlist.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnFileListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var model = DataContext as MainViewModel;
        if (FileListBox.SelectedItem is FileItem fileItem)
        {
            if (model.IsFileSelectMode)
                model.SelectedFile = fileItem;
            else if (model.IsSetListMode)
                model.SetLists.AddToSetList(fileItem);
        }
    }
}