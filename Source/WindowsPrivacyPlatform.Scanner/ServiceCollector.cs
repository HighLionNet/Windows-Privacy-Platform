// Source/WindowsPrivacyPlatform.Scanner/ServiceCollector.cs
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    public sealed class ServiceCollector : IInventoryCollector
    {
        public string Name => "ServiceCollector";

        public void Collect(InventorySnapshot snapshot)
        {
            // Placeholder: leave collection empty (identical to v0.1)
        }
    }
}
