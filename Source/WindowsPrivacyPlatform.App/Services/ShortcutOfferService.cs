using System.Runtime.InteropServices;
using System.IO;
using System.Windows;
using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Core;

namespace WindowsPrivacyPlatform.App.Services;

public static class ShortcutOfferService
{
    public static void OfferIfNeeded(Window owner)
    {
        var product = ProductInfoReader.Read();
        var executable = ResolveExecutable();
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            return;

        var stateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ProductDirectoryName(product.Name));
        var statePath = Path.Combine(stateDirectory, "shortcut-offer.state");
        if (File.Exists(statePath))
            return;

        if (DesktopLinksTo(executable))
        {
            Persist(stateDirectory, statePath, "existing");
            return;
        }

        var choice = MessageBox.Show(
            owner,
            "Create shortcuts for this application?\n\n" +
            "A Desktop shortcut and a Start Menu entry will point to the executable in its current extracted folder. " +
            "Move the folder before creating shortcuts if this is not its permanent location.",
            "Create application shortcuts",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.Yes);

        if (choice != MessageBoxResult.Yes)
        {
            Persist(stateDirectory, statePath, "declined");
            return;
        }

        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            var fileName = ProductDirectoryName(product.Name) + ".lnk";
            CreateShortcut(Path.Combine(desktop, fileName), executable, product.Name);
            CreateShortcut(Path.Combine(programs, fileName), executable, product.Name);
            Persist(stateDirectory, statePath, "created");
            MessageBox.Show(owner, "Desktop and Start Menu shortcuts were created.", "Shortcuts created",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception)
        {
            MessageBox.Show(owner,
                "Windows could not create both shortcuts. The offer will be shown again next launch.",
                "Shortcut creation failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private static bool DesktopLinksTo(string executable)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (!Directory.Exists(desktop))
            return false;

        foreach (var link in Directory.EnumerateFiles(desktop, "*.lnk", SearchOption.TopDirectoryOnly))
        {
            object? shell = null;
            object? shortcut = null;
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType is null)
                    return false;
                shell = Activator.CreateInstance(shellType);
                shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, [link]);
                var target = shortcut?.GetType().InvokeMember("TargetPath", System.Reflection.BindingFlags.GetProperty, null, shortcut, null) as string;
                if (!string.IsNullOrWhiteSpace(target) &&
                    Path.GetFullPath(target).Equals(Path.GetFullPath(executable), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch
            {
                // A malformed or inaccessible shortcut does not prove that this application is linked.
            }
            finally
            {
                ReleaseCom(shortcut);
                ReleaseCom(shell);
            }
        }
        return false;
    }

    private static void CreateShortcut(string shortcutPath, string executable, string description)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
                        ?? throw new PlatformNotSupportedException("Windows Script Host shortcut support is unavailable.");
        object? shell = null;
        object? shortcut = null;
        try
        {
            shell = Activator.CreateInstance(shellType)
                    ?? throw new InvalidOperationException("Windows Script Host could not be started.");
            shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, [shortcutPath])
                       ?? throw new InvalidOperationException("Shortcut object could not be created.");
            var type = shortcut.GetType();
            type.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, [executable]);
            type.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, [Path.GetDirectoryName(executable) ?? string.Empty]);
            type.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, [executable + ",0"]);
            type.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, [description]);
            type.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
        }
        finally
        {
            ReleaseCom(shortcut);
            ReleaseCom(shell);
        }
    }

    private static void Persist(string stateDirectory, string statePath, string state)
    {
        AtomicLocalFile.WriteText(stateDirectory, statePath, state);
    }

    private static string ResolveExecutable()
    {
        var path = Environment.ProcessPath ?? string.Empty;
        if (path.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        return path;
    }

    private static string ProductDirectoryName(string productName) =>
        string.Concat(productName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

    private static void ReleaseCom(object? value)
    {
        try
        {
            if (value is not null && Marshal.IsComObject(value))
                Marshal.FinalReleaseComObject(value);
        }
        catch
        {
            // Best-effort COM cleanup.
        }
    }
}
