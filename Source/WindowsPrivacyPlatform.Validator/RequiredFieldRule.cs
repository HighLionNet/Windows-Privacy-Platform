// Source/WindowsPrivacyPlatform.Validator/RequiredFieldRule.cs
using System;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Validator
{
    public sealed class RequiredFieldRule : IValidationRule
    {
        private readonly string _fieldName;
        private readonly Func<ManagedObject, bool> _predicate;

        public RequiredFieldRule(string fieldName, Func<ManagedObject, bool> predicate)
        {
            _fieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public string Name => $"Required_{_fieldName}";

        public bool Evaluate(ManagedObject obj, out string error)
        {
            if (obj is null)
            {
                error = "ManagedObject is null.";
                return false;
            }

            if (_predicate(obj))
            {
                error = string.Empty;
                return true;
            }

            error = $"Required field '{_fieldName}' is missing or empty.";
            return false;
        }
    }
}
