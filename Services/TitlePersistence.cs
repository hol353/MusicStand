using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Primitive;
using SkiaSharp;

namespace MusicStand;

/// <summary>
/// Persistence store for a title
/// </summary>
public class TitlePersistence
{
    /// <summary>
    /// Read a title's annotations and links from storage.
    /// </summary>
    public static void Initialise(Title title, Settings settings)
    {
        var pdfRelativeFilePath = title.PDFPath.ToRelative(settings.MusicLibraryBaseDirectory);
        var xmlFilePath = Path.Combine(settings.HomeDirectory, 
                                       "Annotations", 
                                       $"{Path.GetFileNameWithoutExtension(pdfRelativeFilePath)}.xml");
        if (File.Exists(xmlFilePath))
        {
            var doc = XDocument.Load(xmlFilePath);
            var titleElement = doc.Elements().FirstOrDefault(n => n.Name == "title");

            // Read pages
            foreach (var pageElement in titleElement?.Elements().Where(n => n.Name == "page"))
            {
                var pageNumber = int.Parse(pageElement.Element("number").Value);
                var svgElement = pageElement.Elements().First(n => n.Name.LocalName == "svg");
                int width = svgElement != null ? int.Parse(svgElement.Attribute("width")?.Value) : 0;
                int height = svgElement != null ? int.Parse(svgElement.Attribute("height")?.Value) : 0;
                Page page = title.Pages.FirstOrDefault(p => p.PageNumber == pageNumber);
                if (page != null)
                {
                    page.Strokes = GetStrokesFromSVGElement(svgElement);
                    page.ViewPortBounds = new SKRect(0, 0, width, height);
                }
            }

            // Read links
            title.Links.Clear();
            foreach (var linkElement in titleElement?.Elements().Where(n => n.Name == "link"))
            {
                var point1Element = linkElement.Elements("point").First();
                var point2Element = linkElement.Elements("point").Last();
                var colorElement = linkElement.Element("color");

                if (point1Element != null && point2Element != null && colorElement != null)
                {
                    var point1 = new Avalonia.Point(
                        double.Parse(point1Element.Element("x")?.Value),
                        double.Parse(point1Element.Element("y")?.Value));
                    var point2 = new Avalonia.Point(
                        double.Parse(point2Element.Element("x")?.Value),
                        double.Parse(point2Element.Element("y")?.Value));
                    var color = Avalonia.Media.Color.Parse(colorElement.Value);

                    title.Links.Add(new Link { Point1 = point1, Point2 = point2, Color = color });
                }
            }
        }
    }

    public static void Write(Title title, Settings settings)
    {
        XDocument doc = new XDocument();
        var titleElement = new XElement("title");
        doc.Add(titleElement);

        // Write pages.
        foreach (var page in title.Pages)
        {
            // Create a page element
            var pageElement = new XElement("page");
            titleElement.Add(pageElement);
            pageElement.Add(new XElement("number", page.PageNumber));

            // Convert strokes to SVG XML and append to page
            pageElement.Add(StrokesToXML(page.Strokes, page.ViewPortBounds));
        }

        // Write links
        foreach (var link in title.Links)
        {
            var linkElement = new XElement("link");
            linkElement.Add(new XElement("color", link.Color));

            linkElement.Add(CreatePointElement(doc, link.Point1PageNumber, link.Point1.X, link.Point1.Y));
            linkElement.Add(CreatePointElement(doc, link.Point2PageNumber, link.Point2.X, link.Point2.Y));

            titleElement.Add(linkElement);
        }

        // If there are any links or pages with annotations then write the file.
        if (titleElement.Descendants().Any())
        {
            var pdfRelativeFilePath = title.PDFPath.ToRelative(settings.MusicLibraryBaseDirectory);
            var filePath = Path.Combine(settings.HomeDirectory, 
                                        "Annotations", 
                                        $"{Path.GetFileNameWithoutExtension(pdfRelativeFilePath)}.xml");
            doc.Save(filePath);    
        }
    }


    /// <summary>
    /// Creates a point element for a link in the XML document.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="pageNumber"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private static XElement CreatePointElement(XDocument doc, int pageNumber, double x, double y)
    {
        var pointElement = new XElement("point");
        pointElement.Add(new XElement("page", pageNumber));
        pointElement.Add(new XElement("x", x));
        pointElement.Add(new XElement("y", y));
        return pointElement;
    }

    /// <summary>
    /// Convert strokes to XMLDocument.
    /// </summary>
    private static XmlDocument StrokesToXML(IReadOnlyList<SkiaStroke> strokes, SKRect viewPortBounds)
    {
        var ms = new MemoryStream();
        using (var skCanvas = SKSvgCanvas.Create(viewPortBounds, ms))
        {
            using var skPaint = new SKPaint();
            skPaint.IsAntialias = true;
            skPaint.Style = SKPaintStyle.Fill;

            foreach (var stroke in strokes)
            {
                skPaint.Color = stroke.Color;
                skCanvas.DrawPath(stroke.Path, skPaint);
            }
        }
        ms.Position = 0;
        XmlDocument svg = new();
        svg.Load(ms);
        return svg;
    }

    /// <summary>
    /// Convert a page element to a list of SkiaStrokes.
    /// </summary>
    /// <param name="svgElement"></param>
    /// <returns></returns>
    private static List<SkiaStroke> GetStrokesFromSVGElement(XElement svgElement)
    {
        var strokes = new List<SkiaStroke>();
        var pathElement = svgElement.Elements().First(e => e.Name.LocalName == "path");
        if (pathElement != null)
        {
            // Convert SVG XML back to strokes
            using (var ms = new MemoryStream())
            {
                pathElement.Document.Save(ms);
                ms.Position = 0;
                using (var skCanvas = SKSvgCanvas.Create(SKRect.Empty, ms))
                {
                    // Get the stroke path.
                    var pathData = pathElement.Attributes("d").First().Value;
                    var skPath = SKPath.ParseSvgPathData(pathData);       

                    // get the stroke colour.
                    string colorName = pathElement.Attributes("fill").First().Value;
                    var fieldInfo = typeof(SKColors).GetFields(BindingFlags.Static | BindingFlags.Public)
                                                    .First(fieldInfo => fieldInfo.Name.ToLower() == colorName);
                    var color = (SKColor)fieldInfo.GetValue(null);

                    // Apply the opacity to the stroke colour.
                    var opacity = GetOpacity(pathElement, "opacity") * GetOpacity(pathElement, "fill-opacity");
                    color = color.WithAlpha((byte)Math.Round(color.Alpha * opacity));

                    List<InkStylusPoint> points = new();
                    StylusPointListSpan stylusPointListSpan = new(points, 0, points.Count);
                    var stroke = SkiaStroke.CreateStaticStroke(InkId.NewId(), skPath, stylusPointListSpan, color, 0.1f, true, inkStrokeRenderer: null);                                  
                    strokes.Add(stroke);
                }
            }
        }
        return strokes;
    }

    /// <summary>
    /// Get the opacity value from an XML element's attribute. If the attribute is not present or cannot be parsed, return 1 (fully opaque).
    /// </summary>
    /// <param name="pathElement"></param>
    /// <param name="attributeName"></param>
    /// <returns></returns>
    private static double GetOpacity(XElement pathElement, string attributeName)
    {
        var attribute = pathElement.Attributes(attributeName).FirstOrDefault();
        if (attribute == null || !double.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity))
            return 1;

        return Math.Clamp(opacity, 0, 1);
    }

}