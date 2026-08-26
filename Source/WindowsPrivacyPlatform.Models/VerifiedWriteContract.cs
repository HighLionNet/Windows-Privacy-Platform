namespace WindowsPrivacyPlatform.Models;

public sealed record ManagedWriteState(bool Readable, string Value, string Detail = "")
{
    public static ManagedWriteState Unreadable(string detail) => new(false, string.Empty, detail);
}

public interface IManagedWriteBackend
{
    ManagedWriteState Read(WritableTarget target);
    bool Write(WritableTarget target, string requestedValue, out string error);
}

public sealed record VerifiedWriteResult(
    bool Success,
    ManagedWriteState Before,
    ManagedWriteState After,
    string Message);

/// <summary>Pure pre-read, typed write, independent read-back contract used by native-surface tests.</summary>
public static class VerifiedWriteContract
{
    public static VerifiedWriteResult Execute(
        WritableTarget target,
        string requestedValue,
        IManagedWriteBackend backend)
    {
        if (target is null || !target.IsComplete)
            return Failure("Write target is incomplete.");
        if (backend is null)
            return Failure("Write backend is unavailable.");
        if (!target.SupportedRawValues.Contains(requestedValue, StringComparer.OrdinalIgnoreCase))
            return Failure("Requested value is outside the explicit allowlist.");

        var before = backend.Read(target);
        if (!before.Readable)
            return new VerifiedWriteResult(false, before, before, "Pre-read failed; no write was attempted.");

        if (Matches(target.Kind, before.Value, requestedValue))
            return new VerifiedWriteResult(true, before, before, "The requested state was already verified.");

        if (!backend.Write(target, requestedValue, out var error))
            return new VerifiedWriteResult(false, before, before, string.IsNullOrWhiteSpace(error) ? "Write failed." : error);

        var after = backend.Read(target);
        if (!after.Readable)
            return new VerifiedWriteResult(false, before, after, "Write completed but independent read-back failed.");
        if (!Matches(target.Kind, after.Value, requestedValue))
            return new VerifiedWriteResult(false, before, after, "Read-back does not match the requested state.");

        return new VerifiedWriteResult(true, before, after, "Change independently verified.");
    }

    public static bool Matches(WritableTargetKind kind, string observed, string requested)
    {
        if (string.IsNullOrWhiteSpace(observed) || string.IsNullOrWhiteSpace(requested))
            return false;

        return kind switch
        {
            WritableTargetKind.Service => observed.Split(';', StringSplitOptions.TrimEntries)
                .Any(part => part.Equals(requested, StringComparison.OrdinalIgnoreCase)),
            WritableTargetKind.AppxPackage when requested.Equals("Remove", StringComparison.OrdinalIgnoreCase) =>
                observed.Equals("Removed", StringComparison.OrdinalIgnoreCase) ||
                observed.Equals("Not installed", StringComparison.OrdinalIgnoreCase),
            _ => observed.Equals(requested, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static VerifiedWriteResult Failure(string message)
    {
        var state = ManagedWriteState.Unreadable(message);
        return new VerifiedWriteResult(false, state, state, message);
    }
}
