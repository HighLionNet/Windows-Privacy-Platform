// Source/WindowsPrivacyPlatform.Scanner/Binding/BinderHelpers.cs
using System;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner.Binding
{
    internal static class BinderHelpers
    {
        public static string ExtractShortName(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId))
                return string.Empty;

            var idx = objectId.LastIndexOf('.');
            return idx >= 0 && idx < objectId.Length - 1
                ? objectId[(idx + 1)..]
                : objectId;
        }

        public static bool NamesLooselyMatch(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;

            static string Norm(string s) =>
                s.Replace(".", "", StringComparison.Ordinal)
                 .Replace("-", "", StringComparison.Ordinal)
                 .Replace("_", "", StringComparison.Ordinal)
                 .ToLowerInvariant();

            var na = Norm(a);
            var nb = Norm(b);
            return na.Contains(nb, StringComparison.Ordinal) ||
                   nb.Contains(na, StringComparison.Ordinal);
        }

        public static bool IsObserved(string? state) =>
            !string.IsNullOrWhiteSpace(state) &&
            !string.Equals(state, "Not observed in this scan", StringComparison.OrdinalIgnoreCase);

        public static bool IsNotConfigured(string? state) =>
            !string.IsNullOrWhiteSpace(state) &&
            state.Contains("Not configured", StringComparison.OrdinalIgnoreCase);

        public static bool IsError(string? state) =>
            !string.IsNullOrWhiteSpace(state) &&
            state.Contains("Error reading", StringComparison.OrdinalIgnoreCase);

        public static void ApplyObservation(ManagedObject mo, string currentState, ConfigurationObservation? layer)
        {
            mo.CurrentState = currentState;
            mo.LastVerified = DateTime.UtcNow;
            mo.Observation ??= new SettingObservation();
            mo.Observation.CurrentValue = currentState;
            mo.Observation.ObservedAt = mo.LastVerified;
            mo.Observation.ConfidenceScore = mo.ConfidenceScore;

            if (layer is not null)
            {
                mo.Observation.SourceSummary = $"{layer.Layer}: {layer.SourcePath}";
                mo.Observation.Layers.Add(layer);
            }
        }
    }
}
