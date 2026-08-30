using System.Windows;
using System.Windows.Controls;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class ScheduledTasksView : UserControl
{
    private readonly IReadOnlyList<TaskInfo> _all;
    private readonly IReadOnlyList<TaskInfo> _visibleSource;
    private readonly InventoryChangeService _changes;
    private readonly ElevationService _elevation;
    private readonly Func<Task> _refresh;
    private readonly Window? _owner;
    private readonly bool _allowActions;

    public ScheduledTasksView(ScanService scan, InventoryChangeService changes, ElevationService elevation,
        Func<Task> refresh, Window? owner, bool otherTasks)
    {
        _all = scan.LastScanResult?.Snapshot?.ScheduledTasks?.ToList() ?? [];
        _changes = changes;
        _elevation = elevation;
        _refresh = refresh;
        _owner = owner;
        _allowActions = otherTasks;
        _visibleSource = _all.Where(task =>
        {
            var allowed = TaskMutationPolicy.CanMutate(task, _all, out _);
            return otherTasks ? allowed : !allowed;
        }).ToList();
        InitializeComponent();
        TitleText.Text = otherTasks ? "Other tasks" : "Windows tasks";
        SubtitleText.Text = otherTasks
            ? "Non-Microsoft task paths from the current scan. Enable/disable is available only in Administrator mode after one explicit confirmation and live read-back."
            : "Microsoft, protected, or invalid task paths are diagnosis-only. WPP never creates, deletes, or changes a task command.";
        if (_all.Count == 0)
            SubtitleText.Text += " No task rows were observed; this is not proof that no tasks exist.";
        Render();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Render();

    private void Render()
    {
        if (TaskList is null || SearchBox is null) return;
        var term = SearchBox.Text.Trim();
        if (term.Length > 200) term = term[..200];
        TaskList.ItemsSource = _visibleSource
            .Where(task => term.Length == 0 || task.Path.Contains(term, StringComparison.OrdinalIgnoreCase) || task.State.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(task => task.Path, StringComparer.OrdinalIgnoreCase)
            .Select(task => new TaskRow(task, _allowActions && _elevation.IsAdminAuthorized)).ToList();
    }

    private async void TaskAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string path, CommandParameter: string actionText } ||
            !Enum.TryParse<ScheduledTaskAction>(actionText, out var action)) return;
        var task = _visibleSource.FirstOrDefault(item => item.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (task is null) return;
        if (!_changes.TryReadTaskStateForConfirmation(task, out var liveState, out var readError))
        {
            MessageBox.Show(_owner, readError, "Task action refused", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var intended = action == ScheduledTaskAction.Enable ? "enabled" : "disabled";
        var confirmed = MessageBox.Show(_owner,
            $"Task: {task.Path}\nCurrent live state: {liveState}\nIntended state: {intended}\n\nSide effect: scheduled background work may begin or stop.\nRecovery: use the opposite action on this same task after a fresh scan.",
            "Confirm scheduled-task action", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
        if (!confirmed) return;
        var success = _changes.TryChangeTask(task, action, confirmed: true, out var error);
        MessageBox.Show(_owner, success ? "Windows completed and verified the scheduled-task action." : error,
            success ? "Task updated" : "Task action refused", MessageBoxButton.OK,
            success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        if (success) await _refresh();
    }

    private sealed class TaskRow
    {
        public TaskRow(TaskInfo task, bool showControls)
        {
            Path = task.Path;
            var index = task.Path.LastIndexOf('\\');
            Name = index >= 0 && index < task.Path.Length - 1 ? task.Path[(index + 1)..] : task.Path;
            State = string.IsNullOrWhiteSpace(task.State) ? "Unknown" : task.State;
            ControlsVisibility = showControls ? Visibility.Visible : Visibility.Collapsed;
        }
        public string Name { get; }
        public string Path { get; }
        public string State { get; }
        public Visibility ControlsVisibility { get; }
    }
}
