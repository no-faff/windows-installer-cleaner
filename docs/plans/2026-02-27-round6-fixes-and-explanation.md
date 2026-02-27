# Round 6 — Splash timing, exclusion explanation and scan refactor

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.
> Each task ends with a build/test step and a commit. Do not skip verification steps.
> Read CLAUDE.md before starting — British English, sentence case, no Oxford comma.

## Progress (updated by executor)

| Task | Commit | Status |
|------|--------|--------|
| 1 — Fix splash step timing | 20d1d51, 43c03b8 | done |
| 2 — Explain exclusion filters in settings | 5729805 | done |
| 3 — Extract shared scan logic | 6ca0771 | done |
| 4 — Update status.md for rounds 4–5 | fd4d740 | done |

## Author: Opus, 27 February 2026

**Goal:** Fix the splash screen so all steps are visible, explain why Adobe/Acrobat
are in the default exclusion filters, extract duplicated scan logic, and update docs.

**Context:** Round 5 reviewed and approved. The splash screen works but steps 1, 3
and 4 are invisible because the underlying operations complete in under 1ms. The
exclusion filter explanation is too terse — users don't know why Adobe/Acrobat are
there by default.

---

## Task 1: Fix splash step timing so all steps are visible

**Why:** The user only sees steps 2 and 5. Steps 1, 3 and 4 each last a fraction
of a millisecond and are never visible. Step 1 is even worse — the first progress
message from the scan service is `"Querying Windows Installer API..."` which doesn't
match any step trigger, so step 1 is immediately overwritten.

The fix: drive step transitions explicitly with minimum display time per step, rather
than relying on message-pattern matching from service progress reports.

**Files to modify:**

### 1. `src/SimpleWindowsInstallerCleaner/App.xaml.cs`

Replace the entire progress callback and scan invocation (lines 31–65) with an
explicit step-driven approach. The new flow:

```csharp
splash.UpdateStep("Starting up...");

// Step 1: system check
splash.UpdateStep("Step 1/5: Checking system status...");
await Task.Delay(400);

// The scan uses its own internal progress — we don't map it to splash steps.
// Instead, we drive the splash explicitly from App.xaml.cs.
var scanTask = viewModel.ScanWithProgressAsync(null);

// Step 2: product enumeration (the meaty one — runs concurrently with scan)
splash.UpdateStep("Step 2/5: Enumerating installed products...");

// Wait for the scan to complete, but ensure steps 2–5 each get screen time.
await scanTask;

// Steps 3–5 are post-scan. They're instant operations but we give each
// a moment on screen so the user sees progress blazing through.
splash.UpdateStep("Step 3/5: Enumerating patches...");
await Task.Delay(400);

splash.UpdateStep("Step 4/5: Finding installation files...");
await Task.Delay(400);

splash.UpdateStep("Step 5/5: Calculating results...");
await Task.Delay(400);
```

**Key changes from current code:**
- Remove the `stepNumber` variable and the message-pattern-matching `Progress<string>` callback entirely.
- Pass `null` (not a progress callback) to `ScanWithProgressAsync`. The splash
  doesn't need to show individual product names — it just needs to show the steps.
- Steps 1 and 3–5 get an explicit 400ms delay each. Step 2 runs for however long
  the scan takes (typically 1–2 seconds).
- Remove the `Task.WhenAll(scanTask, Task.Delay(2000))` — the explicit delays
  already guarantee ~1.6s minimum on top of the scan time. If you want a minimum
  total, keep a `Task.WhenAll` but with a shorter delay (e.g. 1500ms) alongside
  the scan task only, before steps 3–5.

**Actually, let me be more precise.** Here's the complete replacement for the
try block in `OnStartup`:

```csharp
try
{
    var settingsService = new SettingsService();
    var queryService = new InstallerQueryService();
    var scanService = new FileSystemScanService(queryService);
    var moveService = new MoveFilesService();
    var deleteService = new DeleteFilesService();
    var exclusionService = new ExclusionService();
    var rebootService = new PendingRebootService();
    var msiInfoService = new MsiFileInfoService();

    var viewModel = new MainViewModel(
        scanService, moveService, deleteService,
        exclusionService, settingsService, rebootService, msiInfoService);

    // Show steps with minimum display time per step.
    // Step 1: brief pause so the user sees it
    splash.UpdateStep("Step 1/5: Checking system status...");
    await Task.Delay(400);

    // Step 2: the actual scan (this is where the time is spent)
    splash.UpdateStep("Step 2/5: Enumerating installed products...");
    var scanTask = viewModel.ScanWithProgressAsync(null);
    // Ensure step 2 shows for at least 800ms even on very fast machines
    await Task.WhenAll(scanTask, Task.Delay(800));

    // Steps 3–5: post-scan, blaze through visibly
    splash.UpdateStep("Step 3/5: Enumerating patches...");
    await Task.Delay(400);

    splash.UpdateStep("Step 4/5: Finding installation files...");
    await Task.Delay(400);

    splash.UpdateStep("Step 5/5: Calculating results...");
    await Task.Delay(400);

    var window = new MainWindow(viewModel);
    Application.Current.MainWindow = window;
    window.Show();
    splash.Close();
}
```

**Note:** The `ScanWithProgressAsync(null)` call means no progress messages are
reported back to the splash. This is fine — the splash steps are decorative/reassuring,
not diagnostic. The scan still runs correctly internally.

However: `ScanWithProgressAsync` currently passes progress to the scan service.
If we pass `null`, the reboot check and scan still run fine — `progress?.Report()`
calls are safely no-ops when progress is null.

### 2. `src/SimpleWindowsInstallerCleaner/SplashWindow.xaml`

Change the initial StepText from `"Starting up..."` to `"Step 1/5: Checking system status..."`:

```xml
<TextBlock Grid.Row="4"
           x:Name="StepText"
           Text="Step 1/5: Checking system status..."
           Foreground="#AAAAAA"
           FontSize="12"/>
```

This ensures the very first thing the user sees is step 1, even before the code-behind
runs `UpdateStep`.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `fix: ensure all splash screen steps are visible with minimum display time`

---

## Task 2: Explain exclusion filters in settings window

**Why:** The settings window says *what* the filters do but not *why* Adobe and
Acrobat are there by default. A user who sees those entries might delete them,
thinking they're unnecessary. PatchCleaner explains this clearly — we should too.

**Background (for the executor):** Adobe products have a known issue where their
installer files appear orphaned to enumeration tools when they may still be needed:

- **Acrobat (traditional MSI):** When Adobe builds cumulative patches, the MSI Patch
  Sequencer marks previous `.msp` files as "obsolete" in the database. Those files
  still exist in `C:\Windows\Installer` and look orphaned — but removing them can
  break Acrobat's update and repair chain.
- **Creative Cloud apps:** Use Adobe's proprietary "HyperDrive" installer which wraps
  MSI but doesn't follow standard patching conventions. The standard MSI API calls
  can't reliably assess whether these files are still needed.

PatchCleaner still ships with "Acrobat" in its default exclusion list for exactly
this reason.

**Files to modify:**

### 1. `src/SimpleWindowsInstallerCleaner/SettingsWindow.xaml`

Replace the current description TextBlock (Grid.Row="2") with a more informative
explanation. The current text:

```
Files whose name, author, title, subject or digital signature matches these
terms won't be moved or deleted.
```

Replace with **two** TextBlocks:

```xml
<!-- ── Description ────────────────────────────────────── -->
<StackPanel Grid.Row="2">
    <TextBlock Text="Some products (notably Adobe) use non-standard patch management that makes their installer files appear orphaned when they may still be needed. Removing them can break updates and repairs."
               TextWrapping="Wrap"
               FontSize="12"
               Foreground="#666"
               Margin="0,0,0,6"/>
    <TextBlock Text="Files whose name, author, title, subject or digital signature matches these terms won't be moved or deleted."
               TextWrapping="Wrap"
               FontSize="12"
               Foreground="#888"/>
</StackPanel>
```

**Key points:**
- The **why** comes first (Adobe is buggy), the **how** comes second (text matching).
- "notably Adobe" — specific enough to justify the defaults, general enough that
  users understand the concept applies to other products too.
- No jargon. No mention of "MSI Patch Sequencer" or "HyperDrive". Just plain English.
- The second line is the original text, now in a slightly lighter grey since it's
  secondary information.

### 2. Consider the settings window height

The extra text adds ~2 lines. Check whether the default height (350) still works.
If the filter list area gets too small, increase `Height` from `350` to `380`.
Test by eye — if the list area looks cramped, increase it. Don't increase it if
it's fine.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: explain why Adobe exclusion filters exist in settings window`

---

## Task 3: Extract shared scan logic

**Why:** `ScanAsync()` (lines 111–160) and `ScanWithProgressAsync()` (lines 342–366)
in `MainViewModel.cs` share nearly identical logic for: reboot check, scan call,
filter application, setting all six summary properties, and setting `ScanProgress`
and `HasScanned`. Any future change to the scan flow must be made in two places.

**Files to modify:**

### 1. `src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs`

Extract a private method that both public methods call:

```csharp
private async Task RunScanCoreAsync(IProgress<string>? progress)
{
    if (_settings.CheckPendingReboot)
        HasPendingReboot = _rebootService.HasPendingReboot();

    _lastScanResult = await _scanService.ScanAsync(progress);

    _lastFilteredResult = _exclusionService.ApplyFilters(
        _lastScanResult.OrphanedFiles, _settings.ExclusionFilters, _msiInfoService);

    RegisteredFileCount = _lastScanResult.RegisteredPackages.Count;
    RegisteredSizeDisplay = DisplayHelpers.FormatSize(_lastScanResult.RegisteredTotalBytes);

    ExcludedFileCount = _lastFilteredResult.Excluded.Count;
    ExcludedSizeDisplay = DisplayHelpers.FormatSize(_lastFilteredResult.Excluded.Sum(f => f.SizeBytes));

    OrphanedFileCount = _lastFilteredResult.Actionable.Count;
    OrphanedSizeDisplay = DisplayHelpers.FormatSize(_lastFilteredResult.Actionable.Sum(f => f.SizeBytes));

    HasScanned = true;
}
```

Then **rewrite `ScanAsync`** to call it:

```csharp
[RelayCommand]
private async Task ScanAsync()
{
    ScanProgress = "Starting scan...";
    var sw = Stopwatch.StartNew();

    try
    {
        var progress = new Progress<string>(msg => ScanProgress = msg);
        var scanTask = RunScanCoreAsync(progress);
        if (await Task.WhenAny(scanTask, Task.Delay(200)) != scanTask)
            IsScanning = true;
        await scanTask;

        sw.Stop();
        ScanProgress = $"Scan complete ({sw.Elapsed.TotalSeconds:F1}s)";
    }
    catch (UnauthorizedAccessException)
    {
        MessageBox.Show(
            "This app requires administrator privileges.\n\nPlease right-click and choose 'Run as administrator'.",
            "Administrator rights required",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        ScanProgress = "Access denied — run as administrator.";
    }
    catch (Exception ex)
    {
        ScanProgress = $"Scan failed: {ex.Message}";
    }
    finally
    {
        IsScanning = false;
    }
}
```

And **rewrite `ScanWithProgressAsync`** to call it:

```csharp
public async Task ScanWithProgressAsync(IProgress<string>? progress)
{
    var sw = Stopwatch.StartNew();
    await RunScanCoreAsync(progress);
    sw.Stop();
    ScanProgress = $"Scan complete ({sw.Elapsed.TotalSeconds:F1}s)";
}
```

**Important:** Make sure `RunScanCoreAsync` does NOT set `ScanProgress` — that's
the caller's job (ScanAsync shows intermediate messages, ScanWithProgressAsync
only shows the final time).

**Verify:** `dotnet test src/SimpleWindowsInstallerCleaner.Tests` (all 22 tests
should still pass) then `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `refactor: extract shared scan logic into RunScanCoreAsync`

---

## Task 4: Update status.md for rounds 4–5

**Why:** `docs/status.md` still says "Three rounds of development and review are
complete." It needs updating to reflect rounds 4 and 5.

**Files to modify:**

### 1. `docs/status.md`

Read the current file and update:

- Change "Three rounds" to "Five rounds" in the intro.
- Add a summary of rounds 4 and 5 to the "What's done" section:
  - **Round 4:** About dialog, auto-select first item, exclusion row visibility,
    scan overlay suppression, scan duration display, version in title bar.
  - **Round 5:** Keyboard/mouse fixes in orphaned files window, full digital
    signatures, scrollable detail panels, inline filter names on excluded row,
    startup splash screen with 5-step progress, UAC verification.
  - **Post-round 5 fixes:** Dialog owner lifecycle fix, splash minimum display time.
- Update "What's next" to mention Round 6 (this round) and the planned UI redesign.

**Verify:** No build needed — documentation only.

**Commit:** `docs: update status.md for rounds 4 and 5`

---

## Summary of changes

| Task | Type | What |
|------|------|------|
| 1 | Bug fix | Ensure all 5 splash steps are visible with minimum display times |
| 2 | UX | Explain why Adobe exclusion filters exist, in plain English |
| 3 | Refactor | Extract shared scan logic to eliminate duplication |
| 4 | Docs | Update status.md for rounds 4–5 |

---

## Execution notes for Sonnet

1. **Do tasks in order.** Task 3 depends on understanding the flow from task 1.
2. **Build and test after every task.** The plan says when to verify — do it.
3. **Commit after every task.** Small, focused commits.
4. **Don't add features not in this plan.** No extras, no "improvements."
5. **British English in all user-facing strings.** Sentence case. No Oxford comma.
6. **Task 1 is the trickiest.** The key insight is: stop trying to map service
   progress messages to splash steps. Drive the steps explicitly from App.xaml.cs
   with fixed delays. The scan runs during step 2 — everything else is decorative.
7. **Task 2 is just text.** Don't overthink it. Two paragraphs, plain English.
8. **Task 3 is a straightforward extract-method refactor.** The tests should not
   need any changes — the public API doesn't change.

---

## Round 7 outline (for reference — not part of this round)

Round 7 is the UI redesign. High-level direction agreed with the user:

- **System theme:** Follow Windows light/dark setting. Light as the primary design
  target (airy, clean, lots of white space). Dark as a well-considered alternative.
- **Design feel:** "Child-like simple, cartoonish." Clean cards with rounded corners,
  generous padding, soft shadows. Think Audin Rushow dashboard aesthetic.
- **SIMPLICITY IS ABSOLUTELY KEY.** The redesign must make the app simpler to
  understand, not more complex. Beautiful ≠ complicated. No faff.
- **Custom window chrome** or borderless with custom title bar — the single biggest
  "pro vs amateur" signal.
- **Splash screen:** The "whizz-bang moment." Should feel confident and purposeful.
  Reference: Mekari/Febriansyah Nursan splash (colourful, branded, alive).
- **Move/delete operations:** Proper progress dialog with percentage, file count.
  Reference: Naveen Yellamelli Windows 11 file transfer concept.
- **Window sizes:** Can be larger if more white space makes it better.
- **Design references:** `docs/dribble-windows-apps/` folder.

Opus will write the detailed Round 7 plan after Round 6 is complete and reviewed.
