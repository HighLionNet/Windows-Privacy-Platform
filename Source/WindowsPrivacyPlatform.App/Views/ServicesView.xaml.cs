using System.Windows.Controls;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class ServicesView : UserControl
{
    private readonly IReadOnlyList<ServiceInfo> _services;

    public ServicesView(ScanService scan)
    {
        InitializeComponent();
        _services = scan.LastScanResult?.Snapshot?.Services?.ToList() ?? [];
        Render();
    }

    private void FilterChanged(object sender, EventArgs e) => Render();

    private void Render()
    {
        if (ServiceList is null || SearchBox is null) return;
        var filter = new ServiceFilter(SearchBox.Text, Selected(StateBox), Selected(StartupBox), Selected(PublisherBox), Selected(IssueBox));
        var visible = ServiceInspection.Apply(_services, filter);
        ServiceList.ItemsSource = visible.Select(service => new ServiceRow(service)).ToList();
        var flagged = visible.Count(service => ServiceInspection.Classify(service) != ServiceEvidenceState.Normal);
        SubtitleText.Text = $"Showing {visible.Count:N0} of {_services.Count:N0} services. {flagged:N0} need review.";
    }

    private static string Selected(ComboBox box) => (box.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
    private static string Show(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    private static string Join(IReadOnlyCollection<string> values) => values.Count == 0 ? "None observed" : string.Join(", ", values);

    private sealed class ServiceRow
    {
        public ServiceRow(ServiceInfo service)
        {
            var classification = ServiceInspection.Classify(service);
            DisplayName = string.IsNullOrWhiteSpace(service.DisplayName) ? service.Name : service.DisplayName;
            Name = service.Name; Description = Show(service.Description); State = Show(service.State);
            Startup = Show(service.StartMode) + (service.DelayedAutoStart ? " (delayed)" : string.Empty);
            Issue = ServiceInspection.IssueLabel(classification); HasIssue = classification != ServiceEvidenceState.Normal;
            Account = Show(service.Account); Publisher = Show(service.Publisher); Signature = Show(service.SignatureStatus);
            Executable = Show(service.ExecutablePath);
            Relationships = $"Dependencies: {Join(service.Dependencies)} · Dependents: {Join(service.Dependents)}";
        }
        public string DisplayName { get; } public string Name { get; } public string Description { get; }
        public string State { get; } public string Startup { get; } public string Issue { get; } public bool HasIssue { get; }
        public string Account { get; } public string Publisher { get; } public string Signature { get; }
        public string Executable { get; } public string Relationships { get; }
    }
}
