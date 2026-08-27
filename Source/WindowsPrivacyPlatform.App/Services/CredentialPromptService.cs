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
    private const uint CredUiWinGeneric = 0x1;
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
                out packed, out packedSize, ref save, CredUiWinGeneric);
            if (prompt == ErrorCancelled)
            {
                _log.Auth("CredentialPromptService", "Windows credential prompt cancelled.");
                return new CredentialAuthorizationResult(false, true, "Admin authorization was cancelled.");
            }
            if (prompt != 0)
            {
                _failedAttempts++;
                _log.Auth("CredentialPromptService", $"Windows credential prompt failed: win32={prompt}.");
                return new CredentialAuthorizationResult(false, false, "Windows could not open the credential prompt.");
            }

            var userLength = (uint)user.Capacity;
            var domainLength = (uint)domain.Capacity;
            var passwordLength = (uint)password.Capacity;
            if (!CredUnPackAuthenticationBuffer(0, packed, packedSize, user, ref userLength,
                    domain, ref domainLength, password, ref passwordLength))
            {
                var error = Marshal.GetLastWin32Error();
                _failedAttempts++;
                _log.Auth("CredentialPromptService", $"Credential buffer could not be unpacked: win32={error}.");
                return new CredentialAuthorizationResult(false, false, "Windows could not verify those credentials.");
            }

            var account = NormalizeAccount(user.ToString(), domain.ToString());
            using var passwordBuffer = UnmanagedPasswordBuffer.From(password);
            if (!LogonUser(account.UserName, account.Domain, passwordBuffer.Pointer,
                    Logon32LogonInteractive, Logon32ProviderDefault, out var token))
            {
                var error = Marshal.GetLastWin32Error();
                _failedAttempts++;
                _log.Auth("CredentialPromptService",
                    $"LogonUser rejected the supplied credential: win32={error}; account-form={account.Form}.");
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

    internal static (string UserName, string? Domain, string Form) NormalizeAccount(string userName, string? domain)
    {
        var user = (userName ?? string.Empty).Trim();
        var accountDomain = (domain ?? string.Empty).Trim();

        // Generic CredUI can return either separate fields or a qualified account name.
        // LogonUser requires those forms to be split explicitly.
        var separator = user.IndexOf('\\');
        if (separator > 0 && separator < user.Length - 1)
        {
            accountDomain = user[..separator];
            user = user[(separator + 1)..];
        }

        if (user.Contains('@', StringComparison.Ordinal))
            return (user, null, "upn");

        if (string.IsNullOrWhiteSpace(accountDomain) ||
            accountDomain.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
            accountDomain.Equals(".", StringComparison.Ordinal))
            return (user, ".", "local");

        return (user, accountDomain, "domain");
    }

    private sealed class UnmanagedPasswordBuffer : IDisposable
    {
        private readonly int _byteCount;
        public IntPtr Pointer { get; private set; }

        private UnmanagedPasswordBuffer(StringBuilder password)
        {
            _byteCount = checked((password.Length + 1) * sizeof(char));
            Pointer = Marshal.AllocHGlobal(_byteCount);
            for (var index = 0; index < password.Length; index++)
                Marshal.WriteInt16(Pointer, index * sizeof(char), password[index]);
            Marshal.WriteInt16(Pointer, password.Length * sizeof(char), 0);
        }

        public static UnmanagedPasswordBuffer From(StringBuilder password) => new(password);

        public void Dispose()
        {
            if (Pointer == IntPtr.Zero) return;
            for (var index = 0; index < _byteCount; index++) Marshal.WriteByte(Pointer, index, 0);
            Marshal.FreeHGlobal(Pointer);
            Pointer = IntPtr.Zero;
        }
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
    private static extern bool LogonUser(string userName, string? domain, IntPtr password, int logonType,
        int logonProvider, out SafeAccessTokenHandle token);
}
