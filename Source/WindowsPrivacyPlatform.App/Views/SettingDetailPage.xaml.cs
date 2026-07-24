using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>
/// Property-sheet style detail surface. Consumes SettingDetailView only.
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
            AddBadge("High impact", "BadgeWarning", "BrushWarning");

        WhatText.Text = detail.Explanation.WhatIsIt;
        WhyText.Text = detail.Explanation.WhyItMatters;

        ObservedText.Text = detail.CurrentStateDisplay ?? "Unknown";
        EffectiveText.Text = $"{detail.EffectiveValueDisplay ?? "Unknown"}  [{detail.EffectiveSourceDisplay ?? "Unknown"}]";
        ConfidenceText.Text = detail.Confidence.ToString();
        ReasonText.Text = string.IsNullOrWhiteSpace(detail.ResolutionReason)
            ? "No resolution reason available on the current scan."
            : detail.ResolutionReason;

        if (detail.Layers.Count == 0)
        {
            LayersList.Items.Add(new TextBlock
            {
                Text = "No layer observations recorded for this ObjectId.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                TextWrapping = TextWrapping.Wrap
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
                    Foreground = (Brush)FindResource("BrushTextSecondary"),
                    FontSize = 12
                });
            }
        }

        ImpactText.Text = detail.Explanation.ImpactLabel + " — " + detail.Explanation.RiskSummary;
        UserImpactText.Text = "User: " + (detail.Explanation.UserImpact ?? "—");
        EnterpriseText.Text = "Enterprise: " + (detail.Explanation.EnterpriseImpact ?? "—");

        if (!string.IsNullOrWhiteSpace(detail.Explanation.CommonMisconceptions))
            MisconceptionText.Text = "Misconception: " + detail.Explanation.CommonMisconceptions;

        if (!string.IsNullOrWhiteSpace(detail.Explanation.Unknowns))
            UnknownsText.Text = "Limits: " + detail.Explanation.Unknowns;

        if (detail.Related.Count == 0)
        {
            RelatedList.Items.Add(new TextBlock
            {
                Text = "No curated relationships.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(10, 8, 10, 8)
            });
        }
        else
        {
            foreach (var rel in detail.Related)
            {
                var row = new Border { Style = (Style)FindResource("ListRow") };
                var panel = new StackPanel();
                panel.Children.Add(new TextBlock
                {
                    Text = $"{HumanRel(rel.Relationship)} · {rel.Title}",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12
                });
                if (!string.IsNullOrWhiteSpace(rel.Explanation))
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = rel.Explanation,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 0),
                        Foreground = (Brush)FindResource("BrushTextSecondary"),
                        FontSize = 11
                    });
                }
                row.Child = panel;
                var id = rel.ObjectId;
                row.MouseLeftButtonUp += (_, _) => openSetting(id);
                row.ToolTip = $"Open {rel.ObjectId}";
                RelatedList.Items.Add(row);
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
            Margin = new Thickness(0, 0, 6, 0),
            BorderBrush = (Brush)FindResource("BrushBorder"),
            BorderThickness = new Thickness(1)
        };
        badge.Child = new TextBlock
        {
            Text = text,
            FontSize = 10,
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
        "ConflictsWith" => "Potential conflict",
        "DependsOn" or "Requires" => "Depends on",
        "Affects" => "Affects",
        "SameFeatureAlternatePath" => "Alternate path",
        "IgnoredWhen" => "Ignored when",
        "AlternativeStorage" => "Alternate storage",
        "UsuallyConfiguredWith" => "Usually configured with",
        _ => "Related"
    };
}
