namespace WindowsPrivacyPlatform.Models;

public sealed record PendingPolicyChange(ManagedObject Setting, string? RawValue);
public sealed record PolicyChangeOutcome(string ObjectId, bool Success, string Message);

public sealed record PolicyBatchSummary(int Requested, int Verified, int NotVerified)
{
    public bool AllVerified => Requested > 0 && Requested == Verified && NotVerified == 0;

    public static PolicyBatchSummary From(IReadOnlyCollection<PolicyChangeOutcome> outcomes)
    {
        ArgumentNullException.ThrowIfNull(outcomes);
        var verified = outcomes.Count(outcome => outcome.Success);
        return new PolicyBatchSummary(outcomes.Count, verified, outcomes.Count - verified);
    }
}
