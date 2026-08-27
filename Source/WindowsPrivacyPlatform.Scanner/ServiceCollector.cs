using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner;

/// <summary>
/// Bounded, cancellation-aware and read-only Windows service inspection.
/// Service discovery never grants mutation authority.
/// </summary>
public sealed class ServiceCollector : IInventoryCollector
{
    private const int MaxServices = 20_000;
    public string Name => nameof(ServiceCollector);

    public void Collect(InventorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        ServiceController[] services;
        try
        {
            services = ServiceController.GetServices();
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch
        {
            return;
        }

        try
        {
            foreach (var service in services.Take(MaxServices))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    snapshot.Services.Add(ReadService(service, cancellationToken));
                }
                catch (UnauthorizedAccessException)
                {
                    snapshot.Services.Add(new ServiceInfo
                    {
                        Name = Safe(service.ServiceName), DisplayName = Safe(service.DisplayName),
                        State = "Unknown", StartMode = "Unknown", AccessDenied = true,
                        ConfigurationError = "Windows denied access to part of this service configuration."
                    });
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    snapshot.Services.Add(new ServiceInfo
                    {
                        Name = Safe(service.ServiceName), DisplayName = Safe(service.DisplayName),
                        State = "Unknown", StartMode = "Unknown",
                        ConfigurationError = "Service inspection failed (" + ex.GetType().Name + ")."
                    });
                }
            }
        }
        finally
        {
            foreach (var service in services)
                try { service.Dispose(); } catch { }
        }
    }

    private static ServiceInfo ReadService(ServiceController service, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var name = Safe(service.ServiceName);
        var suffix = name.Split('_').LastOrDefault();
        var info = new ServiceInfo
        {
            Name = name,
            DisplayName = Safe(service.DisplayName),
            StartMode = service.StartType.ToString(),
            State = service.Status.ToString(),
            IsUserService = name.Contains('_') && suffix?.Length is >= 5 and <= 12
        };

        TryReadRelations(service, info);
        if (name.Length == 0 || name.Length > 256 || name.Contains('\\') || name.Contains('/'))
        {
            info.ConfigurationError = "The service name is not safe to use as a registry subkey.";
            return info;
        }

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name, writable: false);
            if (key is null)
            {
                info.ConfigurationError = "The Service Control Manager entry has no readable registry configuration.";
                return info;
            }

            info.Description = Safe(key.GetValue("Description")?.ToString());
            info.Account = Safe(key.GetValue("ObjectName")?.ToString());
            info.DelayedAutoStart = ConvertToInt(key.GetValue("DelayedAutoStart")) == 1;
            using var triggerKey = key.OpenSubKey("TriggerInfo", writable: false);
            info.TriggerStart = triggerKey is { SubKeyCount: > 0 } ? "Configured" : "Not observed";

            var image = Safe(key.GetValue("ImagePath", null, RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString());
            info.ExecutablePath = ResolveExecutablePath(image);
            if (!string.IsNullOrWhiteSpace(info.ExecutablePath) && Path.IsPathRooted(info.ExecutablePath))
            {
                info.MissingExecutable = !File.Exists(info.ExecutablePath);
                if (!info.MissingExecutable)
                    ReadPublisher(info);
            }
        }
        catch (UnauthorizedAccessException)
        {
            info.AccessDenied = true;
        }
        catch (System.Security.SecurityException)
        {
            info.AccessDenied = true;
        }
        catch (Exception ex)
        {
            info.ConfigurationError = "Registry configuration could not be read (" + ex.GetType().Name + ").";
        }

        AddTags(info);
        return info;
    }

    private static void TryReadRelations(ServiceController service, ServiceInfo info)
    {
        ServiceController[] dependencies = [];
        try
        {
            dependencies = service.ServicesDependedOn;
            info.Dependencies = dependencies.Take(128)
                .Select(s => Safe(s.ServiceName)).Where(s => s.Length > 0).ToList();
        }
        catch (Exception ex)
        {
            info.Dependencies.Add("Unable to verify dependencies (" + ex.GetType().Name + ")");
        }
        finally
        {
            foreach (var dependency in dependencies)
                try { dependency.Dispose(); } catch { }
        }

        ServiceController[] dependents = [];
        try
        {
            dependents = service.DependentServices;
            info.Dependents = dependents.Take(128)
                .Select(s => Safe(s.ServiceName)).Where(s => s.Length > 0).ToList();
        }
        catch
        {
            // Dependents are optional evidence; an empty list is not presented as proof of absence.
        }
        finally
        {
            foreach (var dependent in dependents)
                try { dependent.Dispose(); } catch { }
        }
    }

    private static void ReadPublisher(ServiceInfo info)
    {
        try
        {
            var version = FileVersionInfo.GetVersionInfo(info.ExecutablePath);
            info.Publisher = Safe(version.CompanyName);
            info.IsMicrosoft = info.Publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)
                ? true
                : string.IsNullOrWhiteSpace(info.Publisher) ? null : false;
        }
        catch
        {
            info.Publisher = string.Empty;
            info.IsMicrosoft = null;
        }

        try
        {
#pragma warning disable SYSLIB0057
            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(info.ExecutablePath));
#pragma warning restore SYSLIB0057
            info.SignatureStatus = "Embedded signature present; trust not validated";
            if (string.IsNullOrWhiteSpace(info.Publisher))
                info.Publisher = Safe(certificate.GetNameInfo(X509NameType.SimpleName, false));
        }
        catch
        {
            info.SignatureStatus = "No embedded signature observed";
        }
    }

    private static string ResolveExecutablePath(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return string.Empty;

        var expanded = Environment.ExpandEnvironmentVariables(imagePath.Trim());
        if (expanded.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase))
            expanded = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), expanded[12..]);
        else if (expanded.StartsWith("System32\\", StringComparison.OrdinalIgnoreCase))
            expanded = Path.Combine(Environment.SystemDirectory, expanded[9..]);

        string candidate;
        if (expanded.StartsWith('"'))
        {
            var closing = expanded.IndexOf('"', 1);
            candidate = closing > 1 ? expanded[1..closing] : expanded.Trim('"');
        }
        else
        {
            var exe = expanded.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            candidate = exe >= 0 ? expanded[..(exe + 4)] : expanded.Split(' ', 2)[0];
        }

        try { return Path.GetFullPath(candidate); }
        catch { return candidate; }
    }

    private static void AddTags(ServiceInfo info)
    {
        if (info.IsMicrosoft == true) info.Tags.Add("Microsoft");
        else if (info.IsMicrosoft == false) info.Tags.Add("Third-party publisher metadata");
        else info.Tags.Add("Publisher unknown");
        info.Tags.Add(info.IsUserService ? "User service" : "System service");
        if (info.DelayedAutoStart) info.Tags.Add("Delayed start");
        if (info.TriggerStart == "Configured") info.Tags.Add("Trigger start");
    }

    private static int ConvertToInt(object? value)
    {
        try { return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture); }
        catch { return 0; }
    }

    private static string Safe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var clean = new string(value.Where(c => !char.IsControl(c) || c is '\t' or '\n').Take(4096).ToArray());
        return clean.Trim();
    }
}
