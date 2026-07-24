namespace WindowsPrivacyPlatform.Models;

public class ScanResult
{
    public InventorySnapshot Snapshot { get; set; } = new();
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
}
