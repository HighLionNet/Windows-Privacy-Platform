using System.Runtime.InteropServices;
using WindowsPrivacyPlatform.Logging;

namespace WindowsPrivacyPlatform.App.Services;

/// <summary>Best-effort process mitigations that remain compatible with WPF and bounded collectors.</summary>
public static class ProcessHardening
{
    public static void Apply(IAuditLogger log)
    {
        ApplyPolicy(log, 0, new DepPolicy { Flags = 1 });
        ApplyPolicy(log, 1, new AslrPolicy { Flags = 1 | 2 | 4 | 8 });
        ApplyPolicy(log, 3, new StrictHandlePolicy { Flags = 1 | 2 });
        ApplyPolicy(log, 6, new ExtensionPointPolicy { Flags = 1 });
        ApplyPolicy(log, 10, new ImageLoadPolicy { Flags = 1 | 2 | 4 });
        log.Info("ProcessHardening",
            "Child-process denial was not enabled because Windows exposes no per-image exceptions; WPP requires fixed-path read-only collectors and bounded self-relaunch. Win32k restriction was not enabled because WPF requires Win32k.");
    }

    private static void ApplyPolicy<T>(IAuditLogger log, int policy, T value) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(value, buffer, false);
            if (!SetProcessMitigationPolicy(policy, buffer, (nuint)size))
                log.Warning("ProcessHardening", $"Process mitigation {policy} was unavailable; continuing fail-soft.");
        }
        catch (Exception ex)
        {
            log.Warning("ProcessHardening", $"Process mitigation {policy} failed: {ex.GetType().Name}.");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [StructLayout(LayoutKind.Sequential)] private struct DepPolicy { public uint Flags; public bool Permanent; }
    [StructLayout(LayoutKind.Sequential)] private struct AslrPolicy { public uint Flags; }
    [StructLayout(LayoutKind.Sequential)] private struct StrictHandlePolicy { public uint Flags; }
    [StructLayout(LayoutKind.Sequential)] private struct ExtensionPointPolicy { public uint Flags; }
    [StructLayout(LayoutKind.Sequential)] private struct ImageLoadPolicy { public uint Flags; }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessMitigationPolicy(int mitigationPolicy, IntPtr buffer, nuint length);
}
