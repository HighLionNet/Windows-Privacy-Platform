using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class DnsResolutionView : UserControl
{
    private readonly ScanService _scan;
    private readonly Action<SettingsListTarget> _openSettings;

    public DnsResolutionView(ScanService scan, Action<SettingsListTarget> openSettings, bool adaptersOnly = false)
    {
        _scan = scan;
        _openSettings = openSettings;
        InitializeComponent();
        TitleText.Text = adaptersOnly ? "Adapters & LAN" : "DNS & name resolution";
        SubtitleText.Text = adaptersOnly
            ? "Observed active adapters, address families, and DNS sources. Empty resolver evidence is Unknown, never ‘DNS disabled’."
            : "Effective DNS is layered: Windows route, VPN participation, NRPT overrides, then browser or app resolvers.";
        var dns = scan.LastScanResult?.Snapshot?.Networking.Dns ?? new DnsResolutionSnapshot();
        Render(dns, adaptersOnly);
    }

    private void Render(DnsResolutionSnapshot dns, bool adaptersOnly)
    {
        if (adaptersOnly)
        {
            AnswerPanel.Visibility = Visibility.Collapsed;
            PolicyPanel.Visibility = Visibility.Collapsed;
            ProbeHeading.Visibility = Visibility.Collapsed;
            ProbePanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            AnswerPanel.Children.Add(Layer("1 · Where Windows will send a system query", dns.PreferredPath));
            AnswerPanel.Children.Add(Layer("2 · Whether VPN DNS is in that path", dns.VpnDnsPath));
            AnswerPanel.Children.Add(Layer("3 · Whether NRPT overrides some names", dns.Nrpt));
            foreach (var rule in dns.NrptRules)
                AnswerPanel.Children.Add(TextCard(rule.Namespace, $"Servers: {rule.NameServers} · DNSSEC: {rule.DnsSec} · DirectAccess: {rule.DirectAccess}", rule.Source));
            AnswerPanel.Children.Add(Layer("Windows encrypted DNS", dns.WindowsDoh));
            var appSummary = dns.ExternalApps.Count == 0
                ? "Browser/app DNS was not observed."
                : string.Join("\n", dns.ExternalApps.Select(app =>
                    $"{app.Application} · {EvidenceStateSemantics.Label(app.Evidence)}: {app.Summary}"));
            var appEvidence = dns.ExternalApps.Select(app => app.Evidence).Distinct().ToList();
            var appEvidenceLabel = appEvidence.Count switch
            {
                0 => "Unknown",
                1 => EvidenceStateSemantics.Label(appEvidence[0]),
                _ => "Mixed evidence"
            };
            AnswerPanel.Children.Add(TextCard($"4 · Whether a browser may be doing something else · {appEvidenceLabel}", appSummary, "ExternalApp boundary"));
        }

        foreach (var adapter in dns.Interfaces.OrderByDescending(item => item.Status == "Up").ThenBy(item => item.Name))
        {
            var dnsServers = adapter.DnsServers.Count == 0 ? "Unknown (no resolver addresses returned)" : string.Join(", ", adapter.DnsServers);
            var addresses = string.Join(" · ", new[]
            {
                adapter.IPv4Addresses.Count == 0 ? null : "IPv4 " + string.Join(", ", adapter.IPv4Addresses),
                adapter.IPv6Addresses.Count == 0 ? null : "IPv6 " + string.Join(", ", adapter.IPv6Addresses)
            }.Where(value => value is not null));
            var description = string.IsNullOrWhiteSpace(adapter.Description) || adapter.Description.Equals(adapter.Name, StringComparison.OrdinalIgnoreCase)
                ? string.Empty : adapter.Description + "\n";
            InterfacePanel.Children.Add(TextCard(adapter.Name + (adapter.IsVpnOrTunnel ? " · VPN/tunnel" : string.Empty) +
                                                  " · " + EvidenceStateSemantics.Label(adapter.Evidence),
                $"{description}{adapter.Type} · {adapter.Status} · index {adapter.InterfaceIndex} · metric {(adapter.InterfaceMetric?.ToString() ?? "Unknown")}\n{addresses}\nDNS ({adapter.AddressSource}): {dnsServers}",
                "System.Net.NetworkInformation + TCP/IP interface registry"));
        }
        if (dns.Interfaces.Count == 0)
            InterfacePanel.Children.Add(TextCard("No adapter evidence", "No active adapter rows were returned; this does not prove that networking or DNS is disabled.", "System.Net.NetworkInformation"));

        foreach (var probe in dns.ResolverProbes)
            ProbePanel.Children.Add(TextCard(probe.Resolver + " · " + EvidenceStateSemantics.Label(probe.Evidence),
                probe.QueryName + " → " + probe.Answer, probe.Source));
        if (!adaptersOnly && dns.ResolverProbes.Count == 0)
            ProbePanel.Children.Add(TextCard("No resolver probe", "No observed resolver address was available to probe. DNS state remains Unknown.", "Observed resolver list"));
    }

    private Border Layer(string title, DnsLayerObservation layer) =>
        TextCard(title + " · " + EvidenceStateSemantics.Label(layer.Evidence), layer.Summary, layer.Source,
            layer.Evidence is EvidenceState.Error or EvidenceState.AccessDenied ? "BrushWarning" : "BrushBorderStrong");

    private Border TextCard(string title, string detail, string source, string border = "BrushBorderStrong")
    {
        var card = new Border
        {
            Style = (Style)FindResource("Card"),
            Margin = new Thickness(0, 0, 0, 9),
            BorderBrush = (Brush)FindResource(border),
            BorderThickness = new Thickness(3, 1, 1, 1)
        };
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = detail, Margin = new Thickness(0, 4, 0, 0), Foreground = (Brush)FindResource("BrushTextSecondary"), TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = "Source: " + source, Margin = new Thickness(0, 5, 0, 0), FontSize = 10, Foreground = (Brush)FindResource("BrushTextMuted"), TextWrapping = TextWrapping.Wrap });
        card.Child = panel;
        return card;
    }

    private void Setting_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string id }) return;
        var setting = _scan.SettingsCatalog.FirstOrDefault(item => item.ObjectId.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (setting is not null) _openSettings(SettingsListTarget.For(setting));
    }

    private void NetworkSettings_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("ms-settings:network") { UseShellExecute = true }); }
        catch { }
    }
}
