using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MuPDFCore;

namespace MusicStand;

public class MusicFile
{
    /// <summary>
    /// The path of the file.
    /// </summary>
    private string path;
    private MainViewModel model;


    /// <summary>
    /// Initializes a new instance of the <see cref="MusicFile"/> class.
    /// </summary>
    /// <param name="path"></param>
    public MusicFile(MainViewModel model, string path)
    {
        this.path = path;
        this.model = model;
        Store = MusicStore.Create(model, path);
        Load(path);
    }

    public MusicStore Store { get; }

    /// <summary>
    /// The list of pages in the file.
    /// </summary>
    public List<MusicPage> Pages { get; } = new();   

    /// <summary>
    /// Called when the music file is closed.
    /// </summary>
    public void Save()
    {
        foreach (var page in Pages)
            page.Save();
        Store.Save();
    }

    /// <summary>
    /// Load a pdf file.
    /// </summary>
    /// <param name="pdfFilePath">The path of the pdf file.</param>
    private void Load(string pdfFilePath)
    {
        if (File.Exists(pdfFilePath))
        {
            // Initialise the MuPDF context. This is needed to open or create documents.
            using MuPDFContext ctx = new MuPDFContext();

            // Open a PDF document
            using MuPDFDocument document = new MuPDFDocument(ctx, pdfFilePath);

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

                Pages.Add(new MusicPage(Store, bitmap, page + 1));
            }
        }
    }    
}
