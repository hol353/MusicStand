using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MuPDFCore;

namespace MusicStand;

/// <summary>
/// Represents a title (PDF file) in the music library, which contains pages and links between markers.
/// </summary>
public class Title
{
    private List<Page> pages = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pdfPath"></param>
    public Title(string pdfPath)
    {
        PDFPath = pdfPath;
        Load();
    }

    /// <summary>
    /// The path of the file relative to the base path.
    /// </summary>
    public string PDFPath { get; }

    /// <summary>
    /// The collection of pages in the file.
    /// </summary>
    public IEnumerable<Page> Pages => pages;

    /// <summary>
    /// The links between markers.
    /// </summary>
    public List<Link> Links { get; set; } = new();

    /// <summary>
    /// Create a title instance for a pdf file.
    /// </summary>
    /// <param name="pdfFilePath">The path of the pdf file.</param>
    private void Load()
    {
        if (File.Exists(PDFPath))
        {
            // Initialise the MuPDF context. This is needed to open or create documents.
            using MuPDFContext ctx = new MuPDFContext();

            // Open a PDF document
            using MuPDFDocument document = new MuPDFDocument(ctx, PDFPath);

            // Convert each page of the PDf to a MusicPage instance.
            for (int page = 0; page < document.Pages.Length; page++)
            {
                var pageBounds = document.Pages[page].Bounds;
                var pixelSize = new PixelSize(
                    (int)Math.Ceiling(pageBounds.Width * 1.7),
                    (int)Math.Ceiling(pageBounds.Height * 1.7));
                var bitmap = new WriteableBitmap(
                    pixelSize,
                    new Vector(96, 96),
                    Avalonia.Platform.PixelFormats.Rgba8888,
                    AlphaFormat.Unpremul);

                using (var framebuffer = bitmap.Lock())
                    document.Render(page, 1.7, MuPDFCore.PixelFormats.RGBA, framebuffer.Address, false);

                pages.Add(new Page(pageNumber: page + 1, bitmap));
            }
        }
    }  
}
