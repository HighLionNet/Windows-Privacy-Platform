using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32.SafeHandles;
using WindowsPrivacyPlatform.Logging;

namespace WindowsPrivacyPlatform.App.Services;

public sealed record CredentialAuthorizationResult(bool Authorized, bool Cancelled, string Error);

public interface ICredentialPromptService
{
    CredentialAuthorizationResult AuthorizeAdmin(Window? owner, string reason);
}

/// <summary>Windows-owned CredUI prompt followed by a local LogonUser verification.</summary>
public sealed class CredentialPromptService : ICredentialPromptService
{
    private const int ErrorCancelled = 1223;
    private const int Logon32LogonInteractive = 2;
    private const int Logon32ProviderDefault = 0;
    private const uint CredUiWinEnumerateAdmins = 0x100;
    private const uint CredUiWinSecurePrompt = 0x1000;
    private readonly IAuditLogger _log;
    private int _failedAttempts;

    public CredentialPromptService(IAuditLogger log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    public CredentialAuthorizationResult AuthorizeAdmin(Window? owner, string reason)
    {
        if (_failedAttempts >= 3)
            return new CredentialAuthorizationResult(false, false,
                "Admin authorization is paused after three unsuccessful attempts. Continue in View-only and restart the app before trying again.");

        var info = new CredUiInfo
        {
            Size = Marshal.SizeOf<CredUiInfo>(),
            Parent = owner is null ? IntPtr.Zero : new WindowInteropHelper(owner).Handle,
            Caption = "Windows Privacy Platform — Admin authorization",
            Message = string.IsNullOrWhiteSpace(reason)
                ? "Enter an administrator password to authorize Admin mode."
                : reason
        };

        uint authPackage = 0;
        IntPtr packed = IntPtr.Zero;
        uint packedSize = 0;
        var save = false;
        var user = new StringBuilder(256);
        var domain = new StringBuilder(256);
        var password = new StringBuilder(256);
        try
        {
            var prompt = CredUIPromptForWindowsCredentials(ref info, 0, ref authPackage, IntPtr.Zero, 0,
                out packed, out packedSize, ref save, CredUiWinEnumerateAdmins | CredUiWinSecurePrompt);
            if (prompt == ErrorCancelled)
            {
                _log.Auth("CredentialPromptService", "Windows credential prompt cancelled.");
                return new CredentialAuthorizationResult(false, true, "Admin authorization was cancelled.");
            }
            if (prompt != 0)
            {
                _failedAttempts++;
                _log.Auth("CredentialPromptService", "Windows credential prompt failed with a Windows error code.");
                return new CredentialAuthorizationResult(false, false, "Windows could not open the credential prompt.");
            }

            var userLength = (uint)user.Capacity;
            var domainLength = (uint)domain.Capacity;
            var passwordLength = (uint)password.Capacity;
            if (!CredUnPackAuthenticationBuffer(0, packed, packedSize, user, ref userLength,
                    domain, ref domainLength, password, ref passwordLength))
            {
                _failedAttempts++;
                _log.Auth("CredentialPromptService", "Credential buffer could not be unpacked.");
                return new CredentialAuthorizationResult(false, false, "Windows could not verify those credentials.");
            }

            if (!LogonUser(user.ToString(), domain.Length == 0 ? null : domain.ToString(), password,
                    Logon32LogonInteractive, Logon32ProviderDefault, out var token))
            {
                _failedAttempts++;
                _log.Auth("CredentialPromptService", "LogonUser rejected the supplied credential.");
                return new CredentialAuthorizationResult(false, false, "The password was not accepted.");
            }

            using (token)
            using (var identity = new WindowsIdentity(token.DangerousGetHandle()))
            {
                if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    _failedAttempts++;
                    _log.Auth("CredentialPromptService", "Verified credential was not an administrator account.");
                    return new CredentialAuthorizationResult(false, false, "That account is not an administrator on this PC.");
                }
            }

            _failedAttempts = 0;
            _log.Auth("CredentialPromptService", "Administrator password verified locally by Windows.");
            return new CredentialAuthorizationResult(true, false, string.Empty);
        }
        catch (Exception ex) when (ex is Win32Exception or ExternalException or InvalidOperationException)
        {
            _failedAttempts++;
            _log.Auth("CredentialPromptService", "Credential verification failed: category=" + ex.GetType().Name);
            return new CredentialAuthorizationResult(false, false, "Windows could not verify the administrator password.");
        }
        finally
        {
            Zero(user);
            Zero(domain);
            Zero(password);
            if (packed != IntPtr.Zero)
            {
                for (var index = 0; index < packedSize; index++) Marshal.WriteByte(packed, index, 0);
                Marshal.FreeCoTaskMem(packed);
            }
        }
    }

    private static void Zero(StringBuilder value)
    {
        for (var index = 0; index < value.Length; index++) value[index] = '\0';
        value.Clear();
    }

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
    private static extern int CredUIPromptForWindowsCredentials(ref CredUiInfo info, int authError,
        ref uint authPackage, IntPtr inputBuffer, uint inputSize, out IntPtr outputBuffer,
        out uint outputSize, ref bool save, uint flags);

    [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredUnPackAuthenticationBuffer(uint flags, IntPtr authBuffer, uint authBufferSize,
        StringBuilder userName, ref uint userNameLength, StringBuilder domainName, ref uint domainNameLength,
        StringBuilder password, ref uint passwordLength);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LogonUser(string userName, string? domain, [In] StringBuilder password, int logonType,
        int logonProvider, out SafeAccessTokenHandle token);
}
