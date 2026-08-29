using System.Windows;
using System.Windows.Media;
using WindowsPrivacyPlatform.Models;
using Wpf.Ui.Appearance;

namespace WindowsPrivacyPlatform.App.Services;

public static class ThemeManager
{
    public static AppTheme Current { get; private set; } = AppTheme.WindowsLight;

    public static void Apply(AppTheme theme)
    {
        if (Application.Current is null) return;
        var fluentTheme = SystemParameters.HighContrast
            ? ApplicationTheme.HighContrast
            : IsDark(theme) ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(fluentTheme);
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
        dictionary["BrushHeaderChrome"] = Gradient(colors["BgHeader"], colors["BgSelected"], horizontal: true);
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

    internal static IReadOnlyDictionary<string, string> PaletteForTesting(AppTheme theme) => Palette(theme);

    private static bool IsDark(AppTheme theme) => theme is AppTheme.NavyDark or AppTheme.EmberDark;

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
            "#D1DEED", "#151D3C", "#E3EAF7", "#F2F5FD", "#D3DDF6", "#BFCDF4", "#CED8F2", "#DCE4F5", "#D4DDF2",
            "#A7B6DB", "#667EA8", "#364E84", "#111C38", "#334568", "#5B6C8B", "#3B50C5", "#2D3E9B", "#006F8F", "#CED5FF",
            "#F7F8FF", "#C8D2F3", "#222D59", "#30408A", "#955A00", "#FFE5AC", "#B92F4D", "#93223C", "#00765F", "#005C4A", "#BDEADB", "#596B8A", "#DDE4F2", "#CA4437", "#F5D3CD"),
        AppTheme.NavyDark => Values(
            "#142033", "#0C1727", "#19283B", "#1F3046", "#294058", "#31516B", "#1B2C42", "#22354C", "#111E30",
            "#314A64", "#4D6A86", "#34516B", "#F7FAFF", "#D7E2F0", "#ABC0D6", "#6F9FCB", "#84B2DA", "#5FC5C9", "#233F5C",
            "#F7FAFF", "#B4C7D9", "#172B43", "#254561", "#F5C66B", "#463A22", "#FF7A7A", "#FF9797", "#52D2AD", "#75E0C2", "#1C473F", "#C3CFDD", "#2A3A4B", "#FF836F", "#4A2C32"),
        AppTheme.EmberDark => Values(
            "#211619", "#120D0F", "#281B20", "#322128", "#432B34", "#542D39", "#2D1E24", "#352329", "#1C1317",
            "#51363F", "#75515D", "#5B3944", "#FFF9F7", "#FFE1DA", "#D8B6B0", "#F36A4D", "#FF8165", "#F0A06F", "#552C27",
            "#FFF8F5", "#D9B9B4", "#2C1B21", "#552733", "#FFD477", "#4F3C1D", "#FF7063", "#FF9086", "#5BD4A6", "#7AE3BD", "#1E493A", "#D8C1BE", "#3B2A2F", "#FF7467", "#55292A"),
        _ => Values(
            "#C8E2F2", "#061B35", "#DFF1FA", "#F2FAFE", "#C4E7F6", "#9FD8EF", "#C5E7F6", "#D5EDF8", "#CBE8F6",
            "#8FC5DC", "#4C91AF", "#145E7E", "#062538", "#214D61", "#476B7B", "#006BA8", "#005484", "#006F89", "#AEE1F2",
            "#F5FBFF", "#B3D8E8", "#0D3A5D", "#0D5274", "#A35500", "#FFE1A3", "#C32F46", "#9D1F34", "#007A62", "#005F4D", "#BDECDC", "#526D7A", "#D6E6EC", "#D04432", "#F6CDC5")
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
        // Domain labels sit on the dark navigation rail in every palette. Keep a
        // dedicated, high-luminance identity set instead of reusing body accents.
        result["DomainPrivacy"] = "#4DDCF4";
        result["DomainSecurity"] = "#FF8D86";
        result["DomainWindows"] = "#52DDA7";
        result["DomainApps"] = "#C5AEFF";
        result["DomainKnowledge"] = "#FFD56F";
        return result;
    }
}
