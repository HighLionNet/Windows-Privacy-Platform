using System.Security.Cryptography;
using System.IO;
using WindowsPrivacyPlatform.Core;
using WindowsPrivacyPlatform.Logging;

namespace WindowsPrivacyPlatform.App.Services;

public static class BinaryIntegrityGuard
{
    private static readonly object Sync = new();
    public static bool HighImpactAllowed { get; private set; }
    public static string CurrentHash { get; private set; } = "Unavailable";
    public static string Status { get; private set; } = "Not checked";

    public static void Initialize(string dataRoot, IAuditLogger log)
    {
        lock (Sync)
        {
            try
            {
                var executable = Environment.ProcessPath ?? string.Empty;
                using var stream = File.OpenRead(executable);
                CurrentHash = Convert.ToHexString(SHA256.HashData(stream));
                var record = Path.Combine(dataRoot, "verified-binary.sha256");
                var previous = AtomicLocalFile.ReadAllLines(dataRoot, record).FirstOrDefault()?.Trim();
                if (string.IsNullOrWhiteSpace(previous))
                {
                    AtomicLocalFile.WriteText(dataRoot, record, CurrentHash + Environment.NewLine);
                    HighImpactAllowed = true;
                    Status = "Current binary recorded at startup";
                }
                else
                {
                    HighImpactAllowed = CryptographicOperations.FixedTimeEquals(
                        Convert.FromHexString(previous), Convert.FromHexString(CurrentHash));
                    Status = HighImpactAllowed ? "Matches last verified startup hash" : "Hash changed — high-impact Apply blocked";
                }
                log.Auth("BinaryIntegrity", "result=" + (HighImpactAllowed ? "Verified" : "Mismatch"));
            }
            catch (Exception ex)
            {
                HighImpactAllowed = false;
                Status = "Unable to verify — high-impact Apply blocked";
                log.Auth("BinaryIntegrity", "result=Error category=" + ex.GetType().Name);
            }
        }
    }

    public static bool AcceptCurrent(string dataRoot, IAuditLogger log)
    {
        lock (Sync)
        {
            try
            {
                if (CurrentHash.Length != 64 || !CurrentHash.All(Uri.IsHexDigit)) return false;
                var record = Path.Combine(dataRoot, "verified-binary.sha256");
                AtomicLocalFile.WriteText(dataRoot, record, CurrentHash + Environment.NewLine);
                HighImpactAllowed = true;
                Status = "Current binary verified by the user";
                log.Auth("BinaryIntegrity", "result=AcceptedCurrentBinary");
                return true;
            }
            catch (Exception ex)
            {
                log.Auth("BinaryIntegrity", "result=Error category=" + ex.GetType().Name);
                return false;
            }
        }
    }
}
