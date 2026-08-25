using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Scanner;
using WindowsPrivacyPlatform.Scanner.Binding;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public class V21CollectorAndBinderTests
{
    [Fact]
    public void PolicyCollector_emits_v2_1_probe_anchors_without_writing()
    {
        var snapshot = new InventorySnapshot();

        new PolicyCollector().Collect(snapshot);

        var ids = snapshot.PolicySettings.Select(setting => setting.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("policy.recall.disableaidataanalysis", ids);
        Assert.Contains("policy.asr.lsasscredentialtheft", ids);
        Assert.Contains("policy.hello.cloudtrust", ids);
        Assert.Contains("policy.storage.senseglobal", ids);
        Assert.Contains("policy.accessibility.disablesettingssync", ids);
        Assert.Contains("policy.network.dohmode", ids);
        Assert.Contains("network.wifi.randommac", ids);

        var catalogIds = ManagedObjectCatalog.All.Select(item => item.ObjectId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(ids, id => Assert.Contains(id, catalogIds));
    }

    [Fact]
    public void LocalSecurityPolicyCollector_is_best_effort_and_whitelisted()
    {
        var snapshot = new InventorySnapshot();

        new LocalSecurityPolicyCollector().Collect(snapshot);

        var allowed = ManagedObjectCatalog.All
            .Where(item => item.ObjectId.StartsWith("policy.security.", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.ObjectId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(snapshot.PolicySettings, setting => Assert.Contains(setting.Name, allowed));
    }

    [Fact]
    public void InventoryAnchorBinder_matches_curated_inventory_without_mutation()
    {
        var snapshot = new InventorySnapshot
        {
            Services = [new ServiceInfo { Name = "cbdhsvc_42a", State = "Running", StartMode = "Manual" }],
            ScheduledTasks = [new TaskInfo { Path = @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator", State = "Ready" }],
            InstalledPackages = ["MicrosoftWindows.Client.WebExperience_8wekyb3d8bbwe"],
            InstalledCapabilities = ["Hello.Face.20134~~~~0.0.1.0"]
        };
        var originals = new
        {
            ServiceCount = snapshot.Services.Count,
            TaskCount = snapshot.ScheduledTasks.Count,
            PackageCount = snapshot.InstalledPackages.Count,
            CapabilityCount = snapshot.InstalledCapabilities.Count
        };
        var binder = new InventoryAnchorBinder();
        var anchors = ManagedObjectCatalog.All.Where(item => item.ObjectId is
            "service.clipboarduser" or "task.ceip.consolidator" or
            "package.widgets.webexperience" or "capability.hello.face").ToList();

        Assert.Equal(4, anchors.Count);
        foreach (var anchor in anchors)
            binder.Bind(snapshot, anchor);

        Assert.Contains("cbdhsvc_42a: Running", anchors.Single(item => item.ObjectId == "service.clipboarduser").CurrentState);
        Assert.Equal("Present: Ready", anchors.Single(item => item.ObjectId == "task.ceip.consolidator").CurrentState);
        Assert.StartsWith("Installed:", anchors.Single(item => item.ObjectId == "package.widgets.webexperience").CurrentState);
        Assert.StartsWith("Installed:", anchors.Single(item => item.ObjectId == "capability.hello.face").CurrentState);
        Assert.Equal(originals.ServiceCount, snapshot.Services.Count);
        Assert.Equal(originals.TaskCount, snapshot.ScheduledTasks.Count);
        Assert.Equal(originals.PackageCount, snapshot.InstalledPackages.Count);
        Assert.Equal(originals.CapabilityCount, snapshot.InstalledCapabilities.Count);
    }
}
