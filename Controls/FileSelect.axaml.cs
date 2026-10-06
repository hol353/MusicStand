using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace MusicStand;

/// <summary>
/// Represents the main view of the application.
/// </summary>
public partial class FileSelect : UserControl
{
    private MainViewModel model;

    /// <summary>
    /// Constructor
    /// </summary>
    public FileSelect()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the control is loaded.
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        model = DataContext as MainViewModel;
    }    

    /// <summary>
    /// User has clicked a navigation button.
    /// </summary>
    private void OnNavigationButtonClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as ToggleButton;
        char? letter = null;
        if ((bool)button.IsChecked)
            letter = button.Content.ToString().First();
        model.MusicLibrary.FilterFiles(letter);
        ListBox.ScrollIntoView(0);
    }    

    /// <summary>
    /// User has changed the file name filter.
    /// </summary>
    private void OnFileNameFilterChanged(object sender, TextChangedEventArgs e)
    {
        model?.MusicLibrary.FilterFilesByName(FileNameFilter.Text);
    }

    private async void OnFileSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListBox.SelectedItem is not FileItem { IsCreateSetListCommand: true })
            return;

        ListBox.SelectedItem = null;
        if (DataContext is not MainViewModel viewModel || TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var dialog = new CreateSetListWindow(viewModel);
        await dialog.ShowDialog<bool>(owner);
    }
}
