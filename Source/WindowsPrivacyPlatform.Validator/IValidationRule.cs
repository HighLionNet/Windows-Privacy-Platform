namespace WindowsPrivacyPlatform.Validator;

using WindowsPrivacyPlatform.Models;

public interface IValidationRule
{
    string Name { get; }
    bool Evaluate(ManagedObject managedObject, out string error);
}
