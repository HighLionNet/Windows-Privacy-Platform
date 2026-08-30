using System.Security.Cryptography;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using WindowsPrivacyPlatform.Core;
using WindowsPrivacyPlatform.Logging;

namespace WindowsPrivacyPlatform.App.Services;

public static class BinaryIntegrityGuard
{
    private const int TrustENoSignature = unchecked((int)0x800B0100);
    private static readonly object Sync = new();
    public static bool HighImpactAllowed { get; private set; }
    public static string CurrentHash { get; private set; } = "Unavailable";
    public static string SignatureStatus { get; private set; } = "Unavailable";
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
                var hashMatches = IsHashMatch(previous, CurrentHash);
                var signature = InspectSignature(executable);
                SignatureStatus = DescribeSignature(signature);
                HighImpactAllowed = EvaluatePolicy(signature.IsSigned, signature.IsValid, signature.PublisherMatches, hashMatches);
                Status = signature.IsSigned
                    ? signature.IsValid && signature.PublisherMatches
                        ? "Authenticode signature valid · publisher HighLionNet"
                        : "Signed build failed signature or publisher validation — high-impact Apply blocked"
                    : string.IsNullOrWhiteSpace(previous)
                        ? "Unsigned community build · no previous hash recorded"
                        : hashMatches
                            ? "Unsigned community build · hash unchanged"
                            : "Unsigned community build · hash changed (status only)";
                log.Auth("BinaryIntegrity", $"result={(HighImpactAllowed ? "Allowed" : "Blocked")} signed={signature.IsSigned} signatureValid={signature.IsValid} publisherMatch={signature.PublisherMatches} hashMatch={hashMatches}");
            }
            catch (Exception ex)
            {
                HighImpactAllowed = false;
                SignatureStatus = "Unavailable";
                Status = "Executable integrity could not be inspected — high-impact Apply blocked";
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
                var signature = InspectSignature(Environment.ProcessPath ?? string.Empty);
                SignatureStatus = DescribeSignature(signature);
                HighImpactAllowed = EvaluatePolicy(signature.IsSigned, signature.IsValid, signature.PublisherMatches, hashMatches: true);
                Status = signature.IsSigned
                    ? HighImpactAllowed ? "Authenticode signature valid · publisher HighLionNet" : "Signed build failed signature or publisher validation"
                    : "Unsigned community build · current hash recorded";
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

    public static bool EvaluatePolicy(bool isSigned, bool signatureValid, bool publisherMatches, bool hashMatches) =>
        isSigned ? signatureValid && publisherMatches : true;

    private static bool IsHashMatch(string? previous, string current)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(previous) && previous.Length == 64 && current.Length == 64 &&
                   CryptographicOperations.FixedTimeEquals(Convert.FromHexString(previous), Convert.FromHexString(current));
        }
        catch { return false; }
    }

    private static (bool IsSigned, bool IsValid, bool PublisherMatches) InspectSignature(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            return (true, false, false);

        int trustResult;
        try
        {
            trustResult = VerifyAuthenticode(executable);
        }
        catch
        {
            return (true, false, false);
        }

        // Only Windows' explicit "no signature" result is treated as an unsigned
        // community build. A malformed, revoked, or otherwise unverifiable signature
        // is a signed-invalid build and therefore fails closed for high-impact Apply.
        if (trustResult == TrustENoSignature) return (false, false, false);
        if (trustResult != 0) return (true, false, false);

        try
        {
            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(executable));
            var publisher = certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false)
                .Equals("HighLionNet", StringComparison.OrdinalIgnoreCase);
            return (true, true, publisher);
        }
        catch (CryptographicException)
        {
            return (true, false, false);
        }
    }

    private static string DescribeSignature((bool IsSigned, bool IsValid, bool PublisherMatches) signature) =>
        !signature.IsSigned ? "Unsigned community build" :
        !signature.IsValid ? "Invalid Authenticode signature" :
        signature.PublisherMatches ? "Valid Authenticode · HighLionNet" :
        "Valid Authenticode · unexpected publisher";

    private static int VerifyAuthenticode(string executable)
    {
        var file = new WinTrustFileInfo
        {
            Size = (uint)Marshal.SizeOf<WinTrustFileInfo>(),
            FilePath = executable
        };
        var filePointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>());
        try
        {
            Marshal.StructureToPtr(file, filePointer, fDeleteOld: false);
            var data = new WinTrustData
            {
                Size = (uint)Marshal.SizeOf<WinTrustData>(),
                UiChoice = 2, // WTD_UI_NONE
                RevocationChecks = 0, // WTD_REVOKE_NONE
                UnionChoice = 1, // WTD_CHOICE_FILE
                FileInfo = filePointer,
                ProviderFlags = 0x00001000 // WTD_CACHE_ONLY_URL_RETRIEVAL
            };
            var action = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE"); // WINTRUST_ACTION_GENERIC_VERIFY_V2
            return WinVerifyTrust(IntPtr.Zero, ref action, ref data);
        }
        finally
        {
            Marshal.DestroyStructure<WinTrustFileInfo>(filePointer);
            Marshal.FreeHGlobal(filePointer);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustFileInfo
    {
        public uint Size;
        [MarshalAs(UnmanagedType.LPWStr)] public string FilePath;
        public IntPtr FileHandle;
        public IntPtr KnownSubject;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustData
    {
        public uint Size;
        public IntPtr PolicyCallbackData;
        public IntPtr SipClientData;
        public uint UiChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public IntPtr FileInfo;
        public uint StateAction;
        public IntPtr StateData;
        public IntPtr UrlReference;
        public uint ProviderFlags;
        public uint UiContext;
        public IntPtr SignatureSettings;
    }

    [DllImport("wintrust.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int WinVerifyTrust(IntPtr window, ref Guid actionId, ref WinTrustData data);
}
