using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.PanAndZoom;

namespace MusicStand;

/// <summary>
/// Represents a renderer for the canvas that displays pages and links between them.
/// </summary>
public class CanvasRenderer
{
    private IReadOnlyList<PageRenderer> pageRenderers;
    private ZoomBorder zoomBorder;

    /// <summary>
    /// Initializes a new instance of the <see cref="CanvasRenderer"/> class.
    /// </summary>
    /// <param name="titles"></param>
    public CanvasRenderer(ZoomBorder zoomBorder, MainViewModel model)
    {
        this.zoomBorder = zoomBorder;
        Model = model;
        LinkRenderer = new(this);
    }

    /// <summary>
    /// Gets the model associated with the canvas renderer.
    /// </summary>
    public MainViewModel Model { get; }

    /// <summary>
    /// Gets the collection of page renderers managed by the canvas renderer.
    /// </summary>
    public IReadOnlyList<PageRenderer> PageRenderers => pageRenderers;

    /// <summary>
    /// Gets the link renderer associated with the canvas renderer.
    /// </summary>
    public LinkRenderer LinkRenderer { get; }

    /// <summary>
    /// Sets the collection of page renderers managed by the canvas renderer.
    /// </summary>
    /// <param name="pageRenderers"></param>
    public void SetPageRenderers(IReadOnlyList<PageRenderer> pageRenderers)
    {
        this.pageRenderers = pageRenderers;
    }

    /// <summary>
    /// Pans the view to center on a specific point on a given page renderer.
    /// </summary>
    /// <param name="p"></param>
    /// <param name="pageRenderer"></param>
    public void PanToPointOnPage(Point p, PageRenderer pageRenderer)
    {
        var point = pageRenderer.Control.TranslatePoint(p, zoomBorder);
        if (point is not Point targetPoint)
            return;

        var deltaX = zoomBorder.Bounds.Width / 2 - targetPoint.X;
        var deltaY = zoomBorder.Bounds.Height / 2 - targetPoint.Y;
        zoomBorder.PanDelta(deltaX, deltaY);
        //CalculatePageIndexOfCurrentPage();
    }
}