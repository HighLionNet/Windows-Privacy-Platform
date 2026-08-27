// Source/WindowsPrivacyPlatform.Scanner/IInventoryCollector.cs
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    public interface IInventoryCollector
    {
        string Name { get; }
        void Collect(InventorySnapshot snapshot, CancellationToken cancellationToken = default);
    }
}
