using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace MusicStand;

/// <summary>
/// Displays the library beside the ordered entries in the current setlist.
/// </summary>
public partial class SetLists : UserControl
{
    private int draggedIndex = -1;
    private int dropIndex = -1;
    private Point pointerStart;
    private bool isDragging;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetLists"/> class.
    /// </summary>
    public SetLists()
    {
        InitializeComponent();
        DropIndicator.RenderTransform = new TranslateTransform();
        SetListFilesList.AddHandler(InputElement.PointerPressedEvent, OnSetListPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        SetListFilesList.AddHandler(InputElement.PointerMovedEvent, OnSetListPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        SetListFilesList.AddHandler(InputElement.PointerReleasedEvent, OnSetListPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>
    /// Handles the PointerPressed event for the SetListFilesList ListBox. 
    /// If the left mouse button is pressed on a ListBoxItem, it initiates a drag operation by 
    /// storing the index of the dragged item and the initial pointer position.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnSetListPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var listBoxItem = FindListBoxItem(e.Source);
        if (listBoxItem == null || !e.GetCurrentPoint(SetListFilesList).Properties.IsLeftButtonPressed)
            return;

        draggedIndex = (DataContext as MainViewModel).SetLists.SelectedSetListTitles.IndexOf(listBoxItem.Content.ToString());
        pointerStart = e.GetPosition(SetListFilesList);
        isDragging = false;
    }

    /// <summary>
    /// Handles the PointerMoved event for the SetListFilesList ListBox. If a drag operation is in progress, it updates the drop indicator position based on the current pointer position.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnSetListPointerMoved(object sender, PointerEventArgs e)
    {
        if (draggedIndex < 0 || !e.GetCurrentPoint(SetListFilesList).Properties.IsLeftButtonPressed)
            return;

        Point position = e.GetPosition(SetListFilesList);
        if (Math.Abs(position.X - pointerStart.X) >= 4 || Math.Abs(position.Y - pointerStart.Y) >= 4)
            isDragging = true;

        if (isDragging)
            UpdateDropIndicator(position);
    }

    /// <summary>
    /// Handles the PointerReleased event for the SetListFilesList ListBox. If a drag operation is in progress 
    /// and the pointer is released within the bounds of the ListBox, it moves the dragged item to the new position in the list.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnSetListPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        var model = DataContext as MainViewModel;
        Point dropPoint = e.GetPosition(SetListFilesList);
        bool isInsideList = dropPoint.X >= 0 && dropPoint.X <= SetListFilesList.Bounds.Width &&
                            dropPoint.Y >= 0 && dropPoint.Y <= SetListFilesList.Bounds.Height;
        if (!isInsideList)
        {
            model.SetLists.DeleteFile(draggedIndex);
        }                            
        else if (isDragging && isInsideList && draggedIndex >= 0 && dropIndex >= 0 && DataContext is MainViewModel)
        {
            int newIndex = dropIndex;
            if (newIndex > draggedIndex)
                newIndex--;

            model.SetLists.MoveTitle(draggedIndex, newIndex);
        }
        else if (draggedIndex >= 0 && DataContext is MainViewModel)
            model.SetLists.ShowMusicTitle(draggedIndex);

        draggedIndex = -1;
        dropIndex = -1;
        isDragging = false;
        DropIndicator.IsVisible = false;
    }

    /// <summary>
    /// Updates the position of the drop indicator based on the current pointer position.
    /// It calculates the appropriate index for the drop indicator and sets its Y position accordingly.
    /// </summary>
    /// <param name="position"></param>
    private void UpdateDropIndicator(Point position)
    {
        var model = DataContext as MainViewModel;
        var targetContainer = FindListBoxItem(SetListFilesList.InputHitTest(position) as Visual);
        double indicatorY;
        dropIndex = model.SetLists.SelectedSetListTitles.Count;

        if (targetContainer != null)
        {
            int targetIndex = SetListFilesList.IndexFromContainer(targetContainer);
            if (targetIndex >= 0)
            {
                Point containerPosition = targetContainer.TranslatePoint(new Point(0, 0), SetListFilesList) ?? new Point(0, position.Y);
                bool insertAfter = position.Y >= containerPosition.Y + targetContainer.Bounds.Height / 2;
                dropIndex = targetIndex + (insertAfter ? 1 : 0);
                indicatorY = containerPosition.Y + (insertAfter ? targetContainer.Bounds.Height : 0);
            }
            else
            {
                indicatorY = position.Y;
            }
        }
        else
        {
            var lastContainer = model.SetLists.SelectedSetListTitles.Count > 0
                ? SetListFilesList.ContainerFromIndex(model.SetLists.SelectedSetListTitles.Count - 1) as ListBoxItem
                : null;
            if (lastContainer != null)
            {
                Point containerPosition = lastContainer.TranslatePoint(new Point(0, 0), SetListFilesList) ?? new Point(0, position.Y);
                indicatorY = containerPosition.Y + lastContainer.Bounds.Height;
            }
            else
            {
                indicatorY = position.Y;
            }
        }

        ((TranslateTransform)DropIndicator.RenderTransform).Y = indicatorY - 1.5;
        DropIndicator.IsVisible = true;
    }

    /// <summary>
    /// Finds the ListBoxItem associated with a given visual element or null if not found.
    /// </summary>
    /// <param name="source"></param>
    private static ListBoxItem FindListBoxItem(object obj)
    {
         if (obj is not Visual visual)
            return null;
        for (Visual current = visual; current != null; current = current.GetVisualParent())
            if (current is ListBoxItem item)
                return item;

        return null;
    }
}
