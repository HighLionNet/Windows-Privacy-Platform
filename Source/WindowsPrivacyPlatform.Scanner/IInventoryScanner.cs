// Source/WindowsPrivacyPlatform.Scanner/IInventoryScanner.cs
using System.Threading;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner;

public interface IInventoryScanner
{
    ScanResult Scan();
    ScanResult Scan(CancellationToken cancellationToken);
}
