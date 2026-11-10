namespace MusicStand;

/// <summary>
/// Represents the application settings.
/// </summary>
public class Settings
{
    /// <summary>
    /// The directory where the application stores settings/annotations.
    /// </summary>
    public string HomeDirectory { get; set; }

    /// <summary>
    /// Top level dirctory of music library (with no trailing space).
    /// </summary>
    public string MusicLibraryBaseDirectory { get; set; }

    /// <summary>
    /// The currently selected directory (relative to MusicLibraryBaseDirectory).
    /// </summary>
    public string SelectedDirectory { get; set; }

    /// <summary>The currently selected pen color index.</summary>
    public int SelectedPenColorIndex { get; set; } = 0;

    /// <summary>The currently selected highlighter color index.</summary>
    public int SelectedHighlighterColorIndex { get; set; } = 1;

    /// <summary>The currently selected link color index.</summary>
    public int SelectedLinkColorIndex { get; set; } = 2;

    /// <summary>The currently selected stamp index.</summary>
    public int SelectedStampIndex { get; set; } = 0;

    /// <summary>The currently selected setlist.</summary>
    public string SelectedSetList { get; set; }

    /// <summary>The currently selected text stamp.</summary>
    public string TextStamp { get; set; }
}