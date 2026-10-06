using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace MusicStand;

/// <summary>
/// Displays the library beside the ordered entries in the current setlist.
/// </summary>
public partial class SetListEditor : UserControl
{
    private int draggedIndex = -1;
    private int dropIndex = -1;
    private Point pointerStart;
    private bool isDragging;

    public SetListEditor()
    {
        InitializeComponent();
        DropIndicator.RenderTransform = new TranslateTransform();
        SetListFilesList.AddHandler(InputElement.PointerPressedEvent, OnSetListPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        SetListFilesList.AddHandler(InputElement.PointerMovedEvent, OnSetListPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        SetListFilesList.AddHandler(InputElement.PointerReleasedEvent, OnSetListPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnDoneClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel model)
            model.FinishSetListEdit();
    }

    private void OnSetListPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(SetListFilesList).Properties.IsLeftButtonPressed)
            return;

        var file = FindFileItem(e.Source);
        draggedIndex = DataContext is MainViewModel model && file != null
            ? model.SetListFiles.IndexOf(file)
            : -1;
        pointerStart = e.GetPosition(SetListFilesList);
        isDragging = false;
    }

    private void OnSetListPointerMoved(object sender, PointerEventArgs e)
    {
        if (draggedIndex < 0 || !e.GetCurrentPoint(SetListFilesList).Properties.IsLeftButtonPressed)
            return;

        Point position = e.GetPosition(SetListFilesList);
        if (Math.Abs(position.X - pointerStart.X) >= 4 || Math.Abs(position.Y - pointerStart.Y) >= 4)
            isDragging = true;

        if (isDragging && DataContext is MainViewModel model)
            UpdateDropIndicator(position, model);
    }

    private void OnSetListPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        Point dropPoint = e.GetPosition(SetListFilesList);
        bool isInsideList = dropPoint.X >= 0 && dropPoint.X <= SetListFilesList.Bounds.Width &&
                            dropPoint.Y >= 0 && dropPoint.Y <= SetListFilesList.Bounds.Height;
        if (isDragging && isInsideList && draggedIndex >= 0 && dropIndex >= 0 && DataContext is MainViewModel model)
        {
            int newIndex = dropIndex;
            if (newIndex > draggedIndex)
                newIndex--;

            model.MoveSetListFile(draggedIndex, newIndex);
        }

        draggedIndex = -1;
        dropIndex = -1;
        isDragging = false;
        DropIndicator.IsVisible = false;
    }

    private void UpdateDropIndicator(Point position, MainViewModel model)
    {
        var targetContainer = FindListBoxItem(SetListFilesList.InputHitTest(position) as Visual);
        double indicatorY;
        dropIndex = model.SetListFiles.Count;

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
            var lastContainer = model.SetListFiles.Count > 0
                ? SetListFilesList.ContainerFromIndex(model.SetListFiles.Count - 1) as ListBoxItem
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

    private static FileItem FindFileItem(object source)
    {
        if (source is not Visual visual)
            return null;

        for (Visual current = visual; current != null; current = current.GetVisualParent())
            if (current is ListBoxItem item)
                return item.DataContext as FileItem;

        return null;
    }

    private static ListBoxItem FindListBoxItem(Visual visual)
    {
        for (Visual current = visual; current != null; current = current.GetVisualParent())
            if (current is ListBoxItem item)
                return item;

        return null;
    }
}
