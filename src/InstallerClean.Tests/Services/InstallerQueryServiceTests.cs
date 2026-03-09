using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Tests for InstallerQueryService. The service calls the Windows Installer
/// API (MsiEnumProductsEx etc.) which requires admin elevation for the
/// "all users" SID. Tests here exercise the cancellation path and the
/// non-elevated error path. The data transformation logic (patch state
/// classification, removability rules, path deduplication) is tested via
/// RegisteredPackageTests and FileSystemScanServiceTests.
/// </summary>
public class InstallerQueryServiceTests
{
    [Fact]
    public void Implements_IInstallerQueryService()
    {
        var svc = new InstallerQueryService();

        Assert.IsAssignableFrom<IInstallerQueryService>(svc);
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_cancellation_before_start_throws()
    {
        var svc = new InstallerQueryService();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.GetRegisteredPackagesAsync(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_cancellation_token_none_does_not_throw_cancellation()
    {
        var svc = new InstallerQueryService();

        // Without admin, we expect UnauthorizedAccessException, not OperationCanceledException.
        // This confirms the default cancellation token does not interfere.
        var ex = await Record.ExceptionAsync(
            () => svc.GetRegisteredPackagesAsync(cancellationToken: CancellationToken.None));

        if (ex is not null)
            Assert.IsNotType<OperationCanceledException>(ex);
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_without_elevation_throws_unauthorized()
    {
        // Non-elevated processes get AccessDenied from MsiEnumProductsEx
        // with the all-users SID. This is the expected behaviour.
        var svc = new InstallerQueryService();

        var ex = await Record.ExceptionAsync(() => svc.GetRegisteredPackagesAsync());

        // If running elevated (e.g. in CI with admin), the call succeeds.
        // If not elevated, it throws UnauthorizedAccessException.
        if (ex is not null)
            Assert.IsType<UnauthorizedAccessException>(ex);
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_progress_receives_initial_message_before_failure()
    {
        var svc = new InstallerQueryService();
        var messages = new List<string>();
        var progress = new SyncProgress<string>(m => messages.Add(m));

        // The service reports "Enumerating installed products..." before
        // calling the API. Even if the API fails, we should see that message.
        try
        {
            await svc.GetRegisteredPackagesAsync(progress);
        }
        catch (UnauthorizedAccessException)
        {
            // Expected when not elevated
        }

        Assert.Contains(messages, m => m.Contains("Enumerating installed products"));
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_null_progress_does_not_throw()
    {
        var svc = new InstallerQueryService();

        // Passing null progress should not cause a NullReferenceException.
        // It may throw UnauthorizedAccessException from the API, which is fine.
        var ex = await Record.ExceptionAsync(
            () => svc.GetRegisteredPackagesAsync(progress: null));

        if (ex is not null)
        {
            Assert.IsNotType<NullReferenceException>(ex);
        }
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_returns_readonly_list_when_elevated()
    {
        var svc = new InstallerQueryService();

        try
        {
            var packages = await svc.GetRegisteredPackagesAsync();

            Assert.IsAssignableFrom<IReadOnlyList<RegisteredPackage>>(packages);
            Assert.NotNull(packages);
        }
        catch (UnauthorizedAccessException)
        {
            // Not elevated - the API path assertion cannot be tested.
            // The non-elevated behaviour is covered by other tests.
        }
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_all_paths_non_empty_when_elevated()
    {
        var svc = new InstallerQueryService();

        try
        {
            var packages = await svc.GetRegisteredPackagesAsync();

            Assert.All(packages, p =>
                Assert.False(string.IsNullOrWhiteSpace(p.LocalPackagePath)));
        }
        catch (UnauthorizedAccessException)
        {
            // Not elevated - skip.
        }
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_paths_unique_case_insensitive_when_elevated()
    {
        var svc = new InstallerQueryService();

        try
        {
            var packages = await svc.GetRegisteredPackagesAsync();

            var uniquePaths = new HashSet<string>(
                packages.Select(p => p.LocalPackagePath),
                StringComparer.OrdinalIgnoreCase);

            Assert.Equal(packages.Count, uniquePaths.Count);
        }
        catch (UnauthorizedAccessException)
        {
            // Not elevated - skip.
        }
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_removable_only_when_superseded_when_elevated()
    {
        var svc = new InstallerQueryService();

        try
        {
            var packages = await svc.GetRegisteredPackagesAsync();

            foreach (var pkg in packages.Where(p => p.IsRemovable))
            {
                Assert.True(pkg.PatchState is 2 or 4,
                    $"IsRemovable=true but PatchState={pkg.PatchState}");
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Not elevated - skip.
        }
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_scan_complete_has_count_when_elevated()
    {
        var svc = new InstallerQueryService();
        var messages = new List<string>();
        var progress = new SyncProgress<string>(m => messages.Add(m));

        try
        {
            var packages = await svc.GetRegisteredPackagesAsync(progress);

            var completionMsg = messages.Last();
            Assert.Contains($"{packages.Count} registered package(s) found", completionMsg);
        }
        catch (UnauthorizedAccessException)
        {
            // Not elevated - skip.
        }
    }
}
