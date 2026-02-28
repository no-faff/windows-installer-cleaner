using Microsoft.Win32;

namespace SimpleWindowsInstallerCleaner.Services;

public sealed class PendingRebootService : IPendingRebootService
{
    public bool HasPendingReboot()
    {
        try
        {
            // Windows Update reboot required
            using var wuKey = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
            if (wuKey is not null)
                return true;

            // Component Based Servicing reboot pending
            using var cbsKey = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
            if (cbsKey is not null)
                return true;

            return false;
        }
        catch
        {
            return false; // fail open — don't block the user
        }
    }
}
