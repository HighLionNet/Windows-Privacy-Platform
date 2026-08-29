using System.Management;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner;

/// <summary>
/// Observes antivirus registrations exposed by Windows Security Center. This collector
/// is deliberately fail-soft and read-only; it never contacts or configures a vendor.
/// </summary>
public sealed class SecurityCenterCollector : IInventoryCollector
{
    public string Name => nameof(SecurityCenterCollector);

    public void Collect(InventorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        snapshot.Security.ProtectionProducts.Clear();
        snapshot.Security.ProtectionProductStatus = ProtectionProductObservationStatus.NotObserved;
        snapshot.Security.ProtectionProductCollectionNotes = string.Empty;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scope = new ManagementScope(@"\\.\root\SecurityCenter2");
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT displayName, productState FROM AntivirusProduct"));
            using var results = searcher.Get();

            foreach (ManagementObject product in results)
            {
                using (product)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var displayName = product["displayName"]?.ToString()?.Trim() ?? string.Empty;
                    if (displayName.Length == 0)
                        continue;

                    var rawState = TryReadState(product["productState"]);
                    snapshot.Security.ProtectionProducts.Add(new ProtectionProductInfo
                    {
                        DisplayName = displayName,
                        ProductState = rawState,
                        IsActive = rawState is null ? null : IsProductActive(rawState.Value),
                        IsMicrosoftDefender = displayName.Contains("Windows Defender", StringComparison.OrdinalIgnoreCase) ||
                            displayName.Contains("Microsoft Defender", StringComparison.OrdinalIgnoreCase)
                    });
                }
            }

            snapshot.Security.ProtectionProducts = snapshot.Security.ProtectionProducts
                .GroupBy(product => product.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            snapshot.Security.ProtectionProductStatus = snapshot.Security.ProtectionProducts.Count > 0
                ? ProtectionProductObservationStatus.Observed
                : ProtectionProductObservationStatus.NotObserved;
            snapshot.Security.ProtectionProductCollectionNotes = snapshot.Security.ProtectionProducts.Count > 0
                ? "Observed through Windows Security Center AntivirusProduct. Completeness for every EDR vendor is not assumed."
                : "Windows Security Center returned no AntivirusProduct rows; absence is not treated as no protection.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            MarkAccessDenied(snapshot);
        }
        catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.AccessDenied)
        {
            MarkAccessDenied(snapshot);
        }
        catch (Exception ex)
        {
            snapshot.Security.ProtectionProductStatus = ProtectionProductObservationStatus.Error;
            snapshot.Security.ProtectionProductCollectionNotes =
                $"Windows Security Center observation failed ({ex.GetType().Name}); protection state remains unknown.";
        }
    }

    /// <summary>
    /// Security Center providers conventionally encode their state in the middle byte.
    /// Only the recognized enabled marker is treated as active; every other value is
    /// retained as raw evidence without an active claim.
    /// </summary>
    public static bool IsProductActive(int productState) => ((productState >> 8) & 0xFF) == 0x10;

    private static int? TryReadState(object? value)
    {
        if (value is null)
            return null;
        try { return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    private static void MarkAccessDenied(InventorySnapshot snapshot)
    {
        snapshot.Security.ProtectionProductStatus = ProtectionProductObservationStatus.AccessDenied;
        snapshot.Security.ProtectionProductCollectionNotes =
            "Access to Windows Security Center AntivirusProduct was denied; protection state was not observed.";
    }
}
