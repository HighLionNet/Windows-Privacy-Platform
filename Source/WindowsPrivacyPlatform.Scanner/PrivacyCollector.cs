// Source/WindowsPrivacyPlatform.Scanner/PrivacyCollector.cs
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    public sealed class PrivacyCollector : IInventoryCollector
    {
        public string Name => "PrivacyCollector";

        public void Collect(InventorySnapshot snapshot)
        {
            // Placeholder: leave collection empty (identical to v0.1)
        }
    }
}
