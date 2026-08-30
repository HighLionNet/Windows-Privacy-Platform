using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner;

/// <summary>Fail-soft, read-only observation of the Windows DNS resolution layers.</summary>
public sealed class NetworkingCollector : IInventoryCollector
{
    private const string ProbeName = "dns.msftncsi.com";
    public string Name => "NetworkingCollector";

    public void Collect(InventorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var dns = new DnsResolutionSnapshot { CapturedAtUtc = DateTime.UtcNow };
        snapshot.Networking.Dns = dns;

        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces()
                         .Where(item => item.OperationalStatus == OperationalStatus.Up &&
                                        item.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                cancellationToken.ThrowIfCancellationRequested();
                dns.Interfaces.Add(ReadInterface(adapter));
            }
        }
        catch (NetworkInformationException)
        {
            dns.PreferredPath = Layer(EvidenceState.Error, "Adapter enumeration failed.", "System.Net.NetworkInformation");
        }
        catch (UnauthorizedAccessException)
        {
            dns.PreferredPath = Layer(EvidenceState.AccessDenied, "Windows denied adapter evidence.", "System.Net.NetworkInformation");
        }

        ReadNrpt(dns);
        ReadDoh(snapshot, dns);
        ReadPreferredPath(dns);
        ReadExternalAppBoundary(dns);
        ProbeResolvers(dns, cancellationToken);
    }

    private static DnsInterfaceInfo ReadInterface(NetworkInterface adapter)
    {
        var item = new DnsInterfaceInfo
        {
            Id = adapter.Id,
            Name = adapter.Name,
            Description = adapter.Description,
            Type = adapter.NetworkInterfaceType.ToString(),
            Status = adapter.OperationalStatus.ToString(),
            IsVpnOrTunnel = adapter.NetworkInterfaceType is NetworkInterfaceType.Tunnel or NetworkInterfaceType.Ppp ||
                            adapter.Description.Contains("VPN", StringComparison.OrdinalIgnoreCase) ||
                            adapter.Name.Contains("VPN", StringComparison.OrdinalIgnoreCase),
            Evidence = EvidenceState.Configured
        };

        try
        {
            var properties = adapter.GetIPProperties();
            var ipv4 = properties.GetIPv4Properties();
            var ipv6 = properties.GetIPv6Properties();
            item.InterfaceIndex = ipv4?.Index ?? ipv6?.Index ?? 0;
            item.IPv4Addresses = properties.UnicastAddresses
                .Where(address => address.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(address => address.Address.ToString()).ToList();
            item.IPv6Addresses = properties.UnicastAddresses
                .Where(address => address.Address.AddressFamily == AddressFamily.InterNetworkV6)
                .Select(address => address.Address.ToString()).ToList();
            item.DnsServers = properties.DnsAddresses.Select(address => address.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            ReadInterfaceRegistry(item, ipv4?.IsDhcpEnabled == true);
        }
        catch (NetworkInformationException)
        {
            item.Evidence = EvidenceState.Error;
            item.AddressSource = "Unknown (adapter properties failed)";
        }

        return item;
    }

    private static void ReadInterfaceRegistry(DnsInterfaceInfo item, bool dhcpEnabled)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\" + item.Id);
            var staticServers = key?.GetValue("NameServer")?.ToString()?.Trim();
            var dhcpServers = key?.GetValue("DhcpNameServer")?.ToString()?.Trim();
            item.AddressSource = !string.IsNullOrWhiteSpace(staticServers) ? "Static" :
                dhcpEnabled && !string.IsNullOrWhiteSpace(dhcpServers) ? "DHCP" : "Unknown";
            if (key?.GetValue("InterfaceMetric") is int metric)
                item.InterfaceMetric = metric;
        }
        catch (UnauthorizedAccessException)
        {
            item.AddressSource = "Unknown (access denied)";
        }
        catch
        {
            item.AddressSource = "Unknown";
        }
    }

    private static void ReadNrpt(DnsResolutionSnapshot dns)
    {
        const string path = @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient\DnsPolicyConfig";
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var root = baseKey.OpenSubKey(path);
            if (root is null || root.GetSubKeyNames().Length == 0)
            {
                dns.Nrpt = Layer(EvidenceState.NotConfigured, "No NRPT rules are configured.", "HKLM\\" + path);
                return;
            }

            foreach (var childName in root.GetSubKeyNames())
            {
                using var child = root.OpenSubKey(childName);
                if (child is null) continue;
                dns.NrptRules.Add(new NrptRuleInfo
                {
                    Namespace = Value(child, "Name", childName),
                    NameServers = Value(child, "GenericDNSServers", "Unknown"),
                    DnsSec = Value(child, "DnsSecValidationRequired", "Unknown"),
                    DirectAccess = Value(child, "ConfigOptions", "Unknown"),
                    Source = @"HKLM\" + path + "\\" + childName
                });
            }
            dns.Nrpt = Layer(EvidenceState.Configured, $"{dns.NrptRules.Count} namespace override(s) observed.", "HKLM\\" + path);
        }
        catch (UnauthorizedAccessException)
        {
            dns.Nrpt = Layer(EvidenceState.AccessDenied, "Windows denied access to NRPT policy.", "HKLM\\" + path);
        }
        catch (Exception ex)
        {
            dns.Nrpt = Layer(EvidenceState.Error, "NRPT observation failed (" + ex.GetType().Name + ").", "HKLM\\" + path);
        }
    }

    private static void ReadDoh(InventorySnapshot snapshot, DnsResolutionSnapshot dns)
    {
        var policy = snapshot.PolicySettings.FirstOrDefault(item =>
            item.Name.Equals("policy.network.dohmode", StringComparison.OrdinalIgnoreCase));
        dns.WindowsDoh = policy?.Status switch
        {
            RegistryObservationStatus.Present => Layer(EvidenceState.Configured,
                "Windows DoH policy value: " + policy.Value + ". Per-resolver encrypted use is not inferred from this value alone.",
                "HKLM\\" + policy.Path + "\\" + policy.ValueName),
            RegistryObservationStatus.NotConfigured => Layer(EvidenceState.NotConfigured,
                "Windows DoH policy is not configured; resolver encryption in use remains Unknown.",
                @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient\DoHPolicy"),
            RegistryObservationStatus.AccessDenied => Layer(EvidenceState.AccessDenied, "Windows denied DoH policy evidence.", "PolicyCollector"),
            RegistryObservationStatus.Error => Layer(EvidenceState.Error, "DoH policy observation failed.", "PolicyCollector"),
            _ => Layer(EvidenceState.Unknown, "Windows resolver encryption could not be proved.", "PolicyCollector")
        };
    }

    private static void ReadPreferredPath(DnsResolutionSnapshot dns)
    {
        try
        {
            var destination = BitConverter.ToUInt32(IPAddress.Parse("1.1.1.1").GetAddressBytes(), 0);
            if (GetBestInterface(destination, out var index) != 0)
            {
                dns.PreferredPath = Layer(EvidenceState.Unknown, "Windows did not identify a preferred general route.", "GetBestInterface");
                dns.VpnDnsPath = Layer(EvidenceState.Unknown, "VPN DNS participation could not be proved.", "GetBestInterface");
                return;
            }

            var selected = dns.Interfaces.FirstOrDefault(item => item.InterfaceIndex == index);
            if (selected is null)
            {
                dns.PreferredPath = Layer(EvidenceState.Unknown, $"Windows selected interface index {index}, which was not in the observed adapter set.", "GetBestInterface");
                dns.VpnDnsPath = Layer(EvidenceState.Unknown, "VPN DNS participation could not be proved.", "GetBestInterface");
                return;
            }

            var resolvers = selected.DnsServers.Count == 0 ? "no resolver addresses were returned" : string.Join(", ", selected.DnsServers);
            dns.PreferredPath = Layer(EvidenceState.Configured,
                $"Windows' general route selects {selected.Name} (index {selected.InterfaceIndex}); observed DNS: {resolvers}.",
                "GetBestInterface + System.Net.NetworkInformation");
            dns.VpnDnsPath = selected.IsVpnOrTunnel
                ? Layer(EvidenceState.Configured, $"The selected path is the VPN/tunnel interface {selected.Name}.", "GetBestInterface")
                : Layer(EvidenceState.NotObserved, "The selected general path is not an observed VPN/tunnel interface; NRPT or app DNS may still differ.", "GetBestInterface");
        }
        catch
        {
            dns.PreferredPath = Layer(EvidenceState.Unknown, "The effective general interface could not be proved.", "GetBestInterface");
            dns.VpnDnsPath = Layer(EvidenceState.Unknown, "VPN DNS participation could not be proved.", "GetBestInterface");
        }
    }

    private static void ReadExternalAppBoundary(DnsResolutionSnapshot dns)
    {
        foreach (var app in new[] { "Microsoft Edge", "Google Chrome", "Mozilla Firefox", "VPN applications" })
        {
            dns.ExternalApps.Add(new ExternalDnsInfo
            {
                Application = app,
                Evidence = EvidenceState.Unknown,
                Summary = app + " may use an encrypted DNS resolver outside the Windows DNS client. WPP cannot infer that state from Windows adapter DNS."
            });
        }
    }

    private static void ProbeResolvers(DnsResolutionSnapshot dns, CancellationToken cancellationToken)
    {
        var executable = Path.Combine(Environment.SystemDirectory, "nslookup.exe");
        foreach (var resolver in dns.Interfaces.SelectMany(item => item.DnsServers)
                     .Distinct(StringComparer.OrdinalIgnoreCase).Take(8))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = SafeProcessRunner.Run(executable, [ProbeName, resolver], TimeSpan.FromSeconds(4), cancellationToken, Encoding.UTF8);
            var addresses = ParseAddresses(result.StdOut).Where(address => !address.Equals(resolver, StringComparison.OrdinalIgnoreCase)).ToList();
            dns.ResolverProbes.Add(new DnsProbeInfo
            {
                Resolver = resolver,
                QueryName = ProbeName,
                Evidence = result.Started && !result.TimedOut && !result.Canceled && result.ExitCode == 0 && addresses.Count > 0
                    ? EvidenceState.Configured : EvidenceState.Error,
                Answer = addresses.Count > 0 ? string.Join(", ", addresses) : "No verified answer",
                Source = executable + " (fixed query name; observed resolver address)"
            });
        }
    }

    private static IEnumerable<string> ParseAddresses(string output)
    {
        foreach (var token in (output ?? string.Empty).Split([' ', '\t', '\r', '\n', ',', '[', ']'], StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = token.TrimEnd('.', ':');
            if (IPAddress.TryParse(candidate, out var address))
                yield return address.ToString();
        }
    }

    private static string Value(RegistryKey key, string name, string fallback) =>
        key.GetValue(name)?.ToString()?.Trim() is { Length: > 0 } value ? value : fallback;

    private static DnsLayerObservation Layer(EvidenceState evidence, string summary, string source) =>
        new() { Evidence = evidence, Summary = summary, Source = source };

    [System.Runtime.InteropServices.DllImport("iphlpapi.dll")]
    private static extern uint GetBestInterface(uint destinationAddress, out uint bestInterfaceIndex);
}
