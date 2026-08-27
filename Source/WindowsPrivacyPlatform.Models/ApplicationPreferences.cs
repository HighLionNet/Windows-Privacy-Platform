namespace WindowsPrivacyPlatform.Models;

public enum DefaultModePreference
{
    AskEveryTime,
    Admin,
    ViewOnly
}

public enum AppTheme
{
    WindowsLight,
    SlateLight,
    NavyDark,
    EmberDark
}

/// <summary>
/// User preferences only. Authorization state and credentials are deliberately absent.
/// </summary>
public sealed class ApplicationPreferences
{
    public DefaultModePreference DefaultMode { get; set; } = DefaultModePreference.AskEveryTime;
    public int AdminSessionMinutes { get; set; }
    public AppTheme Theme { get; set; } = AppTheme.WindowsLight;
    public bool StartMaximized { get; set; } = true;
    public bool RememberWindowPosition { get; set; } = true;
    public bool ScanOnLaunch { get; set; } = true;

    public void Normalize()
    {
        if (!Enum.IsDefined(DefaultMode)) DefaultMode = DefaultModePreference.AskEveryTime;
        if (!Enum.IsDefined(Theme)) Theme = AppTheme.WindowsLight;
        if (AdminSessionMinutes is not (0 or 15 or 30 or 60 or 240)) AdminSessionMinutes = 0;
    }
}
