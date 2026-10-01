
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

    /// <summary>Gets the geometry for the stamp. Used on button on UI.</summary>
    public Geometry Geometry => geometry;   

    /// <summary>
    /// Gets the path for the stamp, scaled and translated to be centred on the specified position.
    /// </summary>
    /// <param name="position">The position to centre the stamp on.</param>
    /// <returns>The transformed path.</returns>
    public SKPath GetPathCentredOn(Point position)
    {
        float scaleX = 0.05F;
        float scaleY = 0.05F;

        float stampHeight = Path.Bounds.Height * scaleX;
        float stampWidth = Path.Bounds.Width * scaleY;
        var matrix = SKMatrix.CreateScaleTranslation(
            scaleX,
            scaleY,
            (float)position.X - stampWidth / 2,
            (float)position.Y - stampHeight / 2);        
        SKPath clonedPath = Path.Clone();
        clonedPath.Transform(matrix);
        return clonedPath;
    }
}