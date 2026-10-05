using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Skia;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Contexts;
using DotNetCampus.Inking.Primitive;
using SkiaSharp;

namespace MusicStand;

/// <summary>
/// Encapsulates a user control that shows a page of music and allows annotations.
/// </summary>
public class PDFPageCanvas : InkCanvas
{

    private MainViewModel model;

    /// <summary>The music page to show.</summary>
    private MusicPage page;

    /// <summary>The main view.</summary>
    private PDFCanvas pdfAnnotator;

    /// <summary>The rectangle the bitmap is drawn into.</summary>
    private Rect bitmapRenderRectangle;

    /// <summary>The time when the user started panning. Used to determine if a tap is a pan or a single tap.</summary>
    private DateTime panStartTime = DateTime.MinValue;

    /// <summary>The size of the viewport in pixels.</summary>
    private SKRect viewPortSize;
    private ZoomBorder zoomBorder;
    private Point pointerPosition;
    private bool isPointerOverPage;
    private bool isHairpinDrawing;
    private Point hairpinStartPosition;
    private Point hairpinCurrentPosition;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="image">The music bitmap to show.</param>
    /// <param name="svg">The annotations from the .svg to show.</param>
    public PDFPageCanvas(PDFCanvas pdfAnnotator, MusicPage page, MainViewModel model)
    {
        this.page = page;
        this.pdfAnnotator = pdfAnnotator;
        this.model = model;
        List<InkStylusPoint> points = new();
        StylusPointListSpan stylusPointListSpan = new(points, 0, points.Count);
        if (page.Annotations != null)
        {
            foreach (var annotation in page.Annotations.Paths)
            {
                var stroke = SkiaStroke.CreateStaticStroke(InkId.NewId(), annotation.Path, stylusPointListSpan, annotation.Color, 0.1f, true, inkStrokeRenderer: null);
                AvaloniaSkiaInkCanvas.AddStaticStroke(stroke);              
            }        
        }
    }

    /// <summary>
    /// Determines if the given y point is within the bounds of the bitmap render rectangle.
    /// </summary>
    /// <param name="ypoint"></param>
    /// <returns></returns>
    public bool ContainsYPoint(double ypoint)
    {
        return ypoint >= Bounds.Top && ypoint < Bounds.Top + bitmapRenderRectangle.Height;
    }

    /// <summary>
    /// Called when the control is attached to the visual tree.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        zoomBorder = this.FindLogicalAncestorOfType<ZoomBorder>();

        this.StrokeCollected += OnStrokeCollected;
        this.StrokeErased += OnStrokeErased;
        this.SizeChanged += OnSizeChanged;
        this.PointerMoved += OnPointerMoved;
        this.PointerPressed += OnPointerPressed;
        this.PointerReleased += OnPointerReleased;
        this.PointerExited += OnPointerExited;
    }

    /// <summary>
    /// Called when the control is detached from the visual tree.
    /// </summary>
    /// <param name="e"></param>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        this.StrokeCollected -= OnStrokeCollected;
        this.StrokeErased -= OnStrokeErased;
        this.SizeChanged -= OnSizeChanged;
        this.PointerMoved -= OnPointerMoved;
        this.PointerPressed -= OnPointerPressed;
        this.PointerReleased -= OnPointerReleased;
        this.PointerExited -= OnPointerExited;
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (!model.IsStampMode || !model.SelectedStamp.IsHairpin ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var position = e.GetPosition(this);
        if (!bitmapRenderRectangle.Contains(position))
            return;

        isHairpinDrawing = true;
        hairpinStartPosition = position;
        hairpinCurrentPosition = position;
        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }

    private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (!isHairpinDrawing)
            return;

        hairpinCurrentPosition = e.GetPosition(this);
        if (hairpinCurrentPosition.X - hairpinStartPosition.X > 1 &&
            Math.Abs(hairpinCurrentPosition.Y - hairpinStartPosition.Y) > 1)
            AddHairpinToPage(hairpinStartPosition, hairpinCurrentPosition);

        isHairpinDrawing = false;
        e.Pointer.Capture(null);
        e.Handled = true;
        InvalidateVisual();
    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        pointerPosition = e.GetPosition(this);
        if (isHairpinDrawing)
            hairpinCurrentPosition = pointerPosition;
        isPointerOverPage = true;
        InvalidateVisual();
    }

    private void OnPointerExited(object sender, PointerEventArgs e)
    {
        isPointerOverPage = false;
        InvalidateVisual();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (page.Annotations != null && AvaloniaSkiaInkCanvas.StaticStrokeList.Count > 0)
        {
            // The strokes are all relative to the page.Annotations.
            double annotationWidth = page.Annotations.Width;
            double annotationHeight = page.Annotations.Height;

            var annotationBitmapRenderRectangle = CalculateRenderRectangle(annotationWidth, annotationHeight);
            var newBitmapRenderRectangle = CalculateRenderRectangle(e.NewSize.Width, e.NewSize.Height);

            // Transform the existing strokes to fit the new size of the control.
            var translation = CreateStretchMatrix(annotationBitmapRenderRectangle.ToSKRect(), newBitmapRenderRectangle.ToSKRect());
            foreach (var stroke in AvaloniaSkiaInkCanvas.StaticStrokeList)
                stroke.SetTransform(translation);
        }
    }

    /// <summary>
    /// Creates a simple affine scale + translation from the source rectangle's coordinate system 
    /// into the destination rectangle's coordinate system
    /// </summary>
    /// <param name="source">Source rectangle.</param>
    /// <param name="destination">Destination rectangle.</param>
    /// <returns></returns>
    private static SKMatrix CreateStretchMatrix(SKRect source, SKRect destination)
    {
        float scaleX = destination.Width / source.Width;
        float scaleY = destination.Height / source.Height;

        float translateX = destination.Left - source.Left * scaleX;
        float translateY = destination.Top  - source.Top  * scaleY;

        return SKMatrix.CreateScaleTranslation(scaleX, scaleY, translateX, translateY);
    }

    /// <summary>
    /// Called when a stroke is collected. Saves the current annotations to the .svg file.
    /// </summary>
    private void OnStrokeCollected(object sender, AvaloniaSkiaInkCanvasStrokeCollectedEventArgs e)
    {
        SaveStrokes();
    }

    /// <summary>
    /// Called when a stroke is erased. Saves the current annotations to the .svg file.
    /// </summary>
    private void OnStrokeErased(object sender, ErasingCompletedEventArgs e)
    {
        SaveStrokes();
    }

    /// <summary>
    /// Saves the current annotations to the .svg file.
    /// </summary>
    private void SaveStrokes()
    {
        // The strokes coming from InkCanvas are in the coordinate space of the control i.e. the viewPortSize.
        SKRect bitmapBounds = bitmapRenderRectangle.ToSKRect();

        // Create a list of strokes that have had their transforms applied. This
        // ensures that the saved annotation contains the correct coordinates.
        List<InkStylusPoint> points = new();
        StylusPointListSpan stylusPointListSpan = new(points, 0, points.Count);
        List<SkiaStroke> transformedStrokes = new();
        foreach (var stroke in AvaloniaSkiaInkCanvas.StaticStrokeList)
        {
            var transformedStroke = SkiaStroke.CreateStaticStroke(InkId.NewId(), stroke.Path.Clone(), stylusPointListSpan, stroke.Color, 0.1f, true, inkStrokeRenderer: null);
            transformedStroke.Path.Transform(stroke.Transform);
            transformedStrokes.Add(transformedStroke);
        }

        var newAnnotations = Annotations.Create(viewPortSize, bitmapBounds, transformedStrokes);
        page.Annotations = newAnnotations;
    }

    /// <summary>
    /// Called by the layout panel to determine what size the control needs.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The size the control wants.</returns>
    protected override Size MeasureCore(Size availableSize)
    {
        bitmapRenderRectangle = CalculateRenderRectangle(availableSize.Width, availableSize.Height);
        var size = new Size(availableSize.Width, bitmapRenderRectangle.Height);

        base.MeasureCore(size);
        return size;
    }

    public Rect CalculateRenderRectangle(double availableWidth, double availableHeight)
    {
        var width = availableWidth;
        var height = availableHeight;
        if (double.IsInfinity(height))
        {
            height = zoomBorder.Bounds.Height;
        }
        if (page.Bitmap != null)
        {
            double imageAspectRatio = page.Bitmap.Size.Height / page.Bitmap.Size.Width;
            width = height / imageAspectRatio;
            if (width > availableWidth)
            {
                width = availableWidth;
                height = width * imageAspectRatio;
            }
        }

        // Create a centered rectangle that represents the area where the music will be rendered.
        return new Rect(new Point((availableWidth - width) / 2, 0), new Size(width, height));
    }

    /// <summary>
    /// Called when positioning child elements as part of a layout pass.
    /// </summary>
    /// <param name="finalSize">The size available to the control.</param>
    /// <returns>The actual size used by the control.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        viewPortSize = new SKRect(0, 0, (float)finalSize.Width, (float)finalSize.Height);
        //return base.ArrangeOverride(finalSize);
        return finalSize;
    }

    /// <summary>
    /// Render the control by drawing the bitmap.
    /// </summary>
    /// <param name="context">The drawing context to draw on.</param>
    public override void Render(DrawingContext context)
    {
        if (page.Bitmap != null)
        {
            //Pen bitmapPen;
            //context.DrawRectangle(bitmapPen, bitmapRenderRectangle);

            context.DrawRectangle(Brushes.White, null, bitmapRenderRectangle);
            context.DrawImage(page.Bitmap, new Rect(0, 0, page.Bitmap.Size.Width, page.Bitmap.Size.Height), bitmapRenderRectangle);
        }

        // If in stamp model, then render a stamp preview at the current pointer position.
        if (isHairpinDrawing && model.IsStampMode && model.SelectedStamp.IsHairpin)
        {
            var path = GetHairpinPath(hairpinStartPosition, hairpinCurrentPosition);
            var geometry = Geometry.Parse(path.ToSvgPathData());
            var color = model.SelectedBrush.Color;
            var previewColor = Color.FromArgb((byte)(color.A * 0.7), color.R, color.G, color.B);
            context.DrawGeometry(new SolidColorBrush(previewColor), null, geometry);
        }
        else if (!model.SelectedStamp.IsHairpin && isPointerOverPage && model.IsStampMode && bitmapRenderRectangle.Contains(pointerPosition))
        {
            var path = model.SelectedStamp.GetPathCentredOn(pointerPosition);
            var geometry = Geometry.Parse(path.ToSvgPathData());
            var color = model.SelectedBrush.Color;
            var previewColor = Color.FromArgb((byte)(color.A * 0.7), color.R, color.G, color.B);
            context.DrawGeometry(new SolidColorBrush(previewColor), null, geometry);
        }

        //base.Render(context);
        //AvaloniaSkiaInkCanvas.Render(context);
    }

    /// <summary>
    /// Add a stamp to the page.
    /// </summary>
    /// <param name="position">The position to add the stamp to.</param>
    /// <param name="color"></param>
    /// <returns></returns>
    public bool AddStampToPage(Point position)
    {
        if (!bitmapRenderRectangle.Contains(position))
            return false;
        SKPath path = model.SelectedStamp.GetPathCentredOn(position);

        var color = model.SelectedBrush.Color;
        var inkColor = new SKColor(color.R, color.G, color.B, color.A);
        List<InkStylusPoint> points = new();
        var stylusPointListSpan = new StylusPointListSpan(points, 0, points.Count);
        var stroke = SkiaStroke.CreateStaticStroke(InkId.NewId(), path, stylusPointListSpan, inkColor, 0.1f, true, inkStrokeRenderer: null);
        AvaloniaSkiaInkCanvas.AddStaticStroke(stroke);
        SaveStrokes();
        return true;
    }

    private SKPath GetHairpinPath(Point start, Point end)
    {
        return model.SelectedStamp.IsDecrescendo
            ? model.SelectedStamp.GetDecrescendoPath(start, end)
            : model.SelectedStamp.GetCrescendoPath(start, end);
    }

    private void AddHairpinToPage(Point start, Point end)
    {
        var path = GetHairpinPath(start, end);
        var color = model.SelectedBrush.Color;
        var inkColor = new SKColor(color.R, color.G, color.B, color.A);
        List<InkStylusPoint> points = new();
        var stylusPointListSpan = new StylusPointListSpan(points, 0, points.Count);
        var stroke = SkiaStroke.CreateStaticStroke(InkId.NewId(), path, stylusPointListSpan, inkColor, 0.1f, true, inkStrokeRenderer: null);
        AvaloniaSkiaInkCanvas.AddStaticStroke(stroke);
        SaveStrokes();
    }
}