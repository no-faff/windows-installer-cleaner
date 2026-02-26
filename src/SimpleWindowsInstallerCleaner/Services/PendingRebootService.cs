using Microsoft.Win32;

namespace SimpleWindowsInstallerCleaner.Services;

public sealed class PendingRebootService : IPendingRebootService
{
    public bool HasPendingReboot()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Session Manager");

            if (key is null)
                return false;

            var value = key.GetValue("PendingFileRenameOperations");
            if (value is string[] ops)
                return ops.Length > 0;

            return value is not null;
        }
        catch
        {
            return false; // fail open — don't block the user
        }
    }
}
