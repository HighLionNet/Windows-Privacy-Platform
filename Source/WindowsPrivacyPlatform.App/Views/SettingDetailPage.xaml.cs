using System;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>
/// Minimal setting detail: status + one organized explanation paragraph.
/// Changes are applied from the category list, not this page.
/// </summary>
public partial class SettingDetailPage : UserControl
{
    public SettingDetailPage(SettingDetailView detail, Action<string> openSetting)
    {
        InitializeComponent();

        TitleText.Text = detail.Title;
        DomainPathText.Text = detail.DomainPath;

        ObservedText.Text = detail.CurrentStateDisplay ?? "Unknown";
        EffectiveText.Text = detail.EffectiveValueDisplay ?? "Unknown";
        if (detail.HasConflict)
            EffectiveText.Foreground = (Brush)FindResource("BrushConflict");

        SourceText.Text = detail.EffectiveSourceDisplay ?? "Unknown";

        ExplanationText.Text = BuildExplanation(detail);
    }

    private static string BuildExplanation(SettingDetailView detail)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(detail.Explanation.WhatIsIt))
            sb.Append(detail.Explanation.WhatIsIt.Trim());

        if (!string.IsNullOrWhiteSpace(detail.Explanation.WhyItMatters))
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(detail.Explanation.WhyItMatters.Trim());
        }

        if (!string.IsNullOrWhiteSpace(detail.Explanation.RiskSummary))
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(detail.Explanation.RiskSummary.Trim());
        }

        if (!string.IsNullOrWhiteSpace(detail.Explanation.CommonMisconceptions))
        {
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append("Note: ");
            sb.Append(detail.Explanation.CommonMisconceptions.Trim());
        }

        if (!string.IsNullOrWhiteSpace(detail.ResolutionReason))
        {
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append(detail.ResolutionReason.Trim());
        }

        if (sb.Length == 0)
            return "No additional explanation is available for this setting on the current scan.";

        return sb.ToString();
    }
}
