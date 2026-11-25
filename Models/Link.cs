using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;

namespace MusicStand;

/// <summary>
/// Represents a link between two points on different pages of a title.
/// </summary>
public sealed class Link
{
    public LinkMarker1 Marker1 { get; set; }
    public LinkMarker1 Marker2 { get; set; }

    public IEnumerable<LinkMarker1> Markers => [ Marker1, Marker2 ];
}  

/// <summary>
/// Represents a marker for a link on a specific page of a title.
/// </summary>
public class LinkMarker1
{
    public int PageNumber { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public Color Color { get; set; }
}