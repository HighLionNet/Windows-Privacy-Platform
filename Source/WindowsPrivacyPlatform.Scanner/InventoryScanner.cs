// Source/WindowsPrivacyPlatform.Scanner/InventoryScanner.cs
using System;
using System.Collections.Generic;
using System.Linq;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    public sealed class InventoryScanner : IInventoryScanner
    {
        private readonly IAuditLogger _logger;
        private readonly IReadOnlyList<IInventoryCollector> _collectors;

        public InventoryScanner(IAuditLogger logger, IEnumerable<IInventoryCollector> collectors)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (collectors is null) throw new ArgumentNullException(nameof(collectors));

            _collectors = collectors.ToList().AsReadOnly();
        }

        public ScanResult Scan()
        {
            _logger.Info("Scanner", "Scan start");

            var snapshot = new InventorySnapshot();

            try
            {
                foreach (var collector in _collectors)
                {
                    _logger.Debug("Scanner", $"Collector start: {collector.Name}");
                    collector.Collect(snapshot);
                    _logger.Debug("Scanner", $"Collector finish: {collector.Name}");
                }

                _logger.Info("Scanner", "Scan finish");

                return new ScanResult
                {
                    Success = true,
                    Snapshot = snapshot,
                    Message = "Scan completed successfully (placeholder data)."
                };
            }
            catch (Exception ex)
            {
                _logger.Error("Scanner", $"Scan failed: {ex.Message}");
                return new ScanResult
                {
                    Success = false,
                    Snapshot = snapshot,
                    Message = ex.Message
                };
            }
        }
    }
}
