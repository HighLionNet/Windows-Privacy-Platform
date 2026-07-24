// Source/WindowsPrivacyPlatform.Scanner/PackageCollector.cs
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    public sealed class PackageCollector : IInventoryCollector
    {
        public string Name => "PackageCollector";

        public void Collect(InventorySnapshot snapshot)
        {
            // Placeholder: leave collection empty (identical to v0.1)
        }
    }
}
