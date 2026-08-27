using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class SettingDetailPage : UserControl
{
    public SettingDetailPage(SettingDetailView detail, Action<string> openSetting)
    {
        InitializeComponent();
        TitleText.Text = detail.Title;
        DomainPathText.Text = detail.Bucket == CatalogBucket.SystemInventory
            ? "System Explorer · observed component"
            : detail.DomainPath;
        ObservedText.Text = detail.CurrentStateDisplay ?? "Unknown";
        EffectiveText.Text = detail.EffectiveValueDisplay ?? "Unknown";
        SourceText.Text = detail.EffectiveSourceDisplay ?? "Unknown";
        if (detail.HasConflict) EffectiveText.Foreground = (Brush)FindResource("BrushConflict");

        SummaryText.Text = string.IsNullOrWhiteSpace(detail.Narrative.Summary)
            ? detail.Explanation.WhatIsIt
            : detail.Narrative.Summary;
        TechnicalLocationText.Text = TechnicalLocationFormatter.DirectPath(detail.TechnicalLocation);
        WhatText.Text = detail.Explanation.WhatIsIt;
        WhyText.Text = detail.Explanation.WhyItMatters;
        GuidanceText.Text = detail.Explanation.DecisionGuidance;
        TradeoffsText.Text = $"Privacy: {detail.Explanation.PrivacyImpactText}\nSecurity: {detail.Explanation.SecurityImpactText}\nSide effects: {detail.Explanation.SideEffects}";
        RestartText.Text = detail.Applicability == ApplicabilityState.Applicable
            ? $"Supported on this scanned device. Restart expectation: {detail.RestartExpectation}."
            : detail.ApplicabilityReason;
        VerificationText.Text = $"Object ID: {detail.ObjectId}\nEvidence confidence: {detail.Confidence}\nResolution: {detail.ResolutionReason ?? "No additional resolution explanation."}";

        if (detail.Bucket == CatalogBucket.SystemInventory)
        {
            AccessBadgeText.Text = "READ ONLY";
            AccessBadge.Style = (Style)FindResource("BadgeUnknown");
            OptionsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            AccessBadgeText.Text = detail.IsWritable ? "CATALOG EDITABLE" : "VIEW ONLY";
            if (!detail.IsWritable) AccessBadge.Style = (Style)FindResource("BadgeUnknown");
            OptionsList.ItemsSource = detail.Options;
        }

        if (detail.Applicability != ApplicabilityState.Applicable)
        {
            ApplicabilityPanel.Visibility = Visibility.Visible;
            ApplicabilityText.Text = detail.ApplicabilityReason;
        }
    }
}
