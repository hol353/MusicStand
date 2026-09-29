using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
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
        var button = sender as Button;
        char letter = button.Content.ToString().First();
        model.MusicLibrary.FilterFiles(letter);
        ListBox.ScrollIntoView(0);
    }    
}
