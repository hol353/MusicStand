using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Xml;
using DotNetCampus.Inking;
using SkiaSharp;

namespace MusicStand;

/// <summary>
/// Represents all annotations in an SVG file.
/// </summary>
/// <param name="Model">The model instance.</param>
public class Annotations
{
    private XmlDocument svg = new();

    public int Width => Int32.Parse(svg.DocumentElement.GetAttribute(("width").ToString()));

    public int Height => Int32.Parse(svg.DocumentElement.GetAttribute(("height").ToString()));

    public IEnumerable<SVGPath> Paths => svg.GetElementsByTagName("path")
                                            .Cast<XmlNode>()
                                            .Select(n => new SVGPath(n));

    /// <summary>
    /// Creates an SVG instance from a file.
    /// </summary>
    /// <param name="pageNumber">The page number.</param>
    public static Annotations Create(MainViewModel model, string pdfPath, int pageNumber)
    {
        var pdfRelativeDirectory = Path.GetDirectoryName(pdfPath).ToRelative(model.Settings.MusicLibraryBaseDirectory);
        var pdfFileName = Path.GetFileNameWithoutExtension(pdfPath);
        var svgFileName = Path.Combine(model.BaseDirectory, "Annotations", pdfRelativeDirectory, $"{pdfFileName}.{pageNumber}.svg");
        if (File.Exists(svgFileName))
        {
            using var s = File.OpenRead(svgFileName);
            return Create(s);
        }
        return null;
    }

    /// <summary>
    /// Creates an SVG instance from a stream.
    /// </summary>
    /// <param name="s">The stream.</param>
    public static Annotations Create(Stream s)
    {
        var svg = new Annotations();  
        svg.svg = new XmlDocument();
        svg.svg.Load(s);
        return svg;
    }

    /// <summary>
    /// Creates an SVG instance from a stream of strokes.
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="strokes"></param>
    /// <returns></returns>
    public static Annotations Create(SKRect bounds, SKRect clipBounds, IReadOnlyList<SkiaStroke> strokes)
    {
        if (strokes.Any())
        {
            using var ms = new MemoryStream();
            using (var skCanvas = SKSvgCanvas.Create(bounds, ms))
            {
                //skCanvas.ClipRect(clipBounds);
                using var skPaint = new SKPaint();
                skPaint.IsAntialias = true;
                skPaint.Style = SKPaintStyle.Fill;

                foreach (var stroke in strokes)
                {
                    skPaint.Color = stroke.Color;
                    skCanvas.DrawPath(stroke.Path, skPaint);
                }
            }
            
            // convert stream to xml document
            ms.Position = 0;
            //using var s = new StreamReader(ms);
            //var st = s.ReadToEnd();
            return Create(ms);
        }
        return null;
    }

    /// <summary>
    /// Saves the SVG to a file.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="pdfPath"></param>
    /// <param name="pageNumber"></param>
    public void Save(MainViewModel model, string pdfPath, int pageNumber)
    {
        var pdfRelativeDirectory = Path.GetDirectoryName(pdfPath).ToRelative(model.Settings.MusicLibraryBaseDirectory);
        var pdfFileName = Path.GetFileNameWithoutExtension(pdfPath);
        
        var annotationsBaseDirectory = Path.Combine(model.BaseDirectory, "Annotations");
        var annotationPdfDirectory = Path.Combine(annotationsBaseDirectory, pdfRelativeDirectory);

        Directory.CreateDirectory(annotationPdfDirectory);

        var saveSvgFile = Path.Combine(annotationPdfDirectory, $"{pdfFileName}.{pageNumber}.svg");            

        if (File.Exists(saveSvgFile))
            File.Delete(saveSvgFile);
        svg.Save(saveSvgFile);
    }

    public class SVGPath(XmlNode pathNode)
    {
        public SKColor Color
        {
            get
            {
                string colorName = pathNode.Attributes["fill"].Value;
                var fieldInfo = typeof(SKColors).GetFields(BindingFlags.Static | BindingFlags.Public)
                                                .First(fieldInfo => fieldInfo.Name.ToLower() == colorName);
                var color = (SKColor)fieldInfo.GetValue(null);
                var opacity = GetOpacity("opacity") * GetOpacity("fill-opacity");
                return color.WithAlpha((byte)Math.Round(color.Alpha * opacity));
            }
        }

        private double GetOpacity(string attributeName)
        {
            var attribute = pathNode.Attributes[attributeName];
            if (attribute == null || !double.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity))
                return 1;

            return Math.Clamp(opacity, 0, 1);
        }

        public SKPath Path
        {
            get
            {
                var pathData = pathNode.Attributes["d"].Value;
                return SKPath.ParseSvgPathData(pathData); 
            }
        }
    }   
}