using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Media;

namespace MusicStand.Tests;

[Collection("Avalonia")]
public class LinkRendererTests
{
    /// <summary>
    /// Tests that the TryAddLink method does nothing when the page parameter is null. 
    /// </summary>
    [Fact]
    public void TryAddLink_WithNullPage_DoesNothing()
    {
        var (canvas, _) = CreateCanvas();
        canvas.Model.IsLinkMode = true;

        canvas.LinkRenderer.TryAddLink(null, new Point(10, 10));

        Assert.True(canvas.Model.IsLinkMode);
    }

    /// <summary>
    /// Tests that the TryAddLink method creates a pending link when the first marker is added to a page.
    /// </summary>
    [Fact]
    public void TryAddLink_FirstMarker_CreatesPendingLink()
    {
        var (canvas, title) = CreateCanvas();
        var page = CreatePageRenderer(title, 1);
        canvas.SetPageRenderers([page]);
        var point = new Point(35, 45);

        canvas.LinkRenderer.TryAddLink(page, point);

        var link = Assert.Single(title.Links);
        Assert.Null(link.Marker2);
        Assert.Equal(1, link.Marker1.PageNumber);
        Assert.Equal(0.15, link.Marker1.X);
        Assert.Equal(0.3125, link.Marker1.Y);
        Assert.Equal(canvas.Model.SelectedBrush.Color, link.Marker1.Color);
    }

    /// <summary>
    /// Tests that the TryAddLink method completes a link when the second marker is added to a page with the same title as the first marker.
    /// </summary>
    [Fact]
    public void TryAddLink_SecondMarkerOnSameTitle_CompletesLink()
    {
        var (canvas, title) = CreateCanvas();
        var firstPage = CreatePageRenderer(title, 1);
        var secondPage = CreatePageRenderer(title, 2);
        canvas.SetPageRenderers([firstPage, secondPage]);
        canvas.Model.IsLinkMode = true;

        canvas.LinkRenderer.TryAddLink(firstPage, new Point(30, 40));
        canvas.LinkRenderer.TryAddLink(secondPage, new Point(60, 80));

        var link = Assert.Single(title.Links);
        Assert.NotNull(link.Marker2);
        Assert.Equal(2, link.Marker2.PageNumber);
        Assert.Equal(0.4, link.Marker2.X);
        Assert.Equal(0.75, link.Marker2.Y);
        Assert.Equal(link.Marker1.Color, link.Marker2.Color);
        Assert.False(canvas.Model.IsLinkMode);
    }

    /// <summary>
    /// Tests that the TryAddLink method ignores a second marker on a different title and keeps the pending link.
    /// </summary>
    [Fact]
    public void TryAddLink_SecondMarkerOnDifferentTitle_IsIgnoredAndKeepsPendingLink()
    {
        var (canvas, firstTitle) = CreateCanvas();
        var firstPage = CreatePageRenderer(firstTitle, 1);
        var secondTitle = new Title(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf"));
        var secondPage = CreatePageRenderer(secondTitle, 2);
        canvas.SetPageRenderers([firstPage, secondPage]);

        canvas.LinkRenderer.TryAddLink(firstPage, new Point(30, 40));
        canvas.LinkRenderer.TryAddLink(secondPage, new Point(60, 80));

        Assert.Single(firstTitle.Links);
        Assert.Empty(secondTitle.Links);
        Assert.Null(firstTitle.Links[0].Marker2);
    }

    /// <summary>
    /// Tests that the TryNavigateToLinkedMarker method returns true when navigating to either endpoint of a linked marker.
    /// </summary>
    /// <param name="fromPageNumber">The page number from which to navigate.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void TryNavigateToLinkedMarker_OnEitherEndpoint_ReturnsTrue(int fromPageNumber)
    {
        var (canvas, title) = CreateCanvas();
        var firstPage = CreatePageRenderer(title, 1);
        var secondPage = CreatePageRenderer(title, 2);
        canvas.SetPageRenderers([firstPage, secondPage]);

        title.Links.Add(new Link
        {
            Marker1 = new LinkMarker1 { PageNumber = 1, X = 0.25, Y = 0.25, Color = Colors.Red },
            Marker2 = new LinkMarker1 { PageNumber = 2, X = 0.75, Y = 0.75, Color = Colors.Red }
        });

        var fromPage = fromPageNumber == 1 ? firstPage : secondPage;
        var marker = fromPageNumber == 1
            ? title.Links[0].Marker1
            : title.Links[0].Marker2;
        var point = AbsolutePoint(fromPage, marker);

        Assert.True(canvas.LinkRenderer.TryNavigateToLinkedMarker(fromPage, point));
    }

    /// <summary>
    /// Tests that the TryNavigateToLinkedMarker method returns false when the point is outside the linked marker area.
    /// </summary>
    [Fact]
    public void TryNavigateToLinkedMarker_OutsideMarker_ReturnsFalse()
    {
        var (canvas, title) = CreateCanvas();
        var page = CreatePageRenderer(title, 1);
        canvas.SetPageRenderers([page]);
        title.Links.Add(new Link
        {
            Marker1 = new LinkMarker1 { PageNumber = 1, X = 0.25, Y = 0.25, Color = Colors.Red },
            Marker2 = new LinkMarker1 { PageNumber = 2, X = 0.75, Y = 0.75, Color = Colors.Red }
        });

        Assert.False(canvas.LinkRenderer.TryNavigateToLinkedMarker(page, new Point(200, 200)));
    }

    /// <summary>
    /// Tests that the CancelPendingLink method removes the pending link and can be called again without error.
    /// </summary>
    [Fact]
    public void CancelPendingLink_RemovesPendingLinkAndCanBeCalledAgain()
    {
        var (canvas, title) = CreateCanvas();
        var page = CreatePageRenderer(title, 1);
        canvas.SetPageRenderers([page]);
        canvas.LinkRenderer.TryAddLink(page, new Point(30, 40));

        canvas.LinkRenderer.CancelPendingLink();

        Assert.Empty(title.Links);
        canvas.LinkRenderer.CancelPendingLink();
        Assert.Empty(title.Links);
    }

    /// <summary>
    /// Creates a new CanvasRenderer and Title for testing purposes.
    /// </summary>
    /// <returns></returns>
    private static (CanvasRenderer Canvas, Title Title) CreateCanvas()
    {
        var title = new Title(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf"));
        var model = new MainViewModel();
        var canvas = new CanvasRenderer(new ZoomBorder(), model);
        return (canvas, title);
    }

    /// <summary>
    /// Creates a new PageRenderer for the specified title and page number, with a predefined render rectangle.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="pageNumber"></param>
    /// <returns></returns>
    private static PageRenderer CreatePageRenderer(Title title, int pageNumber)
    {
        var page = new Page(title, pageNumber, null);
        var control = new Border();
        var renderer = new PageRenderer(page, control);
        renderer.SetRenderRectangle(new Rect(20, 20, 100, 80));
        return renderer;
    }

    /// <summary>
    /// Calculates the absolute position of a link marker on a page renderer based on its relative coordinates and the render rectangle.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="marker"></param>
    /// <returns></returns>
    private static Point AbsolutePoint(PageRenderer page, LinkMarker1 marker)
    {
        var rect = page.RenderRectangle;
        return new Point(
            rect.Left + marker.X * rect.Width,
            rect.Top + marker.Y * rect.Height);
    }
}
