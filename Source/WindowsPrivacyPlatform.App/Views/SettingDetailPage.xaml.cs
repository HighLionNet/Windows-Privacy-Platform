using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>
/// Full decision-support card. Consumes SettingDetailView only — no registry logic.
/// Layered disclosure: primary state first, technical evidence behind expanders.
/// </summary>
public partial class SettingDetailPage : UserControl
{
    private readonly string _objectId;

    public SettingDetailPage(SettingDetailView detail, Action<string> openSetting)
    {
        InitializeComponent();

        _objectId = detail.ObjectId;
        TitleText.Text = detail.Title;
        DomainPathText.Text = detail.DomainPath;
        ObjectIdText.Text = $"ObjectId: {detail.ObjectId}";

        AddBadge(detail.HasConflict ? "Conflict" : null, "BadgeConflict", "BrushConflict");
        AddBadge($"Confidence: {detail.Confidence}", ConfidenceBadgeStyle(detail.Confidence), ConfidenceBrush(detail.Confidence));
        if (detail.RiskLevel == RiskLevel.High)
            AddBadge("High impact tag", "BadgeWarning", "BrushWarning");

        WhatText.Text = detail.Explanation.WhatIsIt;
        WhyText.Text = detail.Explanation.WhyItMatters;

        ObservedText.Text = $"Raw / current: {detail.CurrentStateDisplay ?? "Unknown"}";
        EffectiveText.Text = $"Effective: {detail.EffectiveValueDisplay ?? "Unknown"}  [{detail.EffectiveSourceDisplay ?? "Unknown"}]";
        ConfidenceText.Text = $"Confidence: {detail.Confidence}";
        ReasonText.Text = string.IsNullOrWhiteSpace(detail.ResolutionReason)
            ? "No resolution reason available for this setting on the current scan."
            : detail.ResolutionReason;

        if (detail.Layers.Count == 0)
        {
            LayersList.Items.Add(new TextBlock
            {
                Text = "No layer observations recorded for this ObjectId on the current scan.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        else
        {
            foreach (var layer in detail.Layers)
            {
                LayersList.Items.Add(new TextBlock
                {
                    Text = $"· {layer.LayerName}: {layer.ValueDisplay}  ({layer.SourcePathDisplay})",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0),
                    Foreground = (Brush)FindResource("BrushTextSecondary")
                });
            }
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
                Foreground = (Brush)FindResource("BrushTextMuted")
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
                    Tag = rel.ObjectId,
                    ToolTip = $"Navigate to {rel.ObjectId}"
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
                        Foreground = (Brush)FindResource("BrushTextSecondary"),
                        FontSize = 12
                    });
                }
            }
        }
    }

    private void AddBadge(string? text, string styleKey, string foregroundKey)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var badge = new Border
        {
            Style = (Style)FindResource(styleKey),
            Margin = new Thickness(0, 0, 8, 0)
        };
        badge.Child = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource(foregroundKey)
        };
        BadgeRow.Children.Add(badge);
    }

    private static string ConfidenceBadgeStyle(EffectiveConfidence c) => c switch
    {
        EffectiveConfidence.High => "BadgeSuccess",
        EffectiveConfidence.Medium => "BadgeWarning",
        EffectiveConfidence.Low => "BadgeWarning",
        _ => "BadgeUnknown"
    };

    private static string ConfidenceBrush(EffectiveConfidence c) => c switch
    {
        EffectiveConfidence.High => "BrushSuccess",
        EffectiveConfidence.Medium => "BrushWarning",
        EffectiveConfidence.Low => "BrushWarning",
        _ => "BrushUnknown"
    };

    private void CopyIdButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_objectId);
            CopyIdButton.Content = "Copied";
        }
        catch
        {
            CopyIdButton.Content = "Copy failed";
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
