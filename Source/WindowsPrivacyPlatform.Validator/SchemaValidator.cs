// Source/WindowsPrivacyPlatform.Validator/SchemaValidator.cs
using System;
using System.Collections.Generic;
using System.Linq;
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
                new RequiredFieldRule("SchemaVersion", obj => !string.IsNullOrWhiteSpace(obj.SchemaVersion)),
                // ProductDomain is an enum; zero is ConsentStore which is valid, so we only check ObjectId uniqueness at batch level.
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

            var list = entries.Where(e => e is not null).ToList();
            var passed = 0;
            var failed = 0;

            // Duplicate ObjectId detection across the batch (catalog quality guard).
            var idGroups = list
                .Where(e => e.Object is not null && !string.IsNullOrWhiteSpace(e.Object.ObjectId))
                .GroupBy(e => e.Object!.ObjectId, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            foreach (var entry in list)
            {
                var result = Validate(entry);

                if (entry.Object is not null &&
                    !string.IsNullOrWhiteSpace(entry.Object.ObjectId) &&
                    idGroups.ContainsKey(entry.Object.ObjectId))
                {
                    result.IsValid = false;
                    result.Message = "Catalog quality validation failed.";
                    result.Errors.Add($"Duplicate ObjectId '{entry.Object.ObjectId}' detected in catalog batch.");
                    result.FailedRules.Add("UniqueObjectId");
                }

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
