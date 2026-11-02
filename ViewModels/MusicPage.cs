using System.Collections.Generic;
using Avalonia;
using Avalonia.Media.Imaging;

namespace MusicStand;

public class MusicPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MusicPage"/> class.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="bitmap"></param>
    /// <param name="pageNumber"></param>
    public MusicPage(MusicStore store, Bitmap bitmap, int pageNumber)
    {
        Store = store;
        this.Bitmap = bitmap;
        PageNumber = pageNumber;
        Annotations = store.GetAnnotations(pageNumber);
    }

    internal MusicStore Store { get; }

    public int PageNumber { get; }

    public Bitmap Bitmap { get; }

    public Annotations Annotations { get; set; }   

    /// <summary>
    /// Save annotations for all pages.
    /// </summary>
    public void Save()
    {
        Store.SetAnnotations(PageNumber, Annotations);
    }    

}