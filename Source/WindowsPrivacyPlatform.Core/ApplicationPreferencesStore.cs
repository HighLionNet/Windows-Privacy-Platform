using System.Text.Json;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Core;

public sealed class ApplicationPreferencesStore
{
    private const int MaximumPreferenceBytes = 32 * 1024;
    private readonly string _root;
    private readonly string _path;

    public ApplicationPreferencesStore(string root)
    {
        _root = Path.GetFullPath(root ?? throw new ArgumentNullException(nameof(root)));
        _path = Path.Combine(_root, "app-settings.json");
    }

    public ApplicationPreferences Load()
    {
        try
        {
            var lines = AtomicLocalFile.ReadAllLines(_root, _path, MaximumPreferenceBytes);
            if (lines.Length == 0) return new ApplicationPreferences();
            var result = JsonSerializer.Deserialize<ApplicationPreferences>(string.Join(Environment.NewLine, lines))
                         ?? new ApplicationPreferences();
            result.Normalize();
            return result;
        }
        catch
        {
            return new ApplicationPreferences();
        }
    }

    public void Save(ApplicationPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        preferences.Normalize();
        var json = JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true });
        AtomicLocalFile.WriteText(_root, _path, json + Environment.NewLine);
    }

    public static string SerializeForInspection(ApplicationPreferences preferences) =>
        JsonSerializer.Serialize(preferences ?? throw new ArgumentNullException(nameof(preferences)));
}
