using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Microsoft.Win32.SafeHandles;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Services;

public sealed class HighImpactStepUpService
{
    private readonly IAuditLogger _log;

    public HighImpactStepUpService(IAuditLogger log) => _log = log;

    public bool TryAuthorize(IReadOnlyList<PendingPolicyChange> changes, Window? owner)
    {
        var names = string.Join("\n", changes.Select(change => "• " + change.Setting.ObjectName));
        var risks = string.Join(" ", changes.Select(change => Risk(change.Setting)).Distinct());
        var warning = new HighImpactWarningDialog("The following controls will change:\n" + names, risks)
        {
            Owner = owner
        };
        if (warning.ShowDialog() != true)
        {
            _log.Auth("HighImpactStepUp", "result=Cancelled stage=Warning");
            return false;
        }

        var success = VerifyAdministratorCredential(owner);
        _log.Auth("HighImpactStepUp", "result=" + (success ? "Verified" : "Denied") + " stage=WindowsCredential");
        return success;
    }

    public bool TryAuthorizeBinaryVerification(Window? owner)
    {
        var warning = new HighImpactWarningDialog(
            "The current executable hash will replace the previously verified hash.",
            "Only continue if you intentionally installed or rebuilt this version from a source you trust.")
        { Owner = owner };
        if (warning.ShowDialog() != true)
        {
            _log.Auth("BinaryIntegrity", "result=Cancelled stage=Warning");
            return false;
        }
        var success = VerifyAdministratorCredential(owner);
        _log.Auth("BinaryIntegrity", "result=" + (success ? "CredentialVerified" : "Denied"));
        return success;
    }

    private static string Risk(ManagedObject item)
    {
        if (item.ObjectId.StartsWith("policy.bitlocker.", StringComparison.OrdinalIgnoreCase))
            return "BitLocker changes can start encryption or make a drive inaccessible if recovery information is unavailable.";
        if (item.ObjectId.StartsWith("policy.uac.", StringComparison.OrdinalIgnoreCase))
            return "User Account Control changes can weaken elevation boundaries or alter every later administrator prompt.";
        if (item.ObjectId.Equals("policy.recall.disableaidataanalysis", StringComparison.OrdinalIgnoreCase))
            return "Turning off Recall snapshot saving can remove existing snapshots.";
        if (item.ObjectId.Equals("policy.copilot.removemicrosoftcopilotapp", StringComparison.OrdinalIgnoreCase))
            return "This policy removes the Microsoft Copilot app for affected users.";
        return "This can reduce protection, block updates, or expose a remote entry point.";
    }

    private static bool VerifyAdministratorCredential(Window? owner)
    {
        var info = new CredUiInfo
        {
            Size = Marshal.SizeOf<CredUiInfo>(),
            Parent = owner is null ? IntPtr.Zero : new System.Windows.Interop.WindowInteropHelper(owner).Handle,
            Caption = "Windows Privacy Platform — high-impact verification",
            Message = "Verify an administrator credential to continue. Credentials are never stored."
        };
        uint package = 0;
        var save = false;
        var result = CredUIPromptForWindowsCredentials(ref info, 0, ref package, IntPtr.Zero, 0,
            out var buffer, out var bufferSize, ref save,
            CredUiWinGeneric | CredUiWinEnumerateAdmins | CredUiWinSecurePrompt);
        if (result != 0 || buffer == IntPtr.Zero) return false;

        try
        {
            var user = new StringBuilder(256);
            var domain = new StringBuilder(256);
            var password = new StringBuilder(256);
            var userSize = user.Capacity;
            var domainSize = domain.Capacity;
            var passwordSize = password.Capacity;
            if (!CredUnPackAuthenticationBuffer(0, buffer, bufferSize, user, ref userSize,
                    domain, ref domainSize, password, ref passwordSize))
                return false;
            try
            {
                if (!LogonUser(user.ToString(), domain.ToString(), password.ToString(), 2, 0, out var token))
                    return false;
                token.Dispose();
                return true;
            }
            finally
            {
                password.Clear();
                user.Clear();
                domain.Clear();
            }
        }
        finally
        {
            RtlSecureZeroMemory(buffer, bufferSize);
            Marshal.FreeCoTaskMem(buffer);
        }
    }

    private const uint CredUiWinGeneric = 0x1;
    private const uint CredUiWinEnumerateAdmins = 0x100;
    private const uint CredUiWinSecurePrompt = 0x1000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CredUiInfo
    {
        public int Size;
        public IntPtr Parent;
        public string Message;
        public string Caption;
        public IntPtr Banner;
    }

    [DllImport("credui.dll", CharSet = CharSet.Unicode)]
    private static extern uint CredUIPromptForWindowsCredentials(ref CredUiInfo info, uint authError,
        ref uint authPackage, IntPtr inputBuffer, uint inputSize, out IntPtr outputBuffer,
        out uint outputSize, ref bool save, uint flags);

    [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredUnPackAuthenticationBuffer(uint flags, IntPtr authBuffer,
        uint authBufferSize, StringBuilder user, ref int userSize, StringBuilder domain,
        ref int domainSize, StringBuilder password, ref int passwordSize);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LogonUser(string user, string domain, string password, int logonType,
        int logonProvider, out SafeAccessTokenHandle token);

    [DllImport("kernel32.dll", EntryPoint = "RtlSecureZeroMemory")]
    private static extern IntPtr RtlSecureZeroMemory(IntPtr destination, uint length);
}
