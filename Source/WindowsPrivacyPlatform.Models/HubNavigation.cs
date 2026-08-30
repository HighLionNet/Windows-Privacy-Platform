namespace WindowsPrivacyPlatform.Models;

/// <summary>Pure presentation metadata for one item in the section-scoped rail.</summary>
public sealed record HubNavigationItem(
    HubSection? Section,
    string Title,
    string Tag,
    ProductDomain? Domain = null,
    bool IsPersistent = false,
    bool IsReadOnlyDestination = false);

/// <summary>
/// Owns the four-section information architecture. It classifies presentation only;
/// it never grants write authority or constructs a <see cref="WritableTarget"/>.
/// </summary>
public static class HubNavigation
{
    private static readonly ProductDomain[] PrivacyOrder =
    [
        ProductDomain.ConsentStore, ProductDomain.AppPrivacy, ProductDomain.Telemetry,
        ProductDomain.Location, ProductDomain.CloudContent, ProductDomain.Advertising,
        ProductDomain.ActivityHistory, ProductDomain.Device, ProductDomain.Speech,
        ProductDomain.Clipboard, ProductDomain.Copilot, ProductDomain.Recall,
        ProductDomain.Search, ProductDomain.Edge, ProductDomain.Widgets,
        ProductDomain.OneDrive, ProductDomain.FamilySafety, ProductDomain.Storage,
        ProductDomain.Accessibility
    ];

    private static readonly ProductDomain[] SecurityOrder =
    [
        ProductDomain.Defender, ProductDomain.ExploitProtection, ProductDomain.BitLocker,
        ProductDomain.Uac, ProductDomain.Biometrics, ProductDomain.WindowsHello,
        ProductDomain.LocalSecurity, ProductDomain.WindowsUpdate, ProductDomain.FindMyDevice
    ];

    private static readonly ProductDomain[] NetworkOrder =
    [
        ProductDomain.Network, ProductDomain.Firewall, ProductDomain.RemoteAccess
    ];

    public static HubSection For(ProductDomain domain) => domain switch
    {
        ProductDomain.ConsentStore or ProductDomain.AppPrivacy or ProductDomain.Telemetry or
        ProductDomain.Location or ProductDomain.CloudContent or ProductDomain.Advertising or
        ProductDomain.ActivityHistory or ProductDomain.Device or ProductDomain.Speech or
        ProductDomain.Clipboard or ProductDomain.Copilot or ProductDomain.Recall or
        ProductDomain.Search or ProductDomain.Edge or ProductDomain.Widgets or
        ProductDomain.OneDrive or ProductDomain.FamilySafety or ProductDomain.Storage or
        ProductDomain.Accessibility => HubSection.Privacy,

        ProductDomain.Defender or ProductDomain.ExploitProtection or ProductDomain.BitLocker or
        ProductDomain.Uac or ProductDomain.Biometrics or ProductDomain.WindowsHello or
        ProductDomain.LocalSecurity or ProductDomain.WindowsUpdate or ProductDomain.FindMyDevice => HubSection.Security,

        ProductDomain.Network or ProductDomain.Firewall or ProductDomain.RemoteAccess => HubSection.Network,

        // Other is retained for internal references and dynamic inventory only. Both
        // belong in the read-only Explore surface, never an editable fifth section.
        ProductDomain.Other => HubSection.Explore,

        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unmapped ProductDomain")
    };

    public static HubSection For(ManagedObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Bucket == CatalogBucket.SystemInventory ? HubSection.Explore : For(item.ProductDomain);
    }

    public static string DisplayName(HubSection section) => section switch
    {
        HubSection.Privacy => "Privacy",
        HubSection.Security => "Security",
        HubSection.Network => "Network",
        HubSection.Explore => "Troubleshoot/Explore",
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, null)
    };

    public static string AccentBrushKey(HubSection section) => section switch
    {
        HubSection.Privacy => "BrushDomainPrivacy",
        HubSection.Security => "BrushDomainSecurity",
        HubSection.Network => "BrushDomainWindows",
        HubSection.Explore => "BrushDomainApps",
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, null)
    };

    public static IReadOnlyList<HubNavigationItem> CategoriesFor(
        HubSection section,
        IEnumerable<ManagedObject> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        if (section == HubSection.Explore)
        {
            return
            [
                new(HubSection.Explore, "Windows services", "explore:windows-services", IsReadOnlyDestination: true),
                new(HubSection.Explore, "Other services", "explore:other-services"),
                new(HubSection.Explore, "Windows tasks", "explore:windows-tasks", IsReadOnlyDestination: true),
                new(HubSection.Explore, "Other tasks", "explore:other-tasks"),
                new(HubSection.Explore, "System apps", "explore:system-apps", IsReadOnlyDestination: true),
                new(HubSection.Explore, "Other apps", "explore:other-apps", IsReadOnlyDestination: true),
                new(HubSection.Explore, "Features & capabilities", "explore:features", IsReadOnlyDestination: true),
                new(HubSection.Explore, "Firewall rules", "explore:firewall-rules", IsReadOnlyDestination: true)
            ];
        }

        if (section == HubSection.Network)
        {
            return
            [
                new(HubSection.Network, "DNS & name resolution", "network:dns", ProductDomain.Network),
                new(HubSection.Network, "Adapters & LAN", "network:adapters", ProductDomain.Network, IsReadOnlyDestination: true),
                new(HubSection.Network, "Firewall", "domain:Firewall", ProductDomain.Firewall),
                new(HubSection.Network, "Remote access", "domain:RemoteAccess", ProductDomain.RemoteAccess)
            ];
        }

        var present = catalog
            .Where(item => item.Bucket == CatalogBucket.Settings && For(item.ProductDomain) == section)
            .Select(item => item.ProductDomain)
            .Distinct()
            .ToHashSet();
        var order = section == HubSection.Privacy ? PrivacyOrder :
            section == HubSection.Security ? SecurityOrder : NetworkOrder;

        return order
            .Where(present.Contains)
            .Select(domain => new HubNavigationItem(
                section,
                NavigationBuilder.HumanizeDomain(domain),
                $"domain:{domain}",
                domain))
            .ToList()
            .AsReadOnly();
    }

    public static IReadOnlyList<HubNavigationItem> PersistentItems { get; } =
    [
        new(null, "Dashboard", "home", IsPersistent: true),
        new(null, "Conflicts", "conflicts", IsPersistent: true),
        new(null, "Knowledge Explorer", "knowledge", IsPersistent: true),
        new(null, "App Settings", "settings", IsPersistent: true),
        new(null, "About", "about", IsPersistent: true)
    ];
}
