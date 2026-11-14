using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
                                       settings.SelectedDirectory ?? string.Empty,
                                       $"{Path.GetFileNameWithoutExtension(pdfRelativeFilePath)}.xml");
        if (File.Exists(xmlFilePath))
        {
            var doc = XDocument.Load(xmlFilePath);
            var titleElement = doc.Elements().FirstOrDefault(n => n.Name == "title");

            // Read pages
            foreach (var pageElement in titleElement?.Elements().Where(n => n.Name == "page"))
            {
                var pageNumber = int.Parse(pageElement.Element("number").Value);
                var svgElement = pageElement.Elements().FirstOrDefault(n => n.Name.LocalName == "svg");
                if (svgElement == null)
                    continue;

                int width = int.Parse(svgElement.Attribute("width")?.Value);
                int height = int.Parse(svgElement.Attribute("height")?.Value);
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
                    var point1PageNumber = int.Parse(point1Element.Element("page")?.Value);
                    var point2PageNumber = int.Parse(point2Element.Element("page")?.Value);

                    title.Links.Add(new Link
                    {
                        Point1PageNumber = point1PageNumber,
                        Point1 = point1,
                        Point2PageNumber = point2PageNumber,
                        Point2 = point2,
                        Color = color
                    });
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
            var svgElement = StrokesToXML(page.Strokes, page.ViewPortBounds);
            if (svgElement != null)
                pageElement.Add(svgElement);
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
        var pdfRelativeFilePath = title.PDFPath.ToRelative(settings.MusicLibraryBaseDirectory);
        var filePath = Path.Combine(settings.HomeDirectory,
                                    "Annotations",
                                    settings.SelectedDirectory ?? string.Empty,
                                    $"{Path.GetFileNameWithoutExtension(pdfRelativeFilePath)}.xml");
        if (titleElement.Descendants().Any(e => e.Name == "link" || (e.Name == "page" && e.Elements().Any(p => p.Name.LocalName == "svg"))))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            doc.Save(filePath);    
        }
        else
        {
            // If there are no links or pages with annotations then delete the file if it exists.
            if (File.Exists(filePath))
                File.Delete(filePath);
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
    private static XElement StrokesToXML(IReadOnlyList<SkiaStroke> strokes, SKRect viewPortBounds)
    {
        if (strokes.Count == 0)
            return null;

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
        return XElement.Load(ms);
    }

    /// <summary>
    /// Convert a page element to a list of SkiaStrokes.
    /// </summary>
    /// <param name="svgElement"></param>
    /// <returns></returns>
    public static List<SkiaStroke> GetStrokesFromSVGElement(XElement svgElement)
    {
        var strokes = new List<SkiaStroke>();
        foreach (var pathElement in svgElement.Descendants().Where(e => e.Name.LocalName == "path"))
        {
            var pathData = pathElement.Attribute("d")?.Value
                ?? throw new FormatException("An SVG annotation path is missing its 'd' attribute.");
            var colorValue = pathElement.Attribute("fill")?.Value 
                ?? "black";
            var skPath = SKPath.ParseSvgPathData(pathData)
                ?? throw new FormatException("An SVG annotation path contains invalid path data.");

            var colorField = typeof(SKColors).GetFields(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(field => string.Equals(field.Name, colorValue, StringComparison.OrdinalIgnoreCase));
            var color = colorField == null
                ? string.Equals(colorValue, "currentColor", StringComparison.OrdinalIgnoreCase)
                    ? SKColors.Black
                    : SKColor.Parse(colorValue)
                : (SKColor)colorField.GetValue(null);

            var opacity = GetOpacity(pathElement, "opacity") * GetOpacity(pathElement, "fill-opacity");
            color = color.WithAlpha((byte)Math.Round(color.Alpha * opacity));

            List<InkStylusPoint> points = new();
            StylusPointListSpan stylusPointListSpan = new(points, 0, points.Count);
            var stroke = SkiaStroke.CreateStaticStroke(
                InkId.NewId(), skPath, stylusPointListSpan, color, 0.1f, true, inkStrokeRenderer: null);
            strokes.Add(stroke);
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