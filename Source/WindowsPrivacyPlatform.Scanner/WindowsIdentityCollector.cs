// Source/WindowsPrivacyPlatform.Scanner/WindowsIdentityCollector.cs
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    public sealed class WindowsIdentityCollector : IInventoryCollector
    {
        public string Name => "WindowsIdentityCollector";

        public void Collect(InventorySnapshot snapshot)
        {
            // Exact placeholder values used by Prototype v0.1
            snapshot.WindowsVersion = "Placeholder-WindowsVersion";
            snapshot.Edition = "Placeholder-Edition";
            snapshot.BuildNumber = 0;
        }
    }
}
