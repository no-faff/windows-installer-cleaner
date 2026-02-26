# Round 4 — UX polish from first real-world test

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.
> Each task ends with a build/test step and a commit. Do not skip verification steps.
> Read CLAUDE.md before starting — British English, sentence case, no Oxford comma.

## Progress (updated by executor)

| Task | Commit | Status |
|------|--------|--------|
| 1 — Fix About dialog text | a55ef87 | ✓ done |
| 2 — Auto-select first item in detail windows | 6544db5 | ✓ done |
| 3 — Explain why files are excluded | d3886a6 | ⚠ partial — Opus to redesign (see note below) |
| 4 — Hide "excluded by filters" row when count is 0 | d1ec986 | ✓ done |
| 5 — Hide scanning overlay once scan completes | d76076b | ✓ done |
| 6 — Show scan duration | 8fc7d71 | ✓ done |
| 7 — Require admin elevation via manifest | n/a | ✓ done (already in place) |
| 8 — Add version number to title bar | ca8ad9e | ✓ done |

## Author: Opus, 27 February 2026

**Goal:** Address UX issues found during the first real-world test of the app, plus improvements identified from comparing against PatchCleaner.

**Context:** Rounds 1–3 complete. The app works correctly — scan is fast, finds the right orphans. These are polish and usability improvements.

---

## Task 1: Fix About dialog text

**Why:** The About dialog currently reads:
```
Simple Windows installer cleaner v1.0.0

Part of the No faff suite

MIT licence · github.com/no-faff
```

The `github.com/no-faff` text is just a string in a MessageBox — it's not clickable and looks garbled next to the `·` character. Replace with a proper WPF dialog that has a clickable link.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs`** — replace `ShowAbout()` method. Instead of `MessageBox.Show`, create and show an `AboutWindow`:

   ```csharp
   [RelayCommand]
   private void ShowAbout()
   {
       var window = new AboutWindow
       {
           Owner = Application.Current.MainWindow
       };
       window.ShowDialog();
   }
   ```

2. **Create `src/SimpleWindowsInstallerCleaner/AboutWindow.xaml`** — a small dialog (400×200, non-resizable, CenterOwner) with:
   - App name: "Simple Windows installer cleaner" (sentence case, bold, 16pt)
   - Version: "Version 0.1.0-alpha" (12pt, grey) — note: pre-release, not 1.0.0
   - "Part of the No faff suite" (12pt, grey)
   - "MIT licence" (12pt, grey)
   - A clickable hyperlink to `https://github.com/no-faff/windows-installer-cleaner` using a WPF `Hyperlink` inside a `TextBlock`
   - A Close button (IsCancel=True)

3. **Create `src/SimpleWindowsInstallerCleaner/AboutWindow.xaml.cs`** — minimal code-behind with a `Hyperlink_Click` handler that opens the URL in the default browser using `Process.Start`.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: replace About MessageBox with proper dialog window`

---

## Task 2: Auto-select first item in detail windows

**Why:** PatchCleaner auto-selects the first entry so details are immediately visible. Ours shows "Select a file to view details." until the user clicks. Auto-selecting the first item is a better default.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/ViewModels/OrphanedFilesViewModel.cs`** — at the end of the constructor, after sorting, auto-select the first actionable file:

   ```csharp
   if (ActionableFiles.Count > 0)
       SelectedFile = ActionableFiles[0];
   ```

2. **`src/SimpleWindowsInstallerCleaner/ViewModels/RegisteredFilesViewModel.cs`** — at the end of the constructor, after building the product list:

   ```csharp
   if (Products.Count > 0)
       SelectedProduct = Products[0];
   ```

**Note:** The `OnSelectedFileChanged` / `OnSelectedProductChanged` handlers will fire and load metadata. This is fine — the cache will warm up for the first item.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: auto-select first item in orphaned and registered detail windows`

---

## Task 3: Explain why files are excluded

**Why:** The main window says "X files excluded by filters" but gives no indication of WHY (Adobe safety). A user unfamiliar with the app won't understand. The "Excluded by filters" section in the orphaned files window also has no explanation.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/MainWindow.xaml`** — add a tooltip to the "excluded by filters" row. On the StackPanel containing "● X files excluded by filters", add:

   ```xml
   ToolTip="Excluded files match your exclusion filters (e.g. Adobe). Edit filters in Settings."
   ```

   Also add a "details..." link button to the excluded row (same as the other two rows have), bound to `OpenOrphanedDetailsCommand` — the orphaned details window already shows both actionable and excluded files.

2. **`src/SimpleWindowsInstallerCleaner/OrphanedFilesWindow.xaml`** — add a subtitle under "Excluded by filters" explaining:

   ```xml
   <TextBlock Text="These files match your exclusion filters and won't be moved or deleted."
              FontSize="11"
              Foreground="#AAA"
              Margin="0,0,0,4"/>
   ```

   Place this inside the excluded section StackPanel, after the "Excluded by filters" header border and before the excluded ListBox.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: explain exclusion filters to user with tooltips and descriptions`

---

## Task 4: Hide "excluded by filters" row when count is 0

**Why:** The main window always shows "0 files excluded by filters" even when there are no exclusion matches. This is noise for users who haven't configured filters or whose filters don't match anything. Hide it when the count is 0.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs`** — add a computed property:

   ```csharp
   public bool HasExcludedFiles => ExcludedFileCount > 0;
   ```

   Add `[NotifyPropertyChangedFor(nameof(HasExcludedFiles))]` to the `_excludedFileCount` field.

2. **`src/SimpleWindowsInstallerCleaner/MainWindow.xaml`** — wrap the "excluded by filters" DockPanel in a visibility binding:

   ```xml
   <DockPanel Margin="0,0,0,7"
              Visibility="{Binding HasExcludedFiles, Converter={StaticResource BoolToVis}}">
   ```

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `fix: hide excluded row when no files match exclusion filters`

---

## Task 5: Hide scanning overlay once scan completes

**Why:** The scanning overlay (dark translucent overlay with progress bar) shows during the scan, which is great. But if the scan is nearly instant (as it now is after the performance fix), the overlay flashes briefly and looks janky. After scan completes, the overlay should disappear cleanly.

Check that `IsScanning` is properly set to `false` in the `finally` block. This is already the case (line 142), but verify the overlay actually disappears. If it does (it should), this task is a no-op — mark as done with no commit needed.

However, if there's a visual flash, consider setting `IsScanning = true` only after a short delay (e.g. 200ms) so sub-200ms scans never show the overlay at all. Use a simple approach:

```csharp
var scanTask = _scanService.ScanAsync(progress);
var delayTask = Task.Delay(200);
if (await Task.WhenAny(scanTask, delayTask) == delayTask)
    IsScanning = true;
_lastScanResult = await scanTask;
```

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `fix: suppress scanning overlay for fast scans` (or skip if not needed)

---

## Task 6: Show scan duration

**Why:** Users want confidence the scan completed properly. Showing "Scan complete (1.2s)" in the bottom bar or as the progress text gives reassurance, especially since the scan is now very fast.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs`** — in `ScanAsync()`:

   - Add a `Stopwatch` at the start:
     ```csharp
     var sw = System.Diagnostics.Stopwatch.StartNew();
     ```
   - After the scan and filter logic completes (before `HasScanned = true`), stop and format:
     ```csharp
     sw.Stop();
     ScanProgress = $"Scan complete ({sw.Elapsed.TotalSeconds:F1}s)";
     ```

   Add `using System.Diagnostics;` at the top if not already present.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: show scan duration after completion`

---

## Task 7: Require admin elevation via app manifest

**Why:** The app needs admin rights to scan `C:\Windows\Installer`. Currently it fails at runtime with a confusing error if not elevated. A proper app manifest with `requireAdministrator` will trigger the UAC prompt automatically when the user launches the app.

**Files to create/modify:**

1. **Create `src/SimpleWindowsInstallerCleaner/app.manifest`** — standard Windows app manifest requesting elevation:

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
     <trustInfo xmlns="urn:schemas-microsoft-com:asm.v3">
       <security>
         <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v2">
           <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
         </requestedPrivileges>
       </security>
     </trustInfo>
   </assembly>
   ```

2. **`src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`** — add the manifest reference. Inside the main `<PropertyGroup>`:

   ```xml
   <ApplicationManifest>app.manifest</ApplicationManifest>
   ```

**Note:** `dotnet run` will fail after this change unless the terminal is already elevated. This is expected and correct — the app genuinely needs admin rights. Running from an elevated terminal still works fine.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner` (build should succeed; running requires elevated terminal)

**Commit:** `feat: require admin elevation via app manifest`

---

## Task 8: Add version number to title bar

**Why:** Users need to know which version they're running, especially for bug reports. Add the version to the window title.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/MainWindow.xaml`** — change the Title:

   ```xml
   Title="Simple Windows installer cleaner — v0.1.0-alpha"
   ```

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: show version in main window title bar`

---

## Task 3 note — for Opus review

Tooltip approach was wrong. User feedback:
- Exclusion filters in Settings are already shown as interactive pills with an × to remove them
- There's no space constraint — explanatory text can go anywhere visible
- Hiding explanation behind a tooltip is the wrong pattern
- Sonnet added the subtitle text in OrphanedFilesWindow.xaml (committed in d3886a6) but the main window tooltip was ineffective
- Opus should redesign: consider inline static text near the excluded row, or an explanation in the Settings dialog near the filter pills

---

## Summary of changes

| Task | Type | What |
|------|------|------|
| 1 | Bug fix | Replace garbled About MessageBox with proper dialog |
| 2 | UX | Auto-select first item in detail windows |
| 3 | UX | Explain exclusion filters with tooltips and descriptions |
| 4 | UX | Hide excluded row when count is 0 |
| 5 | UX | Suppress scanning overlay for fast scans |
| 6 | UX | Show scan duration |
| 7 | UX | Admin elevation via app manifest (UAC prompt) |
| 8 | UX | Version number in title bar |

---

## Execution notes for Sonnet

1. **Do tasks in order.** Tasks are independent but should be committed in order.
2. **Build and test after every task.** The plan says when to verify — do it.
3. **Commit after every task.** Small, focused commits.
4. **Don't add features not in this plan.** No extras, no "improvements".
5. **British English in all user-facing strings.** Sentence case. No Oxford comma.
6. **Task 5 might be a no-op.** Check whether the overlay flash is an actual problem before adding the delay logic. If it looks fine, mark done and skip the commit.
7. **Task 7 changes how the app launches.** After adding the manifest, `dotnet run` requires an elevated terminal. Don't be surprised by this. Build verification still works from any terminal.
8. **The version is 0.1.0-alpha**, not 1.0.0. The app hasn't been released yet.
