using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class ServicesView : UserControl
{
    private readonly IReadOnlyList<ServiceInfo> _services;
    private readonly ElevationService _elevation;
    private readonly Func<Task> _refresh;
    private readonly Window? _owner;
    private readonly ServiceControlService _control = new();
    private readonly ServiceFilterState _filterState;

    public ServicesView(ScanService scan, ElevationService elevation, Func<Task> refresh, Window? owner,
        ServiceFilterState filterState)
    {
        _elevation = elevation;
        _refresh = refresh;
        _owner = owner;
        _filterState = filterState;
        _services = scan.LastScanResult?.Snapshot?.Services?.ToList() ?? [];
        InitializeComponent();
        SearchBox.Text = filterState.Search;
        Select(StateBox, filterState.State); Select(StartupBox, filterState.Startup);
        Select(PublisherBox, filterState.Publisher); Select(IssueBox, filterState.Issue);
        Render();
    }

    private async void ServiceAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string name, CommandParameter: string actionText } ||
            !Enum.TryParse<ServiceControlAction>(actionText, out var action)) return;
        var service = _services.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (service is null) return;
        var confirmed = MessageBox.Show(_owner,
            $"{action} {service.DisplayName}? Windows may interrupt applications that depend on this optional service.",
            "Confirm service action", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
        var success = _control.TryChange(service, action, _elevation.IsAdminAuthorized, confirmed, out var error);
        MessageBox.Show(_owner, success ? "Windows completed the service action." : error,
            success ? "Service updated" : "Service action refused", MessageBoxButton.OK,
            success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        if (success) await _refresh();
    }

    private void FilterChanged(object sender, EventArgs e) => Render();

    private void Render()
    {
        if (ServiceList is null || SearchBox is null || StateBox is null || StartupBox is null ||
            PublisherBox is null || IssueBox is null) return;
        _filterState.Search = SearchBox.Text.Trim(); _filterState.State = Selected(StateBox);
        _filterState.Startup = Selected(StartupBox); _filterState.Publisher = Selected(PublisherBox);
        _filterState.Issue = Selected(IssueBox);
        var filter = new ServiceFilter(_filterState.Search, _filterState.State, _filterState.Startup,
            _filterState.Publisher, _filterState.Issue);
        var visible = ServiceInspection.Apply(_services, filter);
        SearchBox.Background = Active(_filterState.Search.Length > 0);
        StateBox.Background = Active(_filterState.State != "All"); StartupBox.Background = Active(_filterState.Startup != "All");
        PublisherBox.Background = Active(_filterState.Publisher != "All"); IssueBox.Background = Active(_filterState.Issue != "All");
        ServiceList.ItemsSource = visible.Select(service => new ServiceRow(service, _elevation.IsAdminAuthorized)).ToList();
        var flagged = visible.Count(service => ServiceInspection.Classify(service) != ServiceEvidenceState.Normal);
        SubtitleText.Text = $"Showing {visible.Count:N0} of {_services.Count:N0} services. {flagged:N0} need review.";
    }

    private static string Selected(ComboBox box) => (box.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
    private Brush Active(bool active) => (Brush)FindResource(active ? "BrushAccentSoft" : "BrushBgCard");
    private static void Select(ComboBox box, string value) => box.SelectedItem = box.Items.OfType<ComboBoxItem>()
        .FirstOrDefault(item => string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase)) ?? box.Items[0];
    private static string Show(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    private static string Join(IReadOnlyCollection<string> values) => values.Count == 0 ? "None observed" : string.Join(", ", values);

    private sealed class ServiceRow
    {
        public ServiceRow(ServiceInfo service, bool administratorAuthorized)
        {
            var classification = ServiceInspection.Classify(service);
            DisplayName = string.IsNullOrWhiteSpace(service.DisplayName) ? service.Name : service.DisplayName;
            Name = service.Name; Description = Show(service.Description); State = Show(service.State);
            Startup = Show(service.StartMode) + (service.DelayedAutoStart ? " (delayed)" : string.Empty);
            Issue = ServiceInspection.IssueLabel(classification); HasIssue = classification != ServiceEvidenceState.Normal;
            Account = Show(service.Account); Publisher = Show(service.Publisher); Signature = Show(service.SignatureStatus);
            Executable = Show(service.ExecutablePath); CommandLine = Show(service.CommandLine); FileVersion = Show(service.FileVersion);
            FileExistence = string.IsNullOrWhiteSpace(service.ExecutablePath) ? "Unknown" :
                service.MissingExecutable ? "Missing or inaccessible" : "Present";
            Dependencies = Join(service.DependencyStates.Count > 0 ? service.DependencyStates : service.Dependencies);
            Dependents = Join(service.DependentStates.Count > 0 ? service.DependentStates : service.Dependents);
            Events = service.RecentEvents.Count == 0 ? "No matching recent events in the bounded query." : string.Join("\n", service.RecentEvents);
            Suggestion = ServiceInspection.Diagnosis(service);
            ControlsVisibility = administratorAuthorized && ServiceMutationPolicy.CanMutate(service, out _)
                ? Visibility.Visible : Visibility.Collapsed;
        }
        public string DisplayName { get; } public string Name { get; } public string Description { get; }
        public string State { get; } public string Startup { get; } public string Issue { get; } public bool HasIssue { get; }
        public string Account { get; } public string Publisher { get; } public string Signature { get; }
        public string Executable { get; } public string CommandLine { get; } public string FileVersion { get; }
        public string FileExistence { get; }
        public string Dependencies { get; } public string Dependents { get; } public string Events { get; }
        public string Suggestion { get; } public Visibility ControlsVisibility { get; }
    }
}
