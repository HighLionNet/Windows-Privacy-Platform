namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// One known raw-value interpretation for a catalog setting.
/// Owned by knowledge/catalog — collectors and resolvers must not invent meanings.
/// </summary>
public sealed class ValueMeaning
{
    /// <summary>Exact or normalized raw token as observed (e.g. "0", "Allow", "1").</summary>
    public string RawValue { get; set; } = string.Empty;

    /// <summary>Stable canonical token for reasoning (e.g. "Security", "ForceDeny", "Enabled").</summary>
    public string Canonical { get; set; } = string.Empty;

    /// <summary>Human-facing short label.</summary>
    public string DisplayLabel { get; set; } = string.Empty;

    /// <summary>Neutral technical description of what this value means in Windows.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Editions where this value is meaningful (empty = general).</summary>
    public List<string> SupportedEditions { get; set; } = new();

    /// <summary>Windows versions/builds where this mapping applies (empty = general).</summary>
    public List<string> SupportedVersions { get; set; } = new();

    public EffectiveConfidence Confidence { get; set; } = EffectiveConfidence.High;

    /// <summary>Notes, deprecation, or edition caveats. Never used as a recommendation.</summary>
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Pure interpretation of a raw observed token using a catalog entry's semantic map.
/// No system access. Unknown is returned when the map has no match — never guess.
/// </summary>
public static class ValueSemanticsInterpreter
{
    public static ValueMeaning? Interpret(ManagedObject? definition, string? rawValue)
    {
        if (definition?.ValueSemantics is null || definition.ValueSemantics.Count == 0)
            return null;

        var token = Normalize(rawValue);
        if (string.IsNullOrEmpty(token))
            return null;

        foreach (var m in definition.ValueSemantics)
        {
            if (m is null || string.IsNullOrWhiteSpace(m.RawValue))
                continue;

            if (string.Equals(Normalize(m.RawValue), token, StringComparison.OrdinalIgnoreCase))
                return m;
        }

        return null;
    }

    public static ValueMeaning Unknown(string? rawValue) => new()
    {
        RawValue = rawValue ?? string.Empty,
        Canonical = "Unknown",
        DisplayLabel = "Unknown",
        Description = "No semantic mapping is defined for this raw value in the knowledge catalog.",
        Confidence = EffectiveConfidence.Unknown,
        Notes = "Do not invent meaning. Treat as Unknown until a catalog map is added."
    };

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var s = raw.Trim();
        // Strip trailing " (HKLM)" style display suffixes used by binders.
        var idx = s.IndexOf(" (", StringComparison.Ordinal);
        if (idx > 0)
            s = s[..idx].Trim();

        return s;
    }

    /// <summary>
    /// Prefer mapped DisplayLabel / Canonical; otherwise return the normalized raw token unchanged.
    /// </summary>
    public static string Display(ManagedObject? definition, string? rawValue)
    {
        var meaning = Interpret(definition, rawValue);
        if (meaning is null)
            return Normalize(rawValue);

        return string.IsNullOrWhiteSpace(meaning.DisplayLabel)
            ? meaning.Canonical
            : meaning.DisplayLabel;
    }

    public static string CanonicalOrRaw(ManagedObject? definition, string? rawValue)
    {
        var meaning = Interpret(definition, rawValue);
        return meaning is null ? Normalize(rawValue) : meaning.Canonical;
    }
}
