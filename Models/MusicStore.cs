using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Avalonia.Media;

namespace MusicStand;

public sealed record MusicLinkEndpoint(string PdfPath, int PageNumber, double X, double Y, Color Color);

public sealed record MusicLink(MusicLinkEndpoint First, MusicLinkEndpoint Second);

/// <summary>
/// Stores a PDF's page annotations and links in a single XML file.
/// </summary>
public sealed class MusicStore
{
    private readonly string filePath;
    private readonly XmlDocument document;
    private readonly List<MusicLink> links = new();

    private MusicStore(string filePath, string pdfPath)
    {
        this.filePath = filePath;
        PdfPath = pdfPath;
        document = new XmlDocument();

        if (File.Exists(filePath))
            document.Load(filePath);
        else
            document.AppendChild(document.CreateElement("store"));

        if (document.DocumentElement?.Name != "store")
            throw new XmlException($"The music store '{filePath}' must have a <store> root element.");

        EnsureSection("annotations");
        EnsureSection("links");
        LoadLinks();
    }

    /// <summary>
    /// Gets the PDF path relative to the music library, with forward slashes.
    /// </summary>
    public string PdfPath { get; }

    /// <summary>
    /// Gets the links saved in this store.
    /// </summary>
    public IReadOnlyList<MusicLink> Links => links;

    public static MusicStore Create(MainViewModel model, string pdfPath)
    {
        var pdfRelativeDirectory = Path.GetDirectoryName(pdfPath)
            .ToRelative(model.Settings.MusicLibraryBaseDirectory);
        var pdfFileName = Path.GetFileNameWithoutExtension(pdfPath);
        var storeDirectory = Path.Combine(model.BaseDirectory, "Annotations", pdfRelativeDirectory);
        var storeFilePath = Path.Combine(storeDirectory, $"{pdfFileName}.xml");
        var relativePdfPath = Path.GetRelativePath(model.Settings.MusicLibraryBaseDirectory, pdfPath)
            .Replace('\\', '/');

        return new MusicStore(storeFilePath, relativePdfPath);
    }

    public Annotations GetAnnotations(int pageNumber)
    {
        var page = GetSection("annotations").ChildNodes
            .OfType<XmlElement>()
            .FirstOrDefault(element => element.Name == "page" &&
                XmlConvert.ToInt32(element.GetAttribute("number")) == pageNumber);
        var svg = page?.ChildNodes.OfType<XmlElement>().FirstOrDefault(element => element.LocalName == "svg");
        return svg == null ? null : Annotations.Create(svg);
    }

    public void SetAnnotations(int pageNumber, Annotations annotations)
    {
        var annotationSection = GetSection("annotations");
        var currentPage = annotationSection.ChildNodes
            .OfType<XmlElement>()
            .FirstOrDefault(element => element.Name == "page" &&
                XmlConvert.ToInt32(element.GetAttribute("number")) == pageNumber);
        if (currentPage != null)
            annotationSection.RemoveChild(currentPage);

        if (annotations == null)
            return;

        var page = document.CreateElement("page");
        page.SetAttribute("number", XmlConvert.ToString(pageNumber));
        page.AppendChild(document.ImportNode(annotations.SvgElement, true));

        var nextPage = annotationSection.ChildNodes
            .OfType<XmlElement>()
            .FirstOrDefault(element => element.Name == "page" &&
                XmlConvert.ToInt32(element.GetAttribute("number")) > pageNumber);
        annotationSection.InsertBefore(page, nextPage);
    }

    public void AddLink(MusicLink link)
    {
        if (!links.Contains(link))
            links.Add(link);
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        WriteLinks();
        using var writer = XmlWriter.Create(filePath, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true
        });
        document.Save(writer);
    }

    private XmlElement EnsureSection(string name)
    {
        var section = GetSection(name);
        if (section != null)
            return section;

        section = document.CreateElement(name);
        document.DocumentElement.AppendChild(section);
        return section;
    }

    private XmlElement GetSection(string name)
    {
        return document.DocumentElement.ChildNodes
            .OfType<XmlElement>()
            .FirstOrDefault(element => element.Name == name);
    }

    private void LoadLinks()
    {
        foreach (var linkElement in GetSection("links").ChildNodes
            .OfType<XmlElement>()
            .Where(element => element.Name == "link"))
        {
            var first = linkElement.ChildNodes.OfType<XmlElement>()
                .First(element => element.Name == "first");
            var second = linkElement.ChildNodes.OfType<XmlElement>()
                .First(element => element.Name == "second");
            links.Add(new MusicLink(ReadEndpoint(first), ReadEndpoint(second)));
        }
    }

    private static MusicLinkEndpoint ReadEndpoint(XmlElement element)
    {
        var pdfPath = element.GetAttribute("pdf");
        var pageNumber = XmlConvert.ToInt32(element.GetAttribute("page"));
        var x = XmlConvert.ToDouble(element.GetAttribute("x"));
        var y = XmlConvert.ToDouble(element.GetAttribute("y"));
        if (string.IsNullOrWhiteSpace(pdfPath) ||
            pageNumber < 1 ||
            !double.IsFinite(x) ||
            !double.IsFinite(y) ||
            x < 0 || x > 1 ||
            y < 0 || y > 1)
            throw new XmlException("A music store link endpoint contains invalid PDF or page coordinates.");

        return new MusicLinkEndpoint(pdfPath, pageNumber, x, y, Color.Parse(element.GetAttribute("color")));
    }

    private void WriteLinks()
    {
        var linkSection = GetSection("links");
        linkSection.RemoveAll();
        foreach (var link in links)
        {
            var linkElement = document.CreateElement("link");
            linkElement.AppendChild(WriteEndpoint("first", link.First));
            linkElement.AppendChild(WriteEndpoint("second", link.Second));
            linkSection.AppendChild(linkElement);
        }
    }

    private XmlElement WriteEndpoint(string name, MusicLinkEndpoint endpoint)
    {
        var element = document.CreateElement(name);
        element.SetAttribute("pdf", endpoint.PdfPath);
        element.SetAttribute("page", XmlConvert.ToString(endpoint.PageNumber));
        element.SetAttribute("x", XmlConvert.ToString(endpoint.X));
        element.SetAttribute("y", XmlConvert.ToString(endpoint.Y));
        element.SetAttribute("color", endpoint.Color.ToString());
        return element;
    }
}
