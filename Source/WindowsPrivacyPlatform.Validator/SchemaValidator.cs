// Source/WindowsPrivacyPlatform.Validator/SchemaValidator.cs
using System;
using System.Collections.Generic;
using WindowsPrivacyPlatform.KnowledgeBase;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Validator
{
    public sealed class SchemaValidator : IObjectValidator
    {
        private readonly IAuditLogger _logger;
        private readonly IReadOnlyList<IValidationRule> _rules;

        public SchemaValidator(IAuditLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Structural required-field rules only.
            // Additional rule classes can be added here later.
            _rules = new List<IValidationRule>
            {
                new RequiredFieldRule("ObjectId", obj => !string.IsNullOrWhiteSpace(obj.ObjectId)),
                new RequiredFieldRule("ObjectName", obj => !string.IsNullOrWhiteSpace(obj.ObjectName))
            };
        }

        public ValidationResult Validate(ManagedObject managedObject)
        {
            _logger.Info("Validator", "Validation start");
            var result = ValidateCore(managedObject);
            _logger.Info("Validator", "Validation finish");
            return result;
        }

        public ValidationResult Validate(KnowledgeBaseEntry entry)
        {
            _logger.Info("Validator", "Validation start");

            if (entry?.Object is null)
            {
                var fail = new ValidationResult
                {
                    IsValid = false,
                    Message = "KnowledgeBaseEntry or its ManagedObject is null."
                };
                fail.Errors.Add("ManagedObject is required.");
                _logger.Info("Validator", "Validation finish");
                return fail;
            }

            var result = ValidateCore(entry.Object);
            result.ObjectId = entry.ObjectId;
            _logger.Info("Validator", "Validation finish");
            return result;
        }

        private ValidationResult ValidateCore(ManagedObject managedObject)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Message = "Structural validation passed.",
                Errors = new List<string>()
            };

            if (managedObject is null)
            {
                result.IsValid = false;
                result.Message = "ManagedObject is null.";
                result.Errors.Add("ManagedObject is required.");
                return result;
            }

            foreach (var rule in _rules)
            {
                if (!rule.Evaluate(managedObject, out var error))
                {
                    result.IsValid = false;
                    result.Errors.Add(error);
                    result.FailedRules.Add(rule.Name);
                }
            }

            if (!result.IsValid)
            {
                result.Message = "Structural validation failed.";
            }

            return result;
        }
    }
}
