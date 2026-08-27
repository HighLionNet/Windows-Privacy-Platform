using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class ServicesView : UserControl
{
    private readonly IReadOnlyList<ServiceInfo> _services;

    public ServicesView(ScanService scan)
    {
        InitializeComponent();
        _services = scan.LastScanResult?.Snapshot?.Services?.ToList() ?? new List<ServiceInfo>();
        Render();
    }

    private void FilterChanged(object sender, EventArgs e) => Render();

    private void Render()
    {
        if (ServiceCards is null || SearchBox is null) return;
        var filter = new ServiceFilter(SearchBox.Text, Selected(StateBox), Selected(StartupBox),
            Selected(PublisherBox), Selected(IssueBox));
        var visible = ServiceInspection.Apply(_services, filter);
        ServiceCards.Children.Clear();
        foreach (var service in visible) ServiceCards.Children.Add(Card(service));
        var flagged = visible.Count(service => ServiceInspection.Classify(service) != ServiceEvidenceState.Normal);
        SubtitleText.Text = $"{visible.Count} of {_services.Count} visible · {flagged} need review · collected from read-only SCM and registry evidence";
        if (visible.Count == 0)
            ServiceCards.Children.Add(new TextBlock { Text = "No services match the active filters.", Margin = new Thickness(4, 10, 4, 10), Foreground = (Brush)FindResource("BrushTextMuted") });
    }

    private Border Card(ServiceInfo service)
    {
        var state = ServiceInspection.Classify(service);
        var issue = ServiceInspection.IssueLabel(state);
        var card = new Border
        {
            Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(12),
            Background = (Brush)FindResource("BrushBgCard"),
            BorderBrush = state == ServiceEvidenceState.Normal ? (Brush)FindResource("BrushBorderStrong") : (Brush)FindResource("BrushWarning"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8)
        };
        var root = new StackPanel();
        var heading = new DockPanel();
        var badge = new Border { Style = (Style)FindResource(state == ServiceEvidenceState.Normal ? "BadgeSuccess" : "BadgeWarning"), Margin = new Thickness(8, 0, 0, 0) };
        DockPanel.SetDock(badge, Dock.Right);
        badge.Child = new TextBlock { Text = issue, FontSize = 9, FontWeight = FontWeights.SemiBold };
        heading.Children.Add(badge);
        heading.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(service.DisplayName) ? service.Name : service.DisplayName,
            FontWeight = FontWeights.SemiBold, FontSize = 13, TextWrapping = TextWrapping.Wrap });
        root.Children.Add(heading);
        root.Children.Add(Line(service.Description, 11, "BrushTextSecondary", 3));
        root.Children.Add(Line($"Service name: {service.Name} · State: {Unknown(service.State)} · Startup: {Unknown(service.StartMode)}" +
            (service.DelayedAutoStart ? " (delayed)" : string.Empty), 11, "BrushTextPrimary", 6));
        root.Children.Add(Line($"Publisher: {Unknown(service.Publisher)} · Signature evidence: {Unknown(service.SignatureStatus)} · Account: {Unknown(service.Account)}", 11, "BrushTextSecondary", 3));
        root.Children.Add(Line($"Trigger start: {Unknown(service.TriggerStart)} · User service: {(service.IsUserService ? "Yes" : "No")} · Tags: {(service.Tags.Count == 0 ? "None" : string.Join(", ", service.Tags))}", 10, "BrushTextMuted", 3));
        var expander = new Expander { Header = "Path, dependencies, and evidence details", Style = (Style)FindResource("DetailExpander") };
        expander.Content = Line($"Executable: {Unknown(service.ExecutablePath)}\nDependencies: {Join(service.Dependencies)}\nDependents: {Join(service.Dependents)}\nConfiguration evidence: {Unknown(service.ConfigurationError)}", 10, "BrushTextMuted", 4);
        root.Children.Add(expander);
        card.Child = root;
        AutomationProperties.SetName(card, $"{service.DisplayName}. {service.Name}. {service.State}. {service.StartMode}. {issue}.");
        return card;
    }

    private static string Selected(ComboBox box) => (box.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
    private static string Unknown(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    private static string Join(IReadOnlyCollection<string> values) => values.Count == 0 ? "None observed" : string.Join(", ", values);
    private static TextBlock Line(string? text, double size, string brush, double top) => new()
    {
        Text = Unknown(text), FontSize = size, Foreground = (Brush)Application.Current.FindResource(brush),
        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, top, 0, 0)
    };
}
