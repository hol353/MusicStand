using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Erasing;
using ReactiveUI;
using SkiaSharp;

namespace MusicStand;

/// <summary>
/// Represents the main view of the application.
/// </summary>
public partial class PDFCanvas : UserControl
{
    private MainViewModel model;
    private ZoomBorder zoomBorder;
    private StackPanel musicCanvas;
    int currentPageIndex = -1;

    /// <summary>The time when the user started panning. Used to determine if a tap is a pan or a single tap.</summary>
    private DateTime panStartTime = DateTime.MinValue;

    /// <summary>The time the user spent panning. Used to determine if a tap is a pan or a single tap.</summary>
    private TimeSpan panTime;

    /// <summary>
    /// Constructor
    /// </summary>
    public PDFCanvas()
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
        model.WhenAnyValue(x => x.SelectedFile).Subscribe(file => LoadFile(file));
        model.WhenAnyValue(x => x.IsPenMode).Subscribe(_ => UpdateAnnotateMode());
        model.WhenAnyValue(x => x.IsEraserMode).Subscribe(_ => UpdateAnnotateMode());
        model.WhenAnyValue(x => x.IsHighlighterMode).Subscribe(_ => UpdateAnnotateMode());
        model.WhenAnyValue(x => x.IsStampMode).Subscribe(_ => UpdateAnnotateMode());
        model.WhenAnyValue(x => x.SelectedBrush).Subscribe(brush => SetAnnotateBrush(brush));
    }

    /// <summary>
    /// Load a pdf or txt file.
    /// </summary>
    /// <param name="file"></param>
    private void LoadFile(FileItem file)
    {
        if (file != null)
        {
            // The file can be either .pdf or .txt (setlist).
            if (File.Exists(file.AbsolutePath))
                Load([file.AbsolutePath]);
            else
            {
                // Handle .txt file case
                var absoluteTxtFilePath = Path.ChangeExtension(file.AbsolutePath, ".txt");
                if (File.Exists(absoluteTxtFilePath))
                    Load(File.ReadAllLines(absoluteTxtFilePath));
            }
            CalculatePageIndexOfCurrentPage();
        }
    }

    /// <summary>
    /// Load multiple pdf files.
    /// </summary>
    /// <param name="pdfFilePaths">The paths to the pdf files.</param>
    private void Load(IEnumerable<string> pdfFilePaths)
    {
        model.MusicLibrary.CloseAll();
        musicCanvas.Children.Clear();
        foreach (var pdfFilePath in pdfFilePaths)
            Load(pdfFilePath);
    }

    /// <summary>
    /// Gets or sets the current editing mode of the InkCanvas.
    /// </summary>
    private void SetAnnotateMode(InkCanvasEditingMode mode)
    {
        foreach (InkCanvas inkCanvas in musicCanvas.Children)
        {
            inkCanvas.EditingMode = mode;
            ApplyInkSettings(inkCanvas);
            inkCanvas.InvalidateVisual();
        }

        if (mode == InkCanvasEditingMode.None && musicCanvas.Children.Count > 0)
        {
        }
        else
            PanOff();
    }

    private void UpdateAnnotateMode()
    {
        if (model.IsHighlighterMode || model.IsPenMode)
            SetAnnotateMode(InkCanvasEditingMode.Ink);
        else if (model.IsEraserMode)
            SetAnnotateMode(InkCanvasEditingMode.EraseByPoint);
        else if (model.IsStampMode)
            SetAnnotateMode(InkCanvasEditingMode.None);
        else
            SetAnnotateMode(InkCanvasEditingMode.None);
    }

    private void ApplyInkSettings(InkCanvas inkCanvas)
    {
        var settings = inkCanvas.AvaloniaSkiaInkCanvas.Settings;
        var colour = settings.InkColor;
        settings.InkThickness = model.IsHighlighterMode ? 40 : 4;
        settings.InkColor = model.IsHighlighterMode
            ? new SKColor(colour.Red, colour.Green, colour.Blue, Math.Min(colour.Alpha, (byte)128))
            : new SKColor(colour.Red, colour.Green, colour.Blue, model.SelectedBrush.Color.A);
    }

    /// <summary>
    /// Sets the annotation brush
    /// </summary>
    /// <param name="colour">The ink colour</param>
    public void SetAnnotateBrush(ISolidColorBrush solidColorBrush)
    {
        if (solidColorBrush != null)
        {
            SKColor colour = new SKColor(solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B, solidColorBrush.Color.A);
            foreach (InkCanvas inkCanvas in musicCanvas.Children)
            {
                inkCanvas.AvaloniaSkiaInkCanvas.Settings.InkColor = colour;
                ApplyInkSettings(inkCanvas);
                inkCanvas.InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// Invoked when the control is attached to the visual tree.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder");
        musicCanvas = this.FindControl<StackPanel>("MusicCanvas");

        //zoomBorder.ResizeBehavior = ResizeBehaviorMode.ReapplyStretch;
        zoomBorder.SizeChanged += OnZoomBorderSizeChanged;
        zoomBorder.PanStarted += OnPanStarted;
        zoomBorder.PanEnded += OnPanEnded;
        zoomBorder.DoubleClickZoomMode = DoubleClickZoomMode.None;
        zoomBorder.ZoomDeltaChanged += (s, e) => OnZoomChanged(null, null);
        this.Tapped += OnSingleTap;
    }
 
    /// <summary>
    /// Invoked when the control is detached from the visual tree.
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        zoomBorder.SizeChanged -= OnZoomBorderSizeChanged;
        zoomBorder.PanStarted -= OnPanStarted;
        zoomBorder.PanEnded -= OnPanEnded;
        this.Tapped -= OnSingleTap;
        base.OnDetachedFromVisualTree(e);
        zoomBorder = null;
    }

    /// <summary>
    /// Load a pdf file.
    /// </summary>
    /// <param name="pdfFilePath">The path of the pdf file.</param>
    private void Load(string pdfFilePath)
    {
        try
        {
            model.MusicLibrary.Open(pdfFilePath);
            
            foreach (var file in model.MusicLibrary.OpenFiles)
            {
                foreach (var page in file.Pages)
                {
                    var sheetMusicControl = new PDFPageCanvas(this, page, model);
                    sheetMusicControl.AvaloniaSkiaInkCanvas.Settings.EraserViewCreator = new DelegateEraserViewCreator(() => new CustomEraserView());
                    sheetMusicControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                    sheetMusicControl.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                    sheetMusicControl.AvaloniaSkiaInkCanvas.Settings.IgnorePressure = true;
                    musicCanvas.Children.Add(sheetMusicControl);
                }
            }
            // Pan back to the origin instead.
            zoomBorder.PanDelta(0, -zoomBorder.OffsetY);
            UpdateAnnotateMode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering PDF: {ex.Message}");
        }
    }    

    /// <summary>
    /// Invoked when the size of the ZoomBorder changes. Resizes all child controls to fit the new size.
    /// </summary>
    private void OnZoomBorderSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ResizeChildren(e.NewSize, e.PreviousSize);
    }

    /// <summary>
    /// Resizes all child controls to fit the new size of the ZoomBorder.
    /// </summary>
    private void ResizeChildren(Avalonia.Size newSize, Avalonia.Size oldSize)
    {
        // Need to resize the children to fit the new size of the ZoomBorder. This is necessary because the 
        // StackPanel does not always automatically resize its children when it is resized.
        foreach (PDFPageCanvas page in musicCanvas.Children)
        {
            page.Width = newSize.Width;
            page.Height = newSize.Height;
        }
        PanInYDirectionOnly(newSize.Height);

        // After resizing, pan back to the same page that was being displayed before resizing.
        if (currentPageIndex >= 0 && currentPageIndex < musicCanvas.Children.Count)
        {
            // Need to calculate an OffsetY by calling page.CalculateRenderRectangle with the newSize.
            // Can't just use page.Bounds or page.ContainsYPoint because the page hasn't been rendered yet
            // and the Bounds won't be the final bounds after resizing.
            double newOffsetY = 0;
            for (int i = 0; i < currentPageIndex; i++)
            {
                Rect r = (musicCanvas.Children[i] as PDFPageCanvas).CalculateRenderRectangle(newSize.Width, newSize.Height);
                newOffsetY += r.Height + musicCanvas.Spacing;
            }

            double deltaPan = -newOffsetY - zoomBorder.OffsetY;
            zoomBorder.PanDelta(0, deltaPan);
        }
    } 

    /// <summary>
    /// Invoked when the user starts panning. Sets the min and max offsets for the ZoomBorder based on the current zoom level and the height of the music canvas.
    /// </summary>
    private void OnPanStarted(object sender, PanEventArgs e)
    {
        if (!model.IsPenMode && !model.IsEraserMode && !model.IsStampMode && musicCanvas.Children.Count > 0)
        {
            panStartTime = DateTime.Now;
            if (Math.Round(zoomBorder.ZoomX, 2) == 1 && Math.Round(zoomBorder.ZoomY, 2) == 1)  
                PanInYDirectionOnly(); // No zoom applied. Pan in Y direction only
        }
    }

    /// <summary>
    /// Invoked when the user ends panning. Records the time spent panning to determine if a tap is a pan or a single tap.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnPanEnded(object sender, PanEventArgs e)
    {
        if (!model.IsPenMode && !model.IsEraserMode && !model.IsStampMode && musicCanvas.Children.Count > 0)
            panTime = DateTime.Now - panStartTime;
        CalculatePageIndexOfCurrentPage();
    }

    /// <summary>
    /// Invoked when the user changes zooming.
    /// </summary>
    private void OnZoomChanged(object sender, ZoomEventArgs e)
    {
        if (Math.Round(zoomBorder.ZoomX, 2) > 1 || Math.Round(zoomBorder.ZoomY, 2) > 1)
            PanNormally();
        else
            PanInYDirectionOnly();
        CalculatePageIndexOfCurrentPage();
    }

    /// <summary>
    /// Handles single-tap gestures.
    /// </summary>
    private void OnSingleTap(object sender, TappedEventArgs e)
    {
        // If stamp mode is on then add a stamp to the page at the cursor position.
        if (model.IsStampMode)
        {
            foreach (var page in musicCanvas.Children.OfType<PDFPageCanvas>())
                if (page.AddStampToPage(e.GetPosition(page)))
                    break;
            e.Handled = true;
            return;
        }

        // detect if the user is currently panning, and if so, ignore the tap
        if (panTime.TotalMilliseconds < 400)
        {
            double centrePointX = zoomBorder.Bounds.Width / 2;
            double centrePointY = zoomBorder.Bounds.Height / 2;
            double xTolerance = zoomBorder.Bounds.Width / 10;
            double yTolerance = zoomBorder.Bounds.Height / 10;

            Point point = e.GetPosition(zoomBorder);

            // If the tap is in the center, toggle the toolbar instead of scrolling
            if (Math.Abs(point.X - centrePointX) < xTolerance &&
                Math.Abs(point.Y - centrePointY) < yTolerance)
            {
                if (!model.IsToolbarVisible)
                    OnCentreTap();
                return;
            }

            int nextPageIndex;
            if (point.Y < centrePointY / 2)
                nextPageIndex = currentPageIndex - 1;   // go to previous page
            else
                nextPageIndex = currentPageIndex + 1;   // go to next page

            if (nextPageIndex >= 0 && nextPageIndex < musicCanvas.Children.Count)
            {
                double scrollAmount = -musicCanvas.Children[nextPageIndex].Bounds.Top;

                // Pan ZoomBorder to the tapped point
                double deltaPan = scrollAmount - zoomBorder.OffsetY;
                zoomBorder.PanDelta(0, deltaPan);

                CalculatePageIndexOfCurrentPage();
            }
        }
        e.Handled = true;
    }

    /// <summary>
    /// Calculate the index of the current page being displayed.
    /// </summary>
    private void CalculatePageIndexOfCurrentPage()
    {
        // Get the y pixel position of the page using a point down a bit from the top of the page.
        // This gets around the problem where the top of the page is down a bit from the top of the screen.
        int smallAmount = 200;
        double yPositionCentreViewPort = -zoomBorder.OffsetY + smallAmount;

        var page = musicCanvas.Children.OfType<PDFPageCanvas>().FirstOrDefault(page => page.ContainsYPoint(yPositionCentreViewPort));
        currentPageIndex = musicCanvas.Children.IndexOf(page);
    }

    /// <summary>
    /// Invoked when the user taps in the center of the screen. 
    /// </summary>
    private void OnCentreTap()
    {
        model.IsToolbarVisible = !model.IsToolbarVisible;
        CalculatePageIndexOfCurrentPage();
    }

    /// <summary>
    /// Pan normally, allowing panning in both X and Y directions.
    /// </summary>
    private void PanNormally()
    {
        double maximumHeight = (musicCanvas.Children.Count * Bounds.Height) + (musicCanvas.Children.Count-1 * musicCanvas.Spacing);
        zoomBorder.MinOffsetX = -Bounds.Width * zoomBorder.MaxZoomX;
        zoomBorder.MaxOffsetX = Bounds.Width * zoomBorder.MaxZoomX;
        zoomBorder.MinOffsetY = -maximumHeight * zoomBorder.MaxZoomY;
        zoomBorder.MaxOffsetY = maximumHeight * zoomBorder.MaxZoomY;
        zoomBorder.EnablePan = true;
        zoomBorder.EnableGestureTranslation = true;
    }

    /// <summary>
    /// Turn all panning off, so that the user can draw/erase without panning the page.
    /// </summary>
    private void PanOff()
    {
        zoomBorder.EnablePan = false;
        zoomBorder.EnableGestureTranslation = false;
        zoomBorder.MinOffsetX = zoomBorder.OffsetX;
        zoomBorder.MaxOffsetX = zoomBorder.OffsetX;
        zoomBorder.MinOffsetY = zoomBorder.OffsetY;
        zoomBorder.MaxOffsetY = zoomBorder.OffsetY;
    }
    
    /// <summary>
    /// Allows panning in the Y direction only.
    /// </summary>
    private void PanInYDirectionOnly(double newHeight = 0)
    {
        if (musicCanvas.Children.Count > 0)
        {
            if (newHeight == 0)
                newHeight = Bounds.Height;

            double maximumHeight = (musicCanvas.Children.Count-1) * (newHeight + musicCanvas.Spacing);
            zoomBorder.MinOffsetX = 0;
            zoomBorder.MaxOffsetX = 0;
            zoomBorder.MinOffsetY = -maximumHeight;
            zoomBorder.MaxOffsetY = 0;

            zoomBorder.EnablePan = true;
            zoomBorder.EnableGestureTranslation = true;
        }
    }      
}