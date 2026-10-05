
using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace MusicStand;

public class Stamp
{
    /// <summary>The SVG path for the stamp.</summary>
    public SKPath Path { get; }

    /// <summary>The SVG geometry data for the stamp.</summary>
    private readonly Geometry geometry;

    /// <summary>Whether the stamp is a crescendo hairpin drawn by dragging.</summary>
    public bool IsCrescendo { get; }

    /// <summary>Whether the stamp is a decrescendo hairpin drawn by dragging.</summary>
    public bool IsDecrescendo { get; }

    /// <summary>Whether the stamp is a hairpin drawn by dragging.</summary>
    public bool IsHairpin => IsCrescendo || IsDecrescendo;

    /// <summary>
    /// Creates a new instance of the <see cref="Stamp"/> class.
    /// </summary>
    /// <param name="fileName">The asset filename.</param>
    public Stamp(string fileName)
    {
        var assetUri = new Uri($"avares://MusicStand/Assets/{fileName}");
        using var stream = AssetLoader.Open(assetUri);

        var annotations = Annotations.Create(stream);
        Path = annotations.Paths.First().Path;
        geometry = Geometry.Parse(Path.ToSvgPathData());
    }

    private Stamp(bool isCrescendo)
    {
        IsCrescendo = isCrescendo;
        IsDecrescendo = !isCrescendo;
        Path = SKPath.ParseSvgPathData("M0 49 L100 2 L100 8 L12 50 L100 92 L100 98 L0 51 Z");
        if (IsDecrescendo)
            Path.Transform(SKMatrix.CreateScaleTranslation(-1, 1, 100, 0));
        geometry = Geometry.Parse(Path.ToSvgPathData());
    }

    /// <summary>
    /// Creates the crescendo stamp.
    /// </summary>
    public static Stamp CreateCrescendo() => new(true);

    /// <summary>
    /// Creates the decrescendo stamp.
    /// </summary>
    public static Stamp CreateDecrescendo() => new(false);

    /// <summary>Gets the geometry for the stamp. Used on button on UI.</summary>
    public Geometry Geometry => geometry;   

    /// <summary>
    /// Gets the path for the stamp, scaled and translated to be centred on the specified position.
    /// </summary>
    /// <param name="position">The position to centre the stamp on.</param>
    /// <returns>The transformed path.</returns>
    public SKPath GetPathCentredOn(Point position)
    {
        float stampHeight = Path.Bounds.Height;
        float stampWidth = Path.Bounds.Width;
        float scaleX = 1.5F;
        float scaleY = 1.5F;
        var matrix = SKMatrix.CreateScaleTranslation(scaleX, scaleY,
                                                     (float)position.X - stampWidth / 2,
                                                     (float)position.Y - stampHeight / 2);        
        SKPath clonedPath = Path.Clone();
        clonedPath.Transform(matrix);
        return clonedPath;
    }

    /// <summary>
    /// Creates the crescendo hairpin defined by a drag from its narrow end.
    /// </summary>
    public SKPath GetCrescendoPath(Point start, Point current)
    {
        return GetHairpinPath(start, current, false);
    }

    /// <summary>
    /// Creates the decrescendo hairpin defined by a drag from its wide end.
    /// </summary>
    public SKPath GetDecrescendoPath(Point start, Point current)
    {
        return GetHairpinPath(start, current, true);
    }

    /// <summary>
    /// Creates the hairpin path, mirroring the crescendo shape for a decrescendo.
    /// </summary>
    private static SKPath GetHairpinPath(Point start, Point current, bool decrescendo)
    {
        float width = (float)Math.Max(0, current.X - start.X);
        float height = (float)Math.Abs(current.Y - start.Y);
        const float lineThickness = 2;

        var path = new SKPath();
        path.MoveTo((float)start.X, (float)start.Y - lineThickness / 2);
        path.LineTo((float)start.X + width, (float)start.Y - height / 2 - lineThickness / 2);
        path.LineTo((float)start.X + width, (float)start.Y - height / 2 + lineThickness / 2);
        path.LineTo((float)start.X, (float)start.Y);
        path.LineTo((float)start.X + width, (float)start.Y + height / 2 - lineThickness / 2);
        path.LineTo((float)start.X + width, (float)start.Y + height / 2 + lineThickness / 2);
        path.LineTo((float)start.X, (float)start.Y + lineThickness / 2);
        path.Close();
        if (decrescendo)
            path.Transform(SKMatrix.CreateScaleTranslation(-1, 1, (float)(start.X + current.X), 0));
        return path;
    }
}