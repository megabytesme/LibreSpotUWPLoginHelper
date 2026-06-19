using System;
using System.IO;
using System.Text.Json;

namespace LibreSpotUWPLoginHelper.Services;

internal static class HelperSettings
{
    private const string SettingsFileName = "settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static SettingsData? _cachedSettings;

    public static string SpotifyCustomClientId
    {
        get => Load().SpotifyCustomClientId;
        set
        {
            var settings = Load();
            settings.SpotifyCustomClientId = value?.Trim() ?? string.Empty;
            Save(settings);
        }
    }

    private static SettingsData Load()
    {
        if (_cachedSettings is not null)
            return _cachedSettings;

        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _cachedSettings = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
                return _cachedSettings;
            }
        }
        catch
        {
        }

        _cachedSettings = new SettingsData();
        return _cachedSettings;
    }

    private static void Save(SettingsData settings)
    {
        _cachedSettings = settings;

        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
        }
    }

    private static string SettingsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LibreSpotUWP Login Helper");

    private static string SettingsPath => Path.Combine(SettingsDirectory, SettingsFileName);

    private sealed class SettingsData
    {
        public string SpotifyCustomClientId { get; set; } = string.Empty;
    }
}
