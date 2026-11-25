using System.Collections.Generic;
using System.Linq;
using Avalonia;

namespace MusicStand;

/// <summary>
/// Represents a renderer for the links between pages in the canvas.
/// </summary>
public class LinkRenderer
{
    private CanvasRenderer canvas;
    private Link pendingLink;
    private PageRenderer firstLinkMarkerPage;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkRenderer"/> class.
    /// </summary>
    /// <param name="canvas"></param>
    public LinkRenderer(CanvasRenderer canvas)
    {
        this.canvas = canvas;
    }

    /// <summary>
    /// Attempts to add a link between two markers on the specified page at the given point relative to the page.
    /// If this is the first marker, it will be stored as a pending link. 
    /// If this is the second marker, a link will be created between the two markers if they belong to the
    /// same title. If they belong to different titles, the second marker will be ignored.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="pointRelativeToPage"></param>
    public void TryAddLink(PageRenderer page, Point pointRelativeToPage)
    {
        if (page != null)
        {
            // Determine the relative position on the page.
            var relativePosition = page.GetRelativePosition(page, pointRelativeToPage);

            // Determine if this is the first marker.
            if (pendingLink == null)
            {
                // First marker.
                firstLinkMarkerPage = page;
                pendingLink = new Link
                {
                    Marker1 = new() 
                    { 
                        PageNumber = page.Page.PageNumber,
                        X = relativePosition.X, 
                        Y = relativePosition.Y,
                        Color = canvas.Model.SelectedBrush.Color
                    },
                };
                page.Page.Title.Links.Add(pendingLink);
                page.Control.InvalidateVisual();
            }
            else
            {
                // Second marker
                var secondLinkMarkerPage = page;
                var secondLinkMarkerPoint = relativePosition;

                // Make sure the 2 markers have the same title. The user may have a setlist open
                // and have tried to create a link between pages in 2 different titles. Don't allow this.
                if (firstLinkMarkerPage.Page.Title == secondLinkMarkerPage.Page.Title)
                {
                    pendingLink.Marker2 = new()
                    {
                        Color = pendingLink.Marker1.Color,
                        PageNumber = secondLinkMarkerPage.Page.PageNumber,
                        X = secondLinkMarkerPoint.X,
                        Y = secondLinkMarkerPoint.Y,
                    };
                    
                    firstLinkMarkerPage = null;
                    pendingLink = null;
                    canvas.Model.IsLinkMode = false;
                    secondLinkMarkerPage.Control.InvalidateVisual();
                }
            }
        }       
    }
  
    /// <summary>
    /// Attempts to navigate to a linked marker on the specified page at the given point relative to the page.
    /// </summary>
    /// <param name="pageRenderer"></param>
    /// <param name="pointRelativeToPage"></param>
    /// <returns></returns>
    public bool TryNavigateToLinkedMarker(PageRenderer pageRenderer, Point pointRelativeToPage)
    {
        foreach (var title in canvas.PageRenderers.Select(pageRenderer => pageRenderer.Page.Title))
        {
            foreach (var link in GetLinksAtPoint(pageRenderer, pointRelativeToPage))
            {
                if (IsLinkMarkerAt(link.Marker1, pageRenderer, pointRelativeToPage))
                {
                    NavigateToEndpoint(link.Marker2);
                    return true;
                }

                if (IsLinkMarkerAt(link.Marker2, pageRenderer, pointRelativeToPage))
                {
                    NavigateToEndpoint(link.Marker1);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Erases any links that are located at the specified point relative to the given page renderer.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="pointRelativeToPage"></param>
    internal void EraseLinksAt(PageRenderer page, Point pointRelativeToPage)
    {
        var allLinks = GetLinksAtPoint(page, pointRelativeToPage).ToArray();
        foreach (var link in allLinks)
        {
            page.Page.Title.Links.Remove(link);
            page.Control.InvalidateVisual();
        }
        CancelPendingLink();
    }

    /// <summary>
    /// Cancels any pending link that is currently being created. If a pending link exists, it will be removed from the title's links collection and the page will be invalidated to reflect the change.
    /// </summary>
    public void CancelPendingLink()
    {
        if (pendingLink != null)
        {
            var page = canvas.PageRenderers[pendingLink.Marker1.PageNumber-1];
            page.Page.Title.Links.Remove(pendingLink);
            page.Control.InvalidateVisual();
        }

        pendingLink = null;
        firstLinkMarkerPage = null;
    }

    /// <summary>
    /// Gets the collection of links that are located at the specified point relative to the given page renderer.
    /// </summary>
    /// <param name="pageRenderer"></param>
    /// <param name="pointRelativeToPage"></param>
    /// <returns></returns>
    private IEnumerable<Link> GetLinksAtPoint(PageRenderer pageRenderer, Point pointRelativeToPage)
    {
        foreach (var title in canvas.PageRenderers.Select(page => pageRenderer.Page.Title))
        {
            foreach (var link in title.Links)
            {
                if (IsLinkMarkerAt(link.Marker1, pageRenderer, pointRelativeToPage) || IsLinkMarkerAt(link.Marker2, pageRenderer, pointRelativeToPage))
                    yield return link;
            }
        }
    }

    /// <summary>
    /// Determines whether the specified link marker is located at the given point relative to the specified page renderer.
    /// </summary>
    /// <param name="marker"></param>
    /// <param name="page"></param>
    /// <param name="pointRelativeToPage"></param>
    /// <returns></returns>
    private bool IsLinkMarkerAt(LinkMarker1 marker, PageRenderer page, Point pointRelativeToPage)
    {
        if (marker.PageNumber == page.Page.PageNumber)
        {
            const double hitRadius = 20;
            var (markerX, markerY) = RelativeToAbsolutePoint(marker);
            var deltaX = pointRelativeToPage.X - markerX;
            var deltaY = pointRelativeToPage.Y - markerY;

            // Look through all link markers in the title that has the specified marker and return true if any
            // match the pointer passed in.
            return canvas.PageRenderers[marker.PageNumber-1]
                        .Page
                        .Title
                        .Links.SelectMany(link => link.Markers)
                        .Any(m => m == marker) && deltaX * deltaX + deltaY * deltaY <= hitRadius * hitRadius;
        }
        return false;
    } 

    /// <summary>
    /// Converts the relative coordinates of a link marker to absolute coordinates on the canvas.
    /// </summary>
    /// <param name="marker"></param>
    /// <returns></returns>
    private (double x, double y) RelativeToAbsolutePoint(LinkMarker1 marker)
    {
        var bitmapRenderRectangle = canvas.PageRenderers[marker.PageNumber-1].RenderRectangle;
        return (
            bitmapRenderRectangle.Left + marker.X * bitmapRenderRectangle.Width,
            bitmapRenderRectangle.Top + marker.Y * bitmapRenderRectangle.Height
            );
    }       

    /// <summary>
    /// Navigates to the endpoint of a link marker by panning the canvas to center on the specified marker's location.
    /// </summary>
    /// <param name="endPoint"></param>
    private void NavigateToEndpoint(LinkMarker1 endPoint)
    {
        var page = canvas.PageRenderers[endPoint.PageNumber-1];

        var markerCoordinates = RelativeToAbsolutePoint(endPoint);
        var markerPoint = new Point(markerCoordinates.x, markerCoordinates.y);

        canvas.PanToPointOnPage(markerPoint, page);
    }  
}