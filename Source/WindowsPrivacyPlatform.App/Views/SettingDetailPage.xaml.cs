using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>
/// Setting detail: primary state, compact options, always-visible minimal knowledge blocks.
/// Consumes SettingDetailView only. v1.5: no expanders.
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
        EffectiveText.Text = detail.EffectiveValueDisplay ?? "Unknown";
        if (detail.HasConflict)
            EffectiveText.Foreground = (Brush)FindResource("BrushConflict");

        SourceText.Text = detail.EffectiveSourceDisplay ?? "Unknown";
        ConfidenceText.Text = detail.Confidence.ToString();
        ReasonText.Text = string.IsNullOrWhiteSpace(detail.ResolutionReason)
            ? "No resolution reason available on the current scan."
            : detail.ResolutionReason;

        PopulateOptions(detail);

        ImpactText.Text = detail.Explanation.ImpactLabel + " — " + detail.Explanation.RiskSummary;

        if (!string.IsNullOrWhiteSpace(detail.Explanation.CommonMisconceptions))
            MisconceptionText.Text = "Misconception: " + detail.Explanation.CommonMisconceptions;
        else
            MisconceptionText.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrWhiteSpace(detail.Explanation.Unknowns))
            UnknownsText.Text = "Limits: " + detail.Explanation.Unknowns;
        else
            UnknownsText.Visibility = Visibility.Collapsed;

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
                    FontSize = 12,
                    FontFamily = new FontFamily("Cascadia Code, Consolas, Segoe UI")
                });
            }
        }

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

    private void PopulateOptions(SettingDetailView detail)
    {
        if (detail.Options.Count == 0)
        {
            OptionsSheet.Visibility = Visibility.Collapsed;
            return;
        }

        OptionsSheet.Visibility = Visibility.Visible;

        var header = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.Children.Add(MakeHeaderCell("Value", 0));
        header.Children.Add(MakeHeaderCell("Label", 1));
        header.Children.Add(MakeHeaderCell("Meaning", 2));
        OptionsList.Items.Add(header);

        foreach (var opt in detail.Options)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var raw = new TextBlock
            {
                Text = opt.RawValue,
                FontSize = 12,
                FontFamily = new FontFamily("Cascadia Code, Consolas, Segoe UI"),
                Foreground = (Brush)FindResource("BrushTextPrimary")
            };
            Grid.SetColumn(raw, 0);
            row.Children.Add(raw);

            var label = new TextBlock
            {
                Text = opt.Label,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("BrushTextPrimary")
            };
            Grid.SetColumn(label, 1);
            row.Children.Add(label);

            if (!string.IsNullOrWhiteSpace(opt.Description))
            {
                var desc = new TextBlock
                {
                    Text = opt.Description,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)FindResource("BrushTextSecondary")
                };
                Grid.SetColumn(desc, 2);
                row.Children.Add(desc);
            }

            OptionsList.Items.Add(row);
        }
    }

    private TextBlock MakeHeaderCell(string text, int column)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("BrushTextMuted")
        };
        Grid.SetColumn(tb, column);
        return tb;
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
