using System.Security.AccessControl;
using System.Security.Principal;
using System.IO;

namespace WindowsPrivacyPlatform.App.Services;

public static class LocalDataSecurity
{
    public static bool EnsurePrivateAcl(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            using var identity = WindowsIdentity.GetCurrent();
            var currentUser = identity.User ?? throw new InvalidOperationException("Current user SID unavailable.");
            var inheritance = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(new FileSystemAccessRule(currentUser, FileSystemRights.FullControl,
                inheritance, PropagationFlags.None, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                FileSystemRights.FullControl, inheritance, PropagationFlags.None, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                FileSystemRights.FullControl, inheritance, PropagationFlags.None, AccessControlType.Allow));
            FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(directory), security);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
