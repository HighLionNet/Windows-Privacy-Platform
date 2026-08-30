using System.Xml.Linq;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public sealed class V262AcceptanceTests
{
    [Fact]
    public void Version_comes_from_DirectoryBuildProps()
    {
        var path = FindRepositoryFile("Directory.Build.props");
        var version = XDocument.Load(path).Descendants("Version").Single().Value;
        Assert.Equal("2.6.2", version);
        Assert.Equal(version, ManagedObjectCatalog.CatalogVersion);
    }

    [Fact]
    public void Hub_is_persistent_and_network_and_explore_are_partitioned_without_a_fifth_section()
    {
        Assert.All(HubNavigation.PersistentItems, item => { Assert.Null(item.Section); Assert.True(item.IsPersistent); });
        Assert.Equal(4, Enum.GetValues<HubSection>().Length);
        Assert.Equal(4, HubNavigation.CategoriesFor(HubSection.Network, ManagedObjectCatalog.All).Count);
        Assert.Equal(8, HubNavigation.CategoriesFor(HubSection.Explore, ManagedObjectCatalog.All).Count);
        Assert.All(Enum.GetValues<ProductDomain>(), domain => Assert.Contains(HubNavigation.For(domain), Enum.GetValues<HubSection>()));
    }

    [Fact]
    public void Dynamic_inventory_never_has_a_writable_target_and_policy_changes_stay_registry_only()
    {
        var snapshot = new InventorySnapshot
        {
            Services = [new ServiceInfo { Name = "VendorService", DisplayName = "Vendor Service", State = "Running" }],
            ScheduledTasks = [new TaskInfo { Path = @"\Foo\Bar", State = "Ready" }]
        };
        var dynamic = DynamicInventoryCatalog.Create(snapshot, ManagedObjectCatalog.All);
        Assert.NotEmpty(dynamic);
        Assert.All(dynamic, item => { Assert.Null(item.WritableTarget); Assert.False(item.IsWritable); });
        Assert.False(PolicyChangeService.IsSupportedTarget(new WritableTarget
        {
            Kind = WritableTargetKind.Service,
            Identifier = "VendorService",
            SupportedRawValues = ["Running"]
        }));
    }

    [Fact]
    public void Service_and_task_policies_deny_protected_rows_and_allow_verified_optional_rows()
    {
        Assert.False(ServiceMutationPolicy.CanMutate(new ServiceInfo { Name = "RpcSs", IsMicrosoft = false }, out _));
        Assert.False(ServiceMutationPolicy.CanMutate(new ServiceInfo { Name = "Vendor", IsMicrosoft = null }, out _));
        Assert.False(ServiceMutationPolicy.CanMutate(new ServiceInfo { Name = "Vendor", IsMicrosoft = false, CommandLine = "svchost.exe -k shared" }, out _));

        var microsoft = new TaskInfo { Path = @"\Microsoft\Windows\Defender\Scan", State = "Ready" };
        var other = new TaskInfo { Path = @"\Foo\Bar", State = "Ready" };
        TaskInfo[] snapshot = [microsoft, other];
        Assert.False(TaskMutationPolicy.CanMutate(microsoft, snapshot, out _));
        Assert.True(TaskMutationPolicy.CanMutate(other, snapshot, out _));
        Assert.False(TaskMutationPolicy.CanMutate(new TaskInfo { Path = @"\Foo\..\Bar" }, snapshot, out _));
    }

    [Fact]
    public void Dns_evidence_keeps_absence_failure_and_external_apps_distinct()
    {
        var dns = new DnsResolutionSnapshot
        {
            Nrpt = new DnsLayerObservation { Evidence = EvidenceState.NotConfigured, Summary = "No NRPT rules are configured." },
            ResolverProbes = [new DnsProbeInfo { Resolver = "192.0.2.53", Evidence = EvidenceState.Error, Answer = "No verified answer" }],
            ExternalApps = [new ExternalDnsInfo { Application = "Microsoft Edge", Evidence = EvidenceState.Unknown, Source = "ExternalApp" }]
        };
        Assert.Equal(EvidenceState.NotConfigured, dns.Nrpt.Evidence);
        Assert.Equal(EvidenceState.Error, dns.ResolverProbes.Single().Evidence);
        Assert.NotEqual(EvidenceState.NotConfigured, dns.ResolverProbes.Single().Evidence);
        Assert.Equal("ExternalApp", dns.ExternalApps.Single().Source);
    }

    [Fact]
    public void Unsigned_hash_change_is_status_only_but_signed_builds_fail_closed()
    {
        Assert.True(BinaryIntegrityGuard.EvaluatePolicy(false, false, false, hashMatches: false));
        Assert.True(BinaryIntegrityGuard.EvaluatePolicy(false, false, false, hashMatches: true));
        Assert.False(BinaryIntegrityGuard.EvaluatePolicy(true, false, true, hashMatches: true));
        Assert.False(BinaryIntegrityGuard.EvaluatePolicy(true, true, false, hashMatches: true));
        Assert.True(BinaryIntegrityGuard.EvaluatePolicy(true, true, true, hashMatches: false));
    }

    [Fact]
    public void Inventory_actions_require_a_completed_recent_and_sane_snapshot_timestamp()
    {
        var now = new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        var result = new ScanResult
        {
            Status = ScanStatus.CompletedWithWarnings,
            Snapshot = new InventorySnapshot { CaptureTimestamp = now.AddMinutes(-29) }
        };
        Assert.True(InventoryChangeService.IsFreshSnapshot(result, now, out _));

        result.Snapshot.CaptureTimestamp = now.AddMinutes(-31);
        Assert.False(InventoryChangeService.IsFreshSnapshot(result, now, out _));
        result.Snapshot.CaptureTimestamp = now.AddMinutes(6);
        Assert.False(InventoryChangeService.IsFreshSnapshot(result, now, out _));
        result.Status = ScanStatus.Failed;
        result.Snapshot.CaptureTimestamp = now;
        Assert.False(InventoryChangeService.IsFreshSnapshot(result, now, out _));
    }

    [Fact]
    public void Consent_outcome_grouping_uses_the_conflict_pairs_without_duplicate_ids()
    {
        var pair = OutcomeConflictEngine.ConsentFamilies.First();
        var items = ManagedObjectCatalog.All.Where(item =>
            item.ObjectId.Equals(pair.UserId, StringComparison.OrdinalIgnoreCase) ||
            item.ObjectId.Equals(pair.PolicyId, StringComparison.OrdinalIgnoreCase)).ToList();
        var groups = OutcomeGrouping.Build(items);
        var family = Assert.Single(groups, group => group.Family == pair.Family);
        Assert.Equal(2, family.ObjectIds.Count);
        Assert.Equal(family.ObjectIds.Count, family.ObjectIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Edge_cage_entries_are_documented_typed_and_authorized()
    {
        var ids = new[]
        {
            "policy.edge.hidefirstrun", "policy.edge.hubssidebar", "policy.edge.shoppingassistant",
            "policy.edge.diagnosticdata", "policy.edge.urldiagnosticdata", "policy.edge.userfeedback"
        };
        foreach (var id in ids)
        {
            var item = ManagedObjectCatalog.All.Single(candidate => candidate.ObjectId == id);
            Assert.NotEmpty(item.References ?? []);
            Assert.Equal(WritableTargetKind.Registry, item.WritableTarget?.Kind);
            Assert.Equal(RegistryValueKindExpected.DWord, item.WritableTarget?.ValueKind);
            Assert.True(ManagedObjectCatalog.IsAuthorizedWriteTarget(item));
        }
    }

    private static string FindRepositoryFile(string name)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, name);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(name);
    }
}
