namespace WindowsPrivacyPlatform.Validator;

using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.KnowledgeBase;

public interface IObjectValidator
{
    ValidationResult Validate(ManagedObject managedObject);
    ValidationResult Validate(KnowledgeBaseEntry entry);
}
