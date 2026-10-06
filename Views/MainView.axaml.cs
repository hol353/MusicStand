using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace MusicStand;

/// <summary>
/// Represents the main view of the application.
/// </summary>
public partial class MainView : UserControl
{
    /// <summary>The PDF canvas control.</summary>
    //private PDFCanvas pdfCanvas;

    /// <summary>
    /// Constructor
    /// </summary>
    public MainView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The stamp button has been clicked. Decide whether to display the flyout.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnStampButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton button || button.IsChecked == true)
            return;

        button.IsChecked = true;
        var flyout = (Flyout)Resources["StampFlyout"]!;
        ((ListBox)flyout.Content!).DataContext = button.DataContext;
        flyout.ShowAt(button);
    }

    private void OnEditSetListClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel model)
            model.BeginSetListEdit();
    }

    /// <summary>
    /// Application is about to close. Save annotations.
    /// </summary>
    internal void OnClosing()
    {
        var model = DataContext as MainViewModel;
        model.SaveSetList();
        model.MusicLibrary.CloseAll();
        model.Settings.Save(model.BaseDirectory);
    }
}
