using System.ServiceProcess;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Services;

public enum ServiceControlAction
{
    Start,
    Stop,
    Restart
}

/// <summary>Small typed service-control surface; it cannot change startup configuration.</summary>
public sealed class ServiceControlService
{
    public bool TryChange(ServiceInfo service, ServiceControlAction action, bool administratorAuthorized,
        bool confirmed, out string error)
    {
        error = string.Empty;
        if (!administratorAuthorized) { error = "Administrator mode is required."; return false; }
        if (!confirmed) { error = "The service action was not confirmed."; return false; }
        if (!ServiceMutationPolicy.CanMutate(service, out error)) return false;

        try
        {
            using var controller = new ServiceController(service.Name);
            controller.Refresh();
            switch (action)
            {
                case ServiceControlAction.Start:
                    if (controller.Status != ServiceControllerStatus.Running) controller.Start();
                    controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
                    break;
                case ServiceControlAction.Stop:
                    if (!controller.CanStop) { error = "Windows reports that this service cannot be stopped."; return false; }
                    if (controller.Status != ServiceControllerStatus.Stopped) controller.Stop();
                    controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
                    break;
                case ServiceControlAction.Restart:
                    if (!controller.CanStop) { error = "Windows reports that this service cannot be restarted."; return false; }
                    if (controller.Status != ServiceControllerStatus.Stopped)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
                    }
                    controller.Start();
                    controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
                    break;
            }
            return true;
        }
        catch (Exception ex)
        {
            error = "Windows rejected the service action (" + ex.GetType().Name + ").";
            return false;
        }
    }
}
