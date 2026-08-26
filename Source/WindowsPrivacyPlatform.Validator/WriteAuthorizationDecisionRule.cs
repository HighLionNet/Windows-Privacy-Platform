using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Validator;

public sealed class WriteAuthorizationDecisionRule : IValidationRule
{
    public string Name => "ExplicitWriteAuthorizationDecision";

    public bool Evaluate(ManagedObject obj, out string error)
    {
        if (obj.IsWritable && obj.ExclusionReason != ExclusionReason.None)
        {
            error = "Writable entries must use ExclusionReason.None.";
            return false;
        }

        if (!obj.IsWritable && obj.ExclusionReason == ExclusionReason.None)
        {
            error = "Every view-only entry requires an explicit ExclusionReason.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
