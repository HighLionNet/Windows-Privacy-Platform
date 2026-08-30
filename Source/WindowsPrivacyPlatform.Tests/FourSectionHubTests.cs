using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Scanner;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public sealed class FourSectionHubTests
{
    [Fact]
    public void Every_product_domain_maps_to_exactly_one_of_the_four_sections()
    {
        var mappings = Enum.GetValues<ProductDomain>()
            .Select(domain => (Domain: domain, Section: HubNavigation.For(domain)))
            .ToList();

        Assert.Equal(Enum.GetValues<ProductDomain>().Length, mappings.Count);
        Assert.Equal(Enum.GetValues<HubSection>().OrderBy(value => value),
            mappings.Select(mapping => mapping.Section).Distinct().OrderBy(value => value));
    }

    [Fact]
    public void Firewall_is_network_and_windows_update_is_security()
    {
        Assert.Equal(HubSection.Network, HubNavigation.For(ProductDomain.Firewall));
        Assert.Equal(HubSection.Security, HubNavigation.For(ProductDomain.WindowsUpdate));
    }

    [Fact]
    public void Explore_destinations_are_partitioned_and_persistent_items_are_not_section_categories()
    {
        var explore = HubNavigation.CategoriesFor(HubSection.Explore, ManagedObjectCatalog.All);
        Assert.NotEmpty(explore);
        Assert.All(explore, item =>
        {
            Assert.Equal(HubSection.Explore, item.Section);
            Assert.False(item.IsPersistent);
        });
        Assert.Contains(explore, item => item.Tag == "explore:other-services" && !item.IsReadOnlyDestination);
        Assert.Contains(explore, item => item.Tag == "explore:other-tasks" && !item.IsReadOnlyDestination);
        Assert.All(explore.Where(item => item.Tag is not ("explore:other-services" or "explore:other-tasks")),
            item => Assert.True(item.IsReadOnlyDestination));

        Assert.All(HubNavigation.PersistentItems, item =>
        {
            Assert.Null(item.Section);
            Assert.True(item.IsPersistent);
        });
    }

    [Fact]
    public void Dynamic_inventory_routes_to_explore_without_a_write_target()
    {
        var item = new ManagedObject
        {
            ProductDomain = ProductDomain.Firewall,
            Bucket = CatalogBucket.SystemInventory,
            IsDynamicInventory = true
        };

        Assert.Equal(HubSection.Explore, HubNavigation.For(item));
        Assert.Null(item.WritableTarget);
        Assert.False(item.IsWritable);
    }

    [Fact]
    public void Legacy_clipboard_settings_are_finalized_into_the_privacy_section()
    {
        var clipboard = ManagedObjectCatalog.All
            .Where(item => item.ObjectId.Contains("clipboard", StringComparison.OrdinalIgnoreCase) &&
                           item.Bucket == CatalogBucket.Settings)
            .ToList();

        Assert.NotEmpty(clipboard);
        Assert.All(clipboard, item =>
        {
            Assert.Equal(ProductDomain.Clipboard, item.ProductDomain);
            Assert.Equal(HubSection.Privacy, HubNavigation.For(item));
        });
        Assert.DoesNotContain(ManagedObjectCatalog.All,
            item => item.Bucket == CatalogBucket.Settings && item.ProductDomain == ProductDomain.Other);
    }

    [Fact]
    public void Protection_product_copy_preserves_observation_states()
    {
        Assert.Equal("not observed", ProtectionProductPresentation.Summary(new SecurityInventory()));
        Assert.Equal("access denied", ProtectionProductPresentation.Summary(new SecurityInventory
        {
            ProtectionProductStatus = ProtectionProductObservationStatus.AccessDenied
        }));

        var observed = new SecurityInventory
        {
            ProtectionProductStatus = ProtectionProductObservationStatus.Observed,
            ProtectionProducts =
            [
                new ProtectionProductInfo
                {
                    DisplayName = "Microsoft Defender Antivirus",
                    IsMicrosoftDefender = true,
                    IsActive = true
                },
                new ProtectionProductInfo { DisplayName = "Vendor X", IsActive = true }
            ]
        };
        Assert.Equal("Defender active · Vendor X reported", ProtectionProductPresentation.Summary(observed));
        Assert.True(SecurityCenterCollector.IsProductActive(0x001000));
        Assert.False(SecurityCenterCollector.IsProductActive(0x000000));
    }

}
