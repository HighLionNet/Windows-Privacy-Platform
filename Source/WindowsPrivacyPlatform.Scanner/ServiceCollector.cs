// Source/WindowsPrivacyPlatform.Scanner/ServiceCollector.cs
using System;
using System.ServiceProcess;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for Windows services.
    /// Uses ServiceController.GetServices() — no elevation, no writes.
    /// </summary>
    public sealed class ServiceCollector : IInventoryCollector
    {
        public string Name => "ServiceCollector";

        public void Collect(InventorySnapshot snapshot)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            try
            {
                var services = ServiceController.GetServices();
                foreach (var svc in services)
                {
                    try
                    {
                        snapshot.Services.Add(new ServiceInfo
                        {
                            Name = svc.ServiceName ?? string.Empty,
                            StartMode = svc.StartType.ToString(),
                            State = svc.Status.ToString()
                        });
                    }
                    catch
                    {
                        // Individual service may be inaccessible; skip it.
                    }
                    finally
                    {
                        svc.Dispose();
                    }
                }
            }
            catch
            {
                // Collection failure must not abort the overall scan.
            }
        }
    }
}
