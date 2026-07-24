// Source/WindowsPrivacyPlatform.Scanner/ScheduledTaskCollector.cs
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    public sealed class ScheduledTaskCollector : IInventoryCollector
    {
        public string Name => "ScheduledTaskCollector";

        public void Collect(InventorySnapshot snapshot)
        {
            // Placeholder: leave collection empty (identical to v0.1)
        }
    }
}
