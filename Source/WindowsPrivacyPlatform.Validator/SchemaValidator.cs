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

            // Structural + catalog-quality rules. Still read-only / no system interaction.
            _rules = new List<IValidationRule>
            {
                new RequiredFieldRule("ObjectId", obj => !string.IsNullOrWhiteSpace(obj.ObjectId)),
                new RequiredFieldRule("ObjectName", obj => !string.IsNullOrWhiteSpace(obj.ObjectName)),
                new RequiredFieldRule("Description", obj => !string.IsNullOrWhiteSpace(obj.Description)),
                new RequiredFieldRule("ObjectType", obj => !string.IsNullOrWhiteSpace(obj.ObjectType)),
                new RequiredFieldRule("SchemaVersion", obj => !string.IsNullOrWhiteSpace(obj.SchemaVersion))
            };
        }

        public ValidationResult Validate(ManagedObject managedObject)
        {
            return ValidateCore(managedObject, log: true);
        }

        public ValidationResult Validate(KnowledgeBaseEntry entry)
        {
            if (entry?.Object is null)
            {
                var fail = new ValidationResult
                {
                    IsValid = false,
                    Message = "KnowledgeBaseEntry or its ManagedObject is null."
                };
                fail.Errors.Add("ManagedObject is required.");
                return fail;
            }

            var result = ValidateCore(entry.Object, log: false);
            result.ObjectId = entry.ObjectId ?? entry.Object.ObjectId;
            return result;
        }

        public IReadOnlyList<ValidationResult> ValidateAll(IEnumerable<KnowledgeBaseEntry> entries)
        {
            _logger.Info("Validator", "Batch validation start");

            var results = new List<ValidationResult>();
            if (entries is null)
            {
                _logger.Info("Validator", "Batch validation finish (null input)");
                return results;
            }

            var passed = 0;
            var failed = 0;

            foreach (var entry in entries)
            {
                var result = Validate(entry);
                results.Add(result);
                if (result.IsValid)
                    passed++;
                else
                    failed++;
            }

            _logger.Info("Validator", $"Batch validation finish: passed={passed}, failed={failed}");
            return results;
        }

        private ValidationResult ValidateCore(ManagedObject managedObject, bool log)
        {
            if (log)
                _logger.Info("Validator", "Validation start");

            var result = new ValidationResult
            {
                IsValid = true,
                Message = "Structural validation passed.",
                Errors = new List<string>(),
                FailedRules = new List<string>(),
                Timestamp = DateTime.UtcNow
            };

            if (managedObject is null)
            {
                result.IsValid = false;
                result.Message = "ManagedObject is null.";
                result.Errors.Add("ManagedObject is required.");
                if (log)
                    _logger.Info("Validator", "Validation finish");
                return result;
            }

            result.ObjectId = managedObject.ObjectId;

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
                result.Message = "Structural validation failed.";

            if (log)
                _logger.Info("Validator", "Validation finish");

            return result;
        }
    }
}
