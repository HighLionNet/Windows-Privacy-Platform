using System;
using System.Windows;
using System.Windows.Controls;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>
/// Full decision-support card. Consumes SettingDetailView only — no registry logic.
/// </summary>
public partial class SettingDetailPage : UserControl
{
    public SettingDetailPage(SettingDetailView detail, Action<string> openSetting)
    {
        InitializeComponent();

        TitleText.Text = detail.Title;
        DomainPathText.Text = detail.DomainPath;
        ObjectIdText.Text = $"ObjectId: {detail.ObjectId}";

        WhatText.Text = detail.Explanation.WhatIsIt;
        WhyText.Text = detail.Explanation.WhyItMatters;

        ObservedText.Text = $"Raw / current: {detail.CurrentStateDisplay ?? "Unknown"}";
        EffectiveText.Text = $"Effective: {detail.EffectiveValueDisplay ?? "Unknown"}  [{detail.EffectiveSourceDisplay ?? "Unknown"}]";
        ConfidenceText.Text = $"Confidence: {detail.Confidence}";
        ReasonText.Text = string.IsNullOrWhiteSpace(detail.ResolutionReason)
            ? "No resolution reason available for this setting on the current scan."
            : detail.ResolutionReason;

        foreach (var layer in detail.Layers)
        {
            LayersList.Items.Add(new TextBlock
            {
                Text = $"· {layer.LayerName}: {layer.ValueDisplay}  ({layer.SourcePathDisplay})",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = (System.Windows.Media.Brush)FindResource("BrushTextSecondary")
            });
        }

        ImpactText.Text = detail.Explanation.ImpactLabel + " — " + detail.Explanation.RiskSummary;
        UserImpactText.Text = "User impact: " + (detail.Explanation.UserImpact ?? "—");
        EnterpriseText.Text = "Enterprise context: " + (detail.Explanation.EnterpriseImpact ?? "—");

        if (!string.IsNullOrWhiteSpace(detail.Explanation.CommonMisconceptions))
            MisconceptionText.Text = "Common misconception: " + detail.Explanation.CommonMisconceptions;

        if (!string.IsNullOrWhiteSpace(detail.Explanation.Unknowns))
            UnknownsText.Text = "Unknowns / limits: " + detail.Explanation.Unknowns;

        if (detail.Related.Count == 0)
        {
            RelatedList.Items.Add(new TextBlock
            {
                Text = "No curated relationships for this ObjectId.",
                Foreground = (System.Windows.Media.Brush)FindResource("BrushTextMuted")
            });
        }
        else
        {
            foreach (var rel in detail.Related)
            {
                var btn = new Button
                {
                    Content = $"{HumanRel(rel.Relationship)}: {rel.Title}",
                    Style = (Style)FindResource("SecondaryButton"),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 6),
                    Tag = rel.ObjectId
                };
                var id = rel.ObjectId;
                btn.Click += (_, _) => openSetting(id);
                RelatedList.Items.Add(btn);

                if (!string.IsNullOrWhiteSpace(rel.Explanation))
                {
                    RelatedList.Items.Add(new TextBlock
                    {
                        Text = rel.Explanation,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(8, 0, 0, 10),
                        Foreground = (System.Windows.Media.Brush)FindResource("BrushTextSecondary"),
                        FontSize = 12
                    });
                }
            }
        }
    }

    private static string HumanRel(string kind) => kind switch
    {
        "Overrides" => "Can override",
        "OverriddenBy" => "Controlled by",
        "ConflictsWith" => "Potential conflict with",
        "DependsOn" or "Requires" => "Depends on",
        "Affects" => "Affects",
        "SameFeatureAlternatePath" => "Alternate path",
        "IgnoredWhen" => "Ignored when",
        "AlternativeStorage" => "Alternate storage",
        "UsuallyConfiguredWith" => "Usually configured with",
        _ => "Related to"
    };
}
