using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

public class PendingRebootServiceTests
{
    [Fact]
    public void HasPendingReboot_returns_bool_without_throwing()
    {
        var svc = new PendingRebootService();

        // Should not throw regardless of elevation or registry state.
        // The result depends on the machine state, so we just check it returns.
        var result = svc.HasPendingReboot();

        Assert.IsType<bool>(result);
    }

    [Fact]
    public void HasPendingReboot_returns_consistent_result_on_repeated_calls()
    {
        var svc = new PendingRebootService();

        var first = svc.HasPendingReboot();
        var second = svc.HasPendingReboot();

        // Registry state shouldn't change between two immediate calls
        Assert.Equal(first, second);
    }

    [Fact]
    public void HasPendingReboot_separate_instances_agree()
    {
        var svc1 = new PendingRebootService();
        var svc2 = new PendingRebootService();

        // Two independent instances should read the same registry state
        Assert.Equal(svc1.HasPendingReboot(), svc2.HasPendingReboot());
    }

    [Fact]
    public void Implements_IPendingRebootService()
    {
        var svc = new PendingRebootService();

        Assert.IsAssignableFrom<IPendingRebootService>(svc);
    }

    [Fact]
    public void HasPendingReboot_can_be_called_from_non_elevated_context()
    {
        // The service catches exceptions and returns false when registry
        // access fails. This test ensures it doesn't throw even if
        // the keys are protected.
        var svc = new PendingRebootService();

        var exception = Record.Exception(() => svc.HasPendingReboot());

        Assert.Null(exception);
    }
}
