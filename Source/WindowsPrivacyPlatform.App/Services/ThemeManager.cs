using System.Windows;
using System.Windows.Media;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Services;

public static class ThemeManager
{
    public static AppTheme Current { get; private set; } = AppTheme.WindowsLight;

    public static void Apply(AppTheme theme)
    {
        if (Application.Current is null) return;
        var dictionary = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(resource => resource.Contains("BgWindow"));
        if (dictionary is null) return;

        Current = theme;
        var colors = SystemParameters.HighContrast ? HighContrast() : Palette(theme);
        foreach (var pair in colors)
            dictionary[pair.Key] = (Color)ColorConverter.ConvertFromString(pair.Value);

        // Replace the brush resources as well as their color tokens. WPF can freeze a
        // brush after a StaticResource lookup; replacing it guarantees that controls
        // using DynamicResource (notably the already-open shell) repaint immediately.
        foreach (var pair in BrushKeys)
        {
            if (!colors.TryGetValue(pair.Value, out var value)) continue;
            dictionary[pair.Key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        }

        dictionary["BrushSidebarChrome"] = Gradient(colors["BgSidebar"], colors["SidebarHover"], horizontal: false);
        dictionary["BrushHeaderChrome"] = Gradient(colors["BgContent"], colors["BgHeader"], horizontal: true);
        dictionary["BrushActionGradient"] = ActionGradient(colors["Accent"], colors["AccentCyan"]);
    }

    public static string DisplayName(AppTheme theme) => theme switch
    {
        AppTheme.WindowsLight => "Windows Light",
        AppTheme.SlateLight => "Slate Light",
        AppTheme.NavyDark => "Navy Dark",
        AppTheme.EmberDark => "Ember Dark",
        _ => "Windows Light"
    };

    private static readonly IReadOnlyDictionary<string, string> BrushKeys = new Dictionary<string, string>
    {
        ["BrushBgWindow"] = "BgWindow", ["BrushBgSidebar"] = "BgSidebar", ["BrushBgContent"] = "BgContent",
        ["BrushBgCard"] = "BgCard", ["BrushBgHover"] = "BgHover", ["BrushBgSelected"] = "BgSelected",
        ["BrushBgHeader"] = "BgHeader", ["BrushBgAltRow"] = "BgAltRow", ["BrushBgMenu"] = "BgMenu",
        ["BrushBorder"] = "BorderSubtle", ["BrushBorderStrong"] = "BorderStrong", ["BrushBorderRule"] = "BorderRule",
        ["BrushTextPrimary"] = "TextPrimary", ["BrushTextSecondary"] = "TextSecondary", ["BrushTextMuted"] = "TextMuted",
        ["BrushAccent"] = "Accent", ["BrushAccentHover"] = "AccentHover", ["BrushAccentCyan"] = "AccentCyan",
        ["BrushAccentSoft"] = "AccentSoft", ["BrushSidebarText"] = "SidebarText", ["BrushSidebarMuted"] = "SidebarMuted",
        ["BrushSidebarHover"] = "SidebarHover", ["BrushSidebarSelected"] = "SidebarSelected",
        ["BrushWarning"] = "Warning", ["BrushWarningSoft"] = "WarningSoft", ["BrushError"] = "Error",
        ["BrushErrorHover"] = "ErrorHover", ["BrushSuccess"] = "Success", ["BrushSuccessHover"] = "SuccessHover",
        ["BrushSuccessSoft"] = "SuccessSoft", ["BrushUnknown"] = "Unknown", ["BrushUnknownSoft"] = "UnknownSoft",
        ["BrushConflict"] = "Conflict", ["BrushConflictSoft"] = "ConflictSoft", ["BrushDomainPrivacy"] = "DomainPrivacy",
        ["BrushDomainSecurity"] = "DomainSecurity", ["BrushDomainWindows"] = "DomainWindows", ["BrushDomainApps"] = "DomainApps",
        ["BrushDomainKnowledge"] = "DomainKnowledge"
    };

    private static LinearGradientBrush Gradient(string start, string end, bool horizontal) => new(
        (Color)ColorConverter.ConvertFromString(start), (Color)ColorConverter.ConvertFromString(end),
        horizontal ? new Point(0, 0) : new Point(0, 0), horizontal ? new Point(1, 0) : new Point(1, 1));

    private static LinearGradientBrush ActionGradient(string accent, string cyan)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(accent), 0));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(accent), 0.62));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(cyan), 1));
        return brush;
    }

    private static IReadOnlyDictionary<string, string> Palette(AppTheme theme) => theme switch
    {
        AppTheme.SlateLight => Values(
            "#EEF2F8", "#111827", "#F9FBFF", "#FFFFFF", "#E8EEFF", "#E3E8FF", "#F0F3FB", "#F5F7FC", "#F7F9FD",
            "#D8DEED", "#BDC8DF", "#33445D", "#101827", "#3D4A60", "#6B7890", "#5A55E7", "#4C46CE", "#12B8D4", "#E7E8FF",
            "#F0F5FF", "#93A4C1", "#17263D", "#263B63", "#A96300", "#FFF2D6", "#D13B4B", "#B52C3C", "#0A8F83", "#07756B", "#DDF7F1", "#5A6570", "#EEF0F2", "#E0523D", "#FDE9E5"),
        AppTheme.NavyDark => Values(
            "#101827", "#09111F", "#162236", "#1B2A41", "#253B59", "#27496D", "#182941", "#1D3049", "#111D2E",
            "#334A68", "#4B6587", "#263A54", "#F4F7FB", "#C5D0DF", "#91A3BB", "#4CA6FF", "#72B8FF", "#29C6E2", "#153F68",
            "#F4F7FB", "#91A3BB", "#172A43", "#23496E", "#FFD166", "#4A3B18", "#FF6B6B", "#FF8585", "#2EC4A6", "#4DD7BC", "#153F39", "#AAB5C3", "#29384A", "#FF6B6B", "#4B242A"),
        AppTheme.EmberDark => Values(
            "#1C1518", "#120D10", "#261B20", "#2D2026", "#3A292F", "#4A2B36", "#2A1D23", "#312329", "#21171B",
            "#513842", "#6A4856", "#402C35", "#FFF7F5", "#E6CFCB", "#B89D99", "#F04E3E", "#FF6B5B", "#FF967D", "#522622",
            "#FFF7F5", "#B89D99", "#2B1A20", "#51252D", "#F6C85F", "#4D3B18", "#F04E3E", "#FF6B5B", "#37B88B", "#55C9A1", "#173C31", "#B8A4A7", "#382A2E", "#FF645A", "#4C2426"),
        _ => Values(
            "#F3F6FA", "#0B1424", "#FCFDFF", "#FFFFFF", "#E7F2FC", "#DCEEFF", "#EFF5FB", "#F7F9FC", "#FAFCFE",
            "#D6E0EA", "#AEBECD", "#33445D", "#101827", "#3D4A60", "#67758A", "#0067C0", "#005A9E", "#00A4EF", "#E1F0FF",
            "#F0F5FF", "#93A4C1", "#17263D", "#23436B", "#9A6700", "#FFF4CE", "#C42B1C", "#A4262C", "#0F7B0F", "#0B5A0B", "#DFF6DD", "#5A6570", "#EEF0F2", "#C42B1C", "#FDE7E9")
    };

    private static IReadOnlyDictionary<string, string> HighContrast() => new Dictionary<string, string>
    {
        ["BgWindow"] = SystemColors.WindowColor.ToString(), ["BgSidebar"] = SystemColors.WindowColor.ToString(),
        ["BgContent"] = SystemColors.WindowColor.ToString(), ["BgCard"] = SystemColors.WindowColor.ToString(),
        ["BgHover"] = SystemColors.HighlightColor.ToString(), ["BgSelected"] = SystemColors.HighlightColor.ToString(),
        ["BgHeader"] = SystemColors.ControlColor.ToString(), ["BgAltRow"] = SystemColors.WindowColor.ToString(),
        ["BgMenu"] = SystemColors.MenuColor.ToString(), ["BorderSubtle"] = SystemColors.WindowTextColor.ToString(),
        ["BorderStrong"] = SystemColors.WindowTextColor.ToString(), ["BorderRule"] = SystemColors.WindowTextColor.ToString(),
        ["TextPrimary"] = SystemColors.WindowTextColor.ToString(), ["TextSecondary"] = SystemColors.WindowTextColor.ToString(),
        ["TextMuted"] = SystemColors.GrayTextColor.ToString(), ["Accent"] = SystemColors.HighlightColor.ToString(),
        ["AccentHover"] = SystemColors.HotTrackColor.ToString(), ["AccentCyan"] = SystemColors.HotTrackColor.ToString(),
        ["AccentSoft"] = SystemColors.WindowColor.ToString(), ["SidebarText"] = SystemColors.WindowTextColor.ToString(),
        ["SidebarMuted"] = SystemColors.GrayTextColor.ToString(), ["SidebarHover"] = SystemColors.HighlightColor.ToString(),
        ["SidebarSelected"] = SystemColors.HighlightColor.ToString(), ["Warning"] = SystemColors.WindowTextColor.ToString(),
        ["WarningSoft"] = SystemColors.WindowColor.ToString(), ["Error"] = SystemColors.WindowTextColor.ToString(),
        ["ErrorHover"] = SystemColors.HotTrackColor.ToString(), ["Success"] = SystemColors.WindowTextColor.ToString(),
        ["SuccessHover"] = SystemColors.HotTrackColor.ToString(), ["SuccessSoft"] = SystemColors.WindowColor.ToString(),
        ["Unknown"] = SystemColors.GrayTextColor.ToString(), ["UnknownSoft"] = SystemColors.WindowColor.ToString(),
        ["Conflict"] = SystemColors.WindowTextColor.ToString(), ["ConflictSoft"] = SystemColors.WindowColor.ToString(),
        ["DomainPrivacy"] = SystemColors.WindowTextColor.ToString(), ["DomainSecurity"] = SystemColors.WindowTextColor.ToString(),
        ["DomainWindows"] = SystemColors.WindowTextColor.ToString(), ["DomainApps"] = SystemColors.WindowTextColor.ToString(),
        ["DomainKnowledge"] = SystemColors.WindowTextColor.ToString()
    };

    private static IReadOnlyDictionary<string, string> Values(params string[] values)
    {
        var keys = new[] { "BgWindow", "BgSidebar", "BgContent", "BgCard", "BgHover", "BgSelected", "BgHeader", "BgAltRow", "BgMenu",
            "BorderSubtle", "BorderStrong", "BorderRule", "TextPrimary", "TextSecondary", "TextMuted", "Accent", "AccentHover", "AccentCyan", "AccentSoft",
            "SidebarText", "SidebarMuted", "SidebarHover", "SidebarSelected", "Warning", "WarningSoft", "Error", "ErrorHover", "Success", "SuccessHover",
            "SuccessSoft", "Unknown", "UnknownSoft", "Conflict", "ConflictSoft" };
        var result = keys.Zip(values).ToDictionary(pair => pair.First, pair => pair.Second);
        result["DomainPrivacy"] = themeColor(result["AccentCyan"]);
        result["DomainSecurity"] = themeColor(result["Error"]);
        result["DomainWindows"] = themeColor(result["Success"]);
        result["DomainApps"] = result["Accent"];
        result["DomainKnowledge"] = result["Warning"];
        return result;

        static string themeColor(string value) => value;
    }
}
