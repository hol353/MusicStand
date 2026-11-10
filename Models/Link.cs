using Avalonia;
using Avalonia.Media;

namespace MusicStand;

/// <summary>
/// Represents a link between two points on different pages of a title.
/// </summary>
public sealed class Link
{
    public int Point1PageNumber { get; set; }
    public int Point2PageNumber { get; set; }
    public Point Point1 { get; set; }
    public Point Point2 { get; set; }
    public Color Color { get; set; }
}  