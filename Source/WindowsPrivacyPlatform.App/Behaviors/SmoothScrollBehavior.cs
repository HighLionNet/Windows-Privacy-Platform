using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WindowsPrivacyPlatform.App.Behaviors;

/// <summary>
/// Converts wheel/touchpad deltas into damped pixel offsets instead of WPF's fixed line jumps.
/// Small precision-touchpad deltas remain small; a conventional 120-unit wheel notch moves 42 px.
/// </summary>
public static class SmoothScrollBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(SmoothScrollBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer viewer)
            return;

        viewer.PreviewMouseWheel -= OnPreviewMouseWheel;
        if (e.NewValue is true)
            viewer.PreviewMouseWheel += OnPreviewMouseWheel;
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer viewer || e.Delta == 0)
            return;

        const double damping = 0.35;
        var next = Math.Clamp(
            viewer.VerticalOffset - (e.Delta * damping),
            0,
            viewer.ScrollableHeight);

        viewer.ScrollToVerticalOffset(next);
        e.Handled = true;
    }
}
