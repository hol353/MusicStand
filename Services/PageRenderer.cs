using System.Linq;
using Avalonia;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Media;

namespace MusicStand;

/// <summary>
/// Represents a renderer for a page in the canvas, responsible for rendering the page's bitmap and any associated link markers.
/// </summary>
public class PageRenderer
{
    private Rect renderRectangle;

    /// <summary>
    /// Initializes a new instance of the <see cref="CanvasRenderer"/> class.
    /// </summary>
    /// <param name="titles"></param>
    public PageRenderer(Page page, Visual visual)
    {
        this.Page = page;
        Control = visual;
    }

    /// <summary>
    /// Gets the page associated with the page renderer.
    /// </summary>
    public Page Page { get; }

    /// <summary>
    /// Gets the visual control associated with the page renderer.
    /// </summary>
    public Visual Control { get; private set; }

    /// <summary>
    /// Gets or sets the rectangle representing the area where the page's content will be rendered.
    /// </summary>
    public Rect RenderRectangle => renderRectangle;

    /// <summary>
    /// Sets the rectangle representing the area where the page's content will be rendered.
    /// </summary>
    /// <param name="renderRectangle"></param>
    public void SetRenderRectangle(Rect renderRectangle)
    {
        this.renderRectangle = renderRectangle;
    }    

    /// <summary>
    /// Determines whether the specified point is within the bounds of the page's render rectangle.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public bool ContainsPoint(TappedEventArgs e)
    {
        var position = e.GetPosition(Control);
        return renderRectangle.Width > 0 &&
               renderRectangle.Height > 0 &&
               renderRectangle.Contains(position);
    }

    /// <summary>
    /// Gets the relative position of a point on the page, expressed as a fraction of the page's width and height.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="pointRelativeToPage"></param>
    /// <returns></returns>
    internal Point GetRelativePosition(PageRenderer page, Point pointRelativeToPage)
    {
        return new Point(
            (pointRelativeToPage.X - renderRectangle.Left) / renderRectangle.Width,
            (pointRelativeToPage.Y - renderRectangle.Top) / renderRectangle.Height);
    }

    /// <summary>
    /// Renders the page's bitmap and any associated link markers onto the specified drawing context.
    /// </summary>
    /// <param name="context"></param>
    public void Render(DrawingContext context)
    {
        foreach (var marker in Page.Title.Links.SelectMany(link => link.Markers)
                                               .Where(marker => marker?.PageNumber == Page.PageNumber))
        {
            const double radius = 12;
            var color = Color.FromArgb((byte)(marker.Color.A * 0.6), marker.Color.R, marker.Color.G, marker.Color.B);
            var markerPositionX = renderRectangle.Left + marker.X * renderRectangle.Width;
            var markerPositionY = renderRectangle.Top + marker.Y * renderRectangle.Height;
            context.DrawEllipse(new SolidColorBrush(color), null,
                new Rect(markerPositionX - radius, markerPositionY - radius, radius * 2, radius * 2));
        }
    }   

    /// <summary>
    /// Measures the size of a page within the available space.
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="availableWidth"></param>
    /// <param name="availableHeight"></param>
    /// <returns></returns>
    public Rect MeasurePage(double availableWidth, double availableHeight)
    {
        var width = availableWidth;
        var height = availableHeight;
        if (Page.Bitmap != null)
        {
            double imageAspectRatio = Page.Bitmap.Size.Height / Page.Bitmap.Size.Width;
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
}