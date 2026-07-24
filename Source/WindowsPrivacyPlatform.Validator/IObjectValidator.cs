namespace WindowsPrivacyPlatform.Validator;

using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.KnowledgeBase;

public interface IObjectValidator
{
    ValidationResult Validate(ManagedObject managedObject);
    ValidationResult Validate(KnowledgeBaseEntry entry);

    /// <summary>
    /// Validates every entry. Does not throw on individual failures.
    /// </summary>
    IReadOnlyList<ValidationResult> ValidateAll(IEnumerable<KnowledgeBaseEntry> entries);
}
