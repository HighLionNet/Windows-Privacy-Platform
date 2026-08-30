using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>Shared full-width setting bar used by category, conflict, and detail actions.</summary>
public sealed class SettingBar : Border
{
    public SettingBar(
        ManagedObject item,
        bool administrator,
        bool busy,
        string windowsVersion,
        string edition,
        bool hasPending,
        string? pendingRaw,
        Action<string?>? stage,
        Action? openDetails,
        ConflictGroup? conflict,
        string? conflictOtherName,
        Action<string>? openConflict)
    {
        ArgumentNullException.ThrowIfNull(item);
        Margin = new Thickness(0, 0, 0, 6);
        Padding = new Thickness(11, 8, 11, 9);
        Background = Brush("BrushBgCard");
        BorderBrush = Brush("BrushBorderStrong");
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(4);

        var evidence = EvidenceStateSemantics.Classify(item);
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(new TextBlock
        {
            Text = item.ObjectName,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("BrushTextPrimary"),
            TextWrapping = TextWrapping.Wrap
        });
        title.Children.Add(new TextBlock
        {
            Text = Introduction(item),
            FontSize = 11.5,
            Foreground = Brush("BrushTextSecondary"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            ToolTip = Introduction(item),
            Margin = new Thickness(0, 2, 12, 0)
        });
        header.Children.Add(title);

        var badge = new Border
        {
            Style = ResourceStyle(evidence is EvidenceState.AccessDenied or EvidenceState.Error ? "BadgeConflict" :
                evidence is EvidenceState.Unknown or EvidenceState.NotObserved or EvidenceState.Stale ? "BadgeWarning" :
                evidence == EvidenceState.Configured ? "BadgeSuccess" : "BadgeUnknown"),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10, 0, 0, 0)
        };
        badge.Child = new TextBlock
        {
            Text = !item.IsApplicableHere ? "NOT ON THIS PC" : EvidenceStateSemantics.Label(evidence).ToUpperInvariant(),
            FontSize = 9,
            FontWeight = FontWeights.Bold
        };
        Grid.SetColumn(badge, 1);
        header.Children.Add(badge);
        root.Children.Add(header);

        var body = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });

        var links = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        links.Children.Add(new TextBlock
        {
            Text = "Current: " + CurrentLabel(item),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("BrushTextPrimary"),
            TextWrapping = TextWrapping.Wrap
        });
        if (conflict is not null && openConflict is not null)
        {
            var otherName = string.IsNullOrWhiteSpace(conflictOtherName) ? conflict.Family : conflictOtherName;
            var chip = new Button
            {
                Content = new TextBlock
                {
                    Text = "⚠ Conflicts with: " + otherName,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Left
                },
                Style = ResourceStyle("ActionDanger"),
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(7, 3, 7, 3),
                Margin = new Thickness(0, 7, 0, 0),
                ToolTip = conflict.OutcomeLine
            };
            chip.Click += (_, _) => openConflict(conflict.GroupId);
            links.Children.Add(chip);
        }
        if (openDetails is not null)
        {
            var details = new Button
            {
                Content = "Details",
                Style = ResourceStyle("ActionNeutral"),
                HorizontalAlignment = HorizontalAlignment.Left,
                MinWidth = 92,
                Margin = new Thickness(0, 7, 0, 0)
            };
            details.Click += (_, _) => openDetails();
            links.Children.Add(details);
        }
        body.Children.Add(links);

        var choices = new WrapPanel { Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
        var options = Options(item, windowsVersion, edition);
        foreach (var option in options)
        {
            var selected = hasPending && string.Equals(option.Raw, pendingRaw, StringComparison.OrdinalIgnoreCase);
            var current = string.Equals(option.Raw, Raw(item.CurrentState), StringComparison.OrdinalIgnoreCase) ||
                          option.Raw is null && IsNotConfigured(item.CurrentState);
            var button = new Button
            {
                Content = option.Label,
                Style = ResourceStyle(selected || current ? "ActionChoiceSelected" : ChoiceStyle(option.Label)),
                MinWidth = 120,
                Margin = new Thickness(0, 0, 7, 7),
                IsEnabled = stage is not null && administrator && !busy && item.IsWritable && item.IsApplicableHere && option.Applicable,
                ToolTip = option.Effect
            };
            var raw = option.Raw;
            button.Click += (_, _) => stage?.Invoke(raw);
            AutomationProperties.SetName(button, $"{option.Label} for {item.ObjectName}");
            choices.Children.Add(button);
        }
        Grid.SetColumn(choices, 1);
        body.Children.Add(choices);

        var legend = new StackPanel
        {
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var option in options)
        {
            legend.Children.Add(new TextBlock
            {
                Text = (option.Raw ?? "Not configured") + " → " + option.Effect,
                FontSize = 10.5,
                Foreground = Brush("BrushTextSecondary"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 3)
            });
        }
        Grid.SetColumn(legend, 2);
        body.Children.Add(legend);
        Grid.SetRow(body, 1);
        root.Children.Add(body);
        Child = root;
        AutomationProperties.SetName(this, $"{item.ObjectName}. {CurrentLabel(item)}.");
    }

    private static List<Choice> Options(ManagedObject item, string windowsVersion, string edition)
    {
        var result = item.ValueSemantics.Where(value => !string.IsNullOrWhiteSpace(value.RawValue))
            .DistinctBy(value => value.RawValue, StringComparer.OrdinalIgnoreCase)
            .Select(value =>
            {
                var copy = SettingOptionLanguage.For(item, value);
                return new Choice(value.RawValue, copy.Action, copy.Effect,
                    ApplicabilityEvaluator.IsValueApplicable(value, windowsVersion, edition));
            }).ToList();
        if (result.Count > 0 && item.WritableTarget is { Kind: WritableTargetKind.Registry, SupportsDeletion: true })
        {
            var clear = SettingOptionLanguage.Clear();
            result.Add(new Choice(null, clear.Action, clear.Effect, true));
        }
        return result;
    }

    private static string Introduction(ManagedObject item)
    {
        var text = string.IsNullOrWhiteSpace(item.Description) ? item.Narrative.Summary : item.Description;
        return string.IsNullOrWhiteSpace(text) ? "Windows setting." : text.Trim();
    }

    private static string CurrentLabel(ManagedObject item)
    {
        var raw = Raw(item.CurrentState);
        var meaning = item.ValueSemantics.FirstOrDefault(value =>
            string.Equals(value.RawValue, raw, StringComparison.OrdinalIgnoreCase));
        return meaning is null ? NavigationBuilder.DisplayValue(item.CurrentState) : SettingOptionLanguage.For(item, meaning).Effect;
    }

    private static string? Raw(string? state)
    {
        if (IsNotConfigured(state)) return null;
        var value = ValueSemanticsInterpreter.Normalize(state);
        return string.IsNullOrWhiteSpace(value) ? null : value.Split(' ', '(', ')', ';')[0];
    }

    private static bool IsNotConfigured(string? state) =>
        string.IsNullOrWhiteSpace(state) || state.StartsWith("Not configured", StringComparison.OrdinalIgnoreCase);

    private static string ChoiceStyle(string label)
    {
        if (label.Contains("block", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("deny", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("stop", StringComparison.OrdinalIgnoreCase)) return "ActionDanger";
        if (label.Contains("allow", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("protect", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("give", StringComparison.OrdinalIgnoreCase)) return "ActionSuccess";
        return "ActionChoice";
    }

    private static Brush Brush(string key) => (Brush)Application.Current.FindResource(key);
    private static Style ResourceStyle(string key) => (Style)Application.Current.FindResource(key);
    private sealed record Choice(string? Raw, string Label, string Effect, bool Applicable);
}
