using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Validator;

public sealed class NarrativeContentRule : IValidationRule
{
    public string Name => "PlainLanguageNarrative";

    public bool Evaluate(ManagedObject obj, out string error)
    {
        if (obj.Narrative is null)
        {
            error = "Structured narrative is required.";
            return false;
        }

        return obj.Narrative.IsComplete(obj, out error);
    }
}
