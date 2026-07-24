// Source/WindowsPrivacyPlatform.Scanner/CapabilityCollector.cs
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    public sealed class CapabilityCollector : IInventoryCollector
    {
        public string Name => "CapabilityCollector";

        public void Collect(InventorySnapshot snapshot)
        {
            // Placeholder: leave collection empty (identical to v0.1)
        }
    }
}
