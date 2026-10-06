using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MusicStand;

/// <summary>
/// Prompts for a filename and creates a setlist.
/// </summary>
public partial class CreateSetListWindow : Window
{
    private MainViewModel model;

    public CreateSetListWindow()
    {
        InitializeComponent();
    }

    public CreateSetListWindow(MainViewModel model) : this()
    {
        this.model = model;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (model.TryCreateSetList(FileNameTextBox.Text, out string error))
        {
            Close(true);
            return;
        }

        ErrorTextBlock.Text = error;
        ErrorTextBlock.IsVisible = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnFileNameKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            OnOkClick(sender, new RoutedEventArgs());
    }
}
