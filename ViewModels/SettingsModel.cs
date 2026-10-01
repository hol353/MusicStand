using System;
using System.IO;
using System.Text.Json;

namespace MusicStand;

/// <summary>
/// Represents the application settings.
/// </summary>
public class SettingsModel
{
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

    /// <summary>The currently selected stamp index.</summary>
    public int SelectedStampIndex { get; set; } = 0;

    /// <summary>
    /// Load the application settings from disk.
    /// </summary>
    /// <returns>An instance of the settings model.</returns>
    public static SettingsModel Create(string BaseDirectory)
    {
        try
        {
            Directory.CreateDirectory(BaseDirectory);
            var path = Path.Combine(BaseDirectory, "settings.json");
            if (!File.Exists(path))
                throw new Exception($"{BaseDirectory}/settings.json file does not exist");

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SettingsModel>(json, opts) ?? new SettingsModel();
        }
        catch (Exception)
        {
            string path = Path.Combine(BaseDirectory, "Library");
            Directory.CreateDirectory(path);
            return new SettingsModel() 
            { 
                MusicLibraryBaseDirectory = path
            };
        }
    }

    /// <summary>
    /// Saves the current settings to disk.
    /// </summary>
    /// <param name="BaseDirectory"></param>
    /// <param name="settings"></param>
    public void Save(string BaseDirectory)
    {
        Directory.CreateDirectory(BaseDirectory);
        var path = Path.Combine(BaseDirectory, "settings.json");
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(this, opts);
        File.WriteAllText(path, json);
    }
}