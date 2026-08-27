using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Validator;

/// <summary>Decision-copy invariants for entries that appear in the editable Settings surface.</summary>
public sealed class SettingContentRule : IValidationRule
{
    public string Name => "EditableSettingContent";

    public bool Evaluate(ManagedObject item, out string error)
    {
        if (item.Bucket != CatalogBucket.Settings)
        {
            error = string.Empty;
            return true;
        }

        if (!item.IsWritable || item.WritableTarget?.Kind != WritableTargetKind.Registry)
            return Fail("Settings entries require a complete, typed registry target.", out error);
        if (Normalize(item.ObjectName) == Normalize(item.Description) ||
            Normalize(item.ObjectName) == Normalize(item.Narrative.Summary))
            return Fail("The setting title must not be repeated as its explanation.", out error);
        if (item.ValueSemantics is not { Count: > 0 })
            return Fail("Editable settings require supported options with explanations.", out error);
        if (item.ValueSemantics.Any(v => v is null || string.IsNullOrWhiteSpace(v.RawValue) ||
                                         string.IsNullOrWhiteSpace(v.DisplayLabel) ||
                                         string.IsNullOrWhiteSpace(v.Description)))
            return Fail("Every editable option requires a raw value, label, and practical explanation.", out error);

        foreach (var value in item.ValueSemantics)
        {
            var copy = SettingOptionLanguage.For(item, value);
            if (string.IsNullOrWhiteSpace(copy.Action) || string.IsNullOrWhiteSpace(copy.Effect) ||
                Normalize(copy.Action) == Normalize(copy.Effect))
                return Fail("Every editable option requires distinct action and effect copy.", out error);
        }

        if (item.MinimumBuild <= 0 && item.SupportedWindowsVersions is not { Count: > 0 })
            return Fail("Editable settings require build or Windows-version applicability metadata.", out error);

        error = string.Empty;
        return true;
    }

    private static string Normalize(string? value) => new((value ?? string.Empty)
        .Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }
}
