using System;
using System.IO;
using System.Text.Json;

namespace MusicStand;

public class SettingsPersistence
{
    /// <summary>
    /// Load the application settings from disk.
    /// </summary>
    /// <returns>An instance of the settings model.</returns>    
    public static Settings Read(string homeDirectory)
    {
        try
        {
            Directory.CreateDirectory(homeDirectory);
            var path = Path.Combine(homeDirectory, "settings.json");
            if (File.Exists(path))
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Settings>(json, opts) ?? new Settings();
            }
            return new()
            {
                HomeDirectory = homeDirectory    
            };
        }
        catch (Exception)
        {
            string path = Path.Combine(homeDirectory, "Library");
            Directory.CreateDirectory(path);
            return new Settings() 
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
    public static void Write(Settings settings, string homeDirectory)
    {
        Directory.CreateDirectory(homeDirectory);
        var path = Path.Combine(homeDirectory, "settings.json");
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(settings, opts);
        File.WriteAllText(path, json);
    }
}