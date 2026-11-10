using System.Collections.Generic;
using Avalonia.Media.Imaging;
using DotNetCampus.Inking;
using SkiaSharp;

namespace MusicStand;

/// <summary>
/// Represents a page in a title in the music library.
/// </summary>
public class Page
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Page"/> class.
    /// </summary>
    /// <param name="pageNumber">Page number.</param>
    public Page(int pageNumber, Bitmap bitmap)
    {
        PageNumber = pageNumber;
        Bitmap = bitmap;
    }

    /// <summary>
    /// Gets the page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// The bitmap representing the page of music.
    /// </summary>
    public Bitmap Bitmap { get; }    

    /// <summary>
    /// A collection of strokes (annotations), relative to ViewPortBounds.
    /// </summary>
    public List<SkiaStroke> Strokes { get; set; } = new();

    /// <summary>
    /// The bounds of the viewport containing the page.
    /// </summary>
    public SKRect ViewPortBounds { get; set; }
}