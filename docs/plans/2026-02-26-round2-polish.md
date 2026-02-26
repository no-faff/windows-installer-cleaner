# Round 2 — bug fixes, PatchCleaner parity and polish

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.
> Each task ends with a build/test step and a commit. Do not skip verification steps.
> Read CLAUDE.md before starting — British English, sentence case, no Oxford comma.

## Progress (updated by executor)

| Task | Commit | Status |
|------|--------|--------|
| 1 — Extract FormatSize helper | 4b7683d | ✓ done |
| 2 — Fix dual-ListBox selection | 4bae2cb | ✓ done |
| 3 — Settings save exception handling | 2df6470 | ✓ done |
| 4 — Dead code cleanup | e045b62 | ✓ done |
| 5 — Exclusion filters on MSI metadata | e6015f3 | ✓ done |
| 6 — Registered files master-detail | 621199f | ✓ done |
| 7 — File Size/Comment in orphaned panel | 7305562 | ✓ done |
| 8 — Publish profile | 8a249cb | ✓ done |
| 9 — Update tests | | |

## Author: Opus, 26 February 2026

**Goal:** Fix known bugs, achieve PatchCleaner feature parity, prepare for distribution.

**Context:** Round 1 (16 tasks, `docs/plans/2026-02-26-v1-redesign.md`) built the complete app skeleton. All 16 tasks are done (17 tests passing, 0 warnings). This round addresses the bugs and feature gaps found during Opus code review.

**PatchCleaner screenshots are at `docs/PatchCleaner-screenshots/` — refer to them for UI reference.**

---

## What already exists (do not recreate)

```
src/SimpleWindowsInstallerCleaner/
  Interop/MsiConstants.cs, MsiNativeMethods.cs
  Models/AppSettings.cs, MsiSummaryInfo.cs, OrphanedFile.cs, RegisteredPackage.cs, ScanResult.cs
  Services/ — all interfaces + implementations (scan, move, delete, exclusion, settings, reboot, MSI info)
  ViewModels/MainViewModel.cs, OrphanedFilesViewModel.cs, RegisteredFilesViewModel.cs, SettingsViewModel.cs
  ViewModels/OrphanedFileViewModel.cs  ← DEAD CODE, will be deleted
  MainWindow.xaml/.cs, OrphanedFilesWindow.xaml/.cs, RegisteredFilesWindow.xaml/.cs, SettingsWindow.xaml/.cs
  App.xaml.cs — composition root
src/SimpleWindowsInstallerCleaner.Tests/ — 17 tests passing
```

---

## Task 1: Extract shared FormatSize helper

**Why:** `FormatSize` is duplicated in `MainViewModel`, `OrphanedFilesViewModel` and `RegisteredFilesViewModel` with a subtle inconsistency — `OrphanedFilesViewModel` uses `F1` for GB while the others use `F2`. The same file could display as "2.0 GB" in one window and "2.00 GB" in another.

**Files to create:**
- `src/SimpleWindowsInstallerCleaner/Helpers/DisplayHelpers.cs`

**Files to modify:**
- `src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs` — delete `FormatSize`, use `DisplayHelpers.FormatSize`
- `src/SimpleWindowsInstallerCleaner/ViewModels/OrphanedFilesViewModel.cs` — delete `FormatSize`, use `DisplayHelpers.FormatSize`
- `src/SimpleWindowsInstallerCleaner/ViewModels/RegisteredFilesViewModel.cs` — delete `FormatSize`, use `DisplayHelpers.FormatSize`

**DisplayHelpers.cs:**
```csharp
namespace SimpleWindowsInstallerCleaner.Helpers;

internal static class DisplayHelpers
{
    internal static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{bytes / 1_024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
```

Use `F2` for GB everywhere (matches PatchCleaner's "2.36 Gb" style).

In `MainViewModel` the method is `internal static` and referenced in one test — check if any test calls `MainViewModel.FormatSize()` and update the reference to `DisplayHelpers.FormatSize()`.

**Verify:** `dotnet test src/SimpleWindowsInstallerCleaner.Tests`

**Commit:** `refactor: extract shared FormatSize helper and fix GB precision inconsistency`

---

## Task 2: Fix dual-ListBox selection highlight in OrphanedFilesWindow

**Why:** `OrphanedFilesWindow.xaml` has two `ListBox` controls (actionable files and excluded files) both binding `SelectedItem` to `SelectedFile`. When the user clicks in one ListBox then clicks in the other, both ListBoxes show a highlighted item — but details only show for the most recently clicked one. This is visually confusing.

**File to modify:**
- `src/SimpleWindowsInstallerCleaner/OrphanedFilesWindow.xaml.cs`

**Change:** Add `GotFocus` event handlers that clear the other ListBox's selection. Give each ListBox an `x:Name` in the XAML.

In `OrphanedFilesWindow.xaml`, add `x:Name="ActionableList"` to the first ListBox and `x:Name="ExcludedList"` to the second. Add `GotFocus="ActionableList_GotFocus"` and `GotFocus="ExcludedList_GotFocus"` respectively.

In `OrphanedFilesWindow.xaml.cs`:
```csharp
private void ActionableList_GotFocus(object sender, RoutedEventArgs e)
{
    ExcludedList.UnselectAll();
}

private void ExcludedList_GotFocus(object sender, RoutedEventArgs e)
{
    ActionableList.UnselectAll();
}
```

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `fix: clear other ListBox selection when switching between orphaned/excluded lists`

---

## Task 3: Add exception handling to settings save operations

**Why:** `SettingsService.Save()` doesn't catch exceptions. If writing to disk fails (permissions, disk full), an unhandled exception propagates. Both `MainViewModel.BrowseDestination()` and `SettingsViewModel.Save()` call it without try/catch.

**Files to modify:**
- `src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs` — wrap the `_settingsService.Save()` call in `BrowseDestination()` in try/catch
- `src/SimpleWindowsInstallerCleaner/ViewModels/SettingsViewModel.cs` — wrap the `_settingsService.Save()` call in `Save()` in try/catch

On error, show a `MessageBox` with the message: "Could not save settings: {ex.Message}". Use `MessageBoxImage.Warning`. The operation that triggered the save (browse / settings change) should still succeed in-memory — the user just won't have persistence until the disk issue is resolved.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `fix: handle settings save exceptions gracefully`

---

## Task 4: Clean up dead code

**Why:** Several files are unused or misplaced.

**Changes:**

1. **Delete `src/SimpleWindowsInstallerCleaner/ViewModels/OrphanedFileViewModel.cs`** — wraps `OrphanedFile` with `IsSelected` but is not used anywhere. It was kept from round 1 "for future use" but per-file selection is not being added in this round either.

2. **Delete `src/SimpleWindowsInstallerCleaner.Tests/PlaceholderTest.cs`** — contains only `Assert.True(true)`. Was the initial scaffold.

3. **Remove `MsiInstallProperty.Publisher`** from `MsiConstants.cs` — defined but never used. Only `ProductName` and `LocalPackage` are used by `InstallerQueryService`.

4. **Move `IMsiFileInfoService` interface to its own file** — currently declared inside `MsiFileInfoService.cs`. Every other service interface has its own file. Create `src/SimpleWindowsInstallerCleaner/Services/IMsiFileInfoService.cs` with just the interface, and remove the interface declaration from `MsiFileInfoService.cs`.

**Verify:** `dotnet test src/SimpleWindowsInstallerCleaner.Tests` — should now be 16 tests (placeholder removed).

**Commit:** `chore: remove dead code and move IMsiFileInfoService to own file`

---

## Task 5: Enhance exclusion filters to match on MSI summary info fields

**Why:** PatchCleaner's exclusion filter is described in its Settings window as: "This is a contains filter on the author, title, subject and digital signature properties of the orphaned file." Our current implementation only matches on filename. This means a filter like "Acrobat" works by coincidence (Adobe filenames often contain it), but "Adobe" wouldn't catch files whose filename is a hex GUID like `9c738.msi` but whose Author is "Adobe Systems Incorporated". For true PatchCleaner parity, filters must match against summary info fields.

**Performance note:** We already have `MsiFileInfoService` which reads summary info via P/Invoke — this is fast (single file open per file). We only need to read metadata for orphaned files (typically 0–50 files), not all registered files. No "Deep Scan" toggle is needed — our approach is inherently faster than PatchCleaner's OLE COM interop.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/Services/IExclusionService.cs`** — add `IMsiFileInfoService` as a parameter or change signature:

   ```csharp
   public interface IExclusionService
   {
       FilteredResult ApplyFilters(
           IReadOnlyList<OrphanedFile> files,
           IReadOnlyList<string> filters,
           IMsiFileInfoService? infoService = null);
   }
   ```

   When `infoService` is null, fall back to filename-only matching (keeps tests simple).

2. **`src/SimpleWindowsInstallerCleaner/Services/ExclusionService.cs`** — update the matching logic:

   ```csharp
   foreach (var file in files)
   {
       // Always check filename first (fast)
       var isExcluded = filters.Any(f =>
           file.FileName.Contains(f, StringComparison.OrdinalIgnoreCase));

       // If not excluded by filename and we have an info service, check metadata
       if (!isExcluded && infoService is not null)
       {
           var info = infoService.GetSummaryInfo(file.FullPath);
           if (info is not null)
           {
               isExcluded = filters.Any(f =>
                   info.Author.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                   info.Title.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                   info.Subject.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                   info.DigitalSignature.Contains(f, StringComparison.OrdinalIgnoreCase));
           }
       }

       if (isExcluded)
           excluded.Add(file);
       else
           actionable.Add(file);
   }
   ```

3. **`src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs`** — pass `_msiInfoService` to `ApplyFilters`:

   In `ScanAsync()`:
   ```csharp
   _lastFilteredResult = _exclusionService.ApplyFilters(
       _lastScanResult.OrphanedFiles, _settings.ExclusionFilters, _msiInfoService);
   ```

   In `OpenSettings()` (the re-filter after settings change):
   ```csharp
   _lastFilteredResult = _exclusionService.ApplyFilters(
       _lastScanResult.OrphanedFiles, _settings.ExclusionFilters, _msiInfoService);
   ```

4. **`src/SimpleWindowsInstallerCleaner/SettingsWindow.xaml`** — update the description text from "Files matching these terms won't be moved or deleted." to:

   "Files whose name, author, title, subject or digital signature matches these terms won't be moved or deleted."

5. **`src/SimpleWindowsInstallerCleaner.Tests/Services/ExclusionServiceTests.cs`** — existing tests pass `null` for the info service (they test filename matching only). Add one new test:

   - `ApplyFilters_matches_on_summary_info_fields` — create a `Mock<IMsiFileInfoService>` that returns an `MsiSummaryInfo` with Author "Adobe Systems". Pass filter "Adobe" and a file whose filename does NOT contain "Adobe". Verify the file ends up in Excluded.

**Verify:** `dotnet test src/SimpleWindowsInstallerCleaner.Tests`

**Commit:** `feat: exclusion filters match on MSI summary info fields (author, title, subject, signature)`

---

## Task 6: Redesign registered files window as master-detail with patches

**Why:** PatchCleaner's Products window (see `docs/PatchCleaner-screenshots/still-used.png` and `4Details of the product to retain.png`) shows a three-panel layout:
- **Top:** Products list with Name, File Name, File Size, Patches columns
- **Bottom-left:** Patches list for the selected product
- **Bottom-right:** Product Details (Author, Title, Subject, Digital Signature, File Size, Comment)

Our current `RegisteredFilesWindow` is a flat list with a details panel. It doesn't group patches under products or show patch counts.

**Data model insight:** `InstallerQueryService` creates a `RegisteredPackage` for both the product's MSI and each of its patches, all sharing the same `ProductCode`. We can group by `ProductCode` and distinguish MSI (product) from MSP (patch) by file extension.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/ViewModels/RegisteredFilesViewModel.cs`** — full rewrite.

   New types (define in this file):

   ```csharp
   public sealed record ProductRow(
       string ProductName,
       string FileName,       // the MSI filename
       string FullPath,       // the MSI full path
       string SizeDisplay,
       int PatchCount,
       IReadOnlyList<PatchRow> Patches);

   public sealed record PatchRow(
       string FileName,
       string FullPath,
       string SizeDisplay);
   ```

   ViewModel properties:

   ```csharp
   public IReadOnlyList<ProductRow> Products { get; }
   public string Summary { get; }

   [ObservableProperty] private ProductRow? _selectedProduct;
   [ObservableProperty] private MsiSummaryInfo? _selectedDetails;

   // Derived
   public bool HasSelection => SelectedProduct is not null;
   public bool ShowDetails => SelectedProduct is not null && SelectedDetails is not null;
   public bool ShowNoMetadata => SelectedProduct is not null && SelectedDetails is null;
   public IReadOnlyList<PatchRow> SelectedPatches => SelectedProduct?.Patches ?? Array.Empty<PatchRow>();
   ```

   Constructor logic:
   ```csharp
   // Group RegisteredPackages by ProductCode
   var groups = packages.GroupBy(p => p.ProductCode, StringComparer.OrdinalIgnoreCase);

   var products = new List<ProductRow>();
   foreach (var group in groups.OrderBy(g => g.First().ProductName))
   {
       var items = group.ToList();

       // Find the MSI (product) — it's the one with .msi extension
       var msi = items.FirstOrDefault(p =>
           p.LocalPackagePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

       // Patches are the .msp entries
       var patches = items
           .Where(p => p.LocalPackagePath.EndsWith(".msp", StringComparison.OrdinalIgnoreCase))
           .Select(p => new PatchRow(
               Path.GetFileName(p.LocalPackagePath),
               p.LocalPackagePath,
               GetSizeDisplay(p.LocalPackagePath)))
           .ToList();

       if (msi is null && patches.Count == 0) continue;

       var productName = items.First().ProductName;
       if (string.IsNullOrEmpty(productName)) productName = "(unknown)";

       var msiPath = msi?.LocalPackagePath ?? items.First().LocalPackagePath;

       products.Add(new ProductRow(
           productName,
           Path.GetFileName(msiPath),
           msiPath,
           GetSizeDisplay(msiPath),
           patches.Count,
           patches));
   }

   Products = products;
   ```

   `OnSelectedProductChanged` partial method: read MSI summary info for the product's MSI path (cached). Also notify `SelectedPatches` changed.

   Delete the old `RegisteredFileRow` record and all old code.

2. **`src/SimpleWindowsInstallerCleaner/RegisteredFilesWindow.xaml`** — redesign to match PatchCleaner layout:

   ```
   ┌─ Registered files ──────────────────────────────────────── ×─┐
   │                                                               │
   │  Products                                                     │
   │  ┌─────────────────────────────────────────────────────────┐  │
   │  │ Name              │ File       │ Size     │ Patches    │  │
   │  │ calibre 64bit     │ 915...msi  │ 197 MB   │ 0          │  │
   │  │ GitHub CLI ◄──    │ 159...msi  │ 13.4 MB  │ 0          │  │
   │  │ KeePassXC         │ 145...msi  │ 34.0 MB  │ 0          │  │
   │  └─────────────────────────────────────────────────────────┘  │
   │                                                               │
   │  Patches                          Product details             │
   │  ┌────────────────┐  ┌─────────────────────────────────────┐  │
   │  │ File name      │  │ Author:    Kovid Goyal              │  │
   │  │ (none)         │  │ Title:     Installation Database    │  │
   │  │                │  │ Subject:   calibre Installer        │  │
   │  │                │  │ Signature: CN=Kovid Goyal...        │  │
   │  │                │  │ Size:      197 MB                   │  │
   │  │                │  │ Comment:   This installer...        │  │
   │  └────────────────┘  └─────────────────────────────────────┘  │
   │                                                               │
   │  86 registered files (2.36 GB)                       [Close]  │
   └───────────────────────────────────────────────────────────────┘
   ```

   Window size: `Width="800" Height="550"` (slightly bigger to fit the three-panel layout).

   **Top half:** `ListView` with `GridView` for Products. Four columns: Product name (Width="250"), File (Width="140"), Size (Width="80"), Patches (Width="60"). Bind `SelectedItem="{Binding SelectedProduct}"`.

   **Bottom half:** Two-column Grid.
   - **Left column (~220px):** "Patches" header label + `ListBox` bound to `SelectedPatches`. Item template: filename + size. Show "(none)" placeholder when the list is empty.
   - **Right column (*):** "Product details" header label + details grid (same fields as PatchCleaner: Author, Title, Subject, Digital Signature, File Size, Comment). Add **File Size** to the detail panel — it's not in `MsiSummaryInfo` but it's the `SizeDisplay` from the selected `ProductRow`. Also add **Comment** (which is `MsiSummaryInfo.Comments`).

   Use the same visibility pattern as the current detail panel (placeholder when nothing selected, "No metadata available" when MSI can't be read).

3. **`src/SimpleWindowsInstallerCleaner/RegisteredFilesWindow.xaml.cs`** — no changes needed beyond constructor (already takes ViewModel).

4. **`src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs`** — the `OpenRegisteredDetails()` method passes `_lastScanResult.RegisteredPackages` to the ViewModel. No change needed — the ViewModel constructor signature stays compatible.

**Note on "File Size" in detail panel:** PatchCleaner shows File Size as a detail field (see screenshots). Add it to the detail panel using the selected product's `SizeDisplay` property, not from `MsiSummaryInfo`. Bind directly: `Text="{Binding SelectedProduct.SizeDisplay}"`. Show this in the details grid between Digital Signature and Comment rows.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: redesign registered files window with master-detail product/patch layout`

---

## Task 7: Add File Size and Comment to orphaned files detail panel

**Why:** PatchCleaner shows File Size and Comment in the orphaned files detail panel (see `docs/PatchCleaner-screenshots/orphaned.png`). Our current panel shows Author, Title, Subject, Comments, Signature but not File Size. Also, "Comments" should be labelled "Comment" (singular, matching PatchCleaner).

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/OrphanedFilesWindow.xaml`** — add a "Size" row to the details grid showing the selected file's `SizeDisplay` property. Rename "Comments" label to "Comment". Reorder to match PatchCleaner: Author, Title, Subject, Digital Signature (shortened to "Signature" for space), File Size (shortened to "Size"), Comment.

   Add two more rows to the details Grid:
   ```xml
   <!-- After Signature row -->
   <TextBlock Grid.Row="10" Grid.Column="0" Text="Size" FontSize="11" Foreground="#888"/>
   <TextBlock Grid.Row="10" Grid.Column="2" Text="{Binding SelectedFile.SizeDisplay}" FontSize="12" Foreground="#222"/>

   <TextBlock Grid.Row="12" Grid.Column="0" Text="Comment" FontSize="11" Foreground="#888"/>
   <TextBlock Grid.Row="12" Grid.Column="2" Text="{Binding SelectedDetails.Comments}" FontSize="12" Foreground="#222" TextWrapping="Wrap"/>
   ```

   Update the RowDefinitions to accommodate the extra rows. Remove the existing "Comments" row (it'll be replaced by the "Comment" row above).

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: add file size and comment to orphaned files detail panel`

---

## Task 8: Add framework-dependent publish profile

**Why:** Need a documented way to produce the distributable exe.

**File to create:**
- `src/SimpleWindowsInstallerCleaner/Properties/PublishProfiles/FrameworkDependent.pubxml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <Platform>Any CPU</Platform>
    <PublishDir>..\..\publish</PublishDir>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  </PropertyGroup>
</Project>
```

**Test the publish:**
```
dotnet publish src/SimpleWindowsInstallerCleaner -c Release -p:PublishProfile=FrameworkDependent
```

Verify the output exe exists in `publish/` and is small (< 5 MB).

If the publish profile approach doesn't work cleanly with `dotnet publish`, just document the command in a comment in the pubxml or skip the profile and use the direct command:
```
dotnet publish src/SimpleWindowsInstallerCleaner -c Release --self-contained false -p:PublishSingleFile=true -r win-x64 -o publish
```

**Verify:** The publish command produces an exe in `publish/`.

**Commit:** `chore: add framework-dependent publish profile`

---

## Task 9: Update tests for new architecture

**Why:** Tasks 4 and 5 changed `ExclusionService`'s interface and removed dead test code. Make sure everything still passes and add missing coverage.

**Check and fix:**

1. All existing `ExclusionService` tests — update calls to `ApplyFilters` to pass `null` as the third argument (info service) if the interface changed. Alternatively, if you used a default parameter (`IMsiFileInfoService? infoService = null`), existing tests should compile without changes.

2. Add the new test from Task 5: `ApplyFilters_matches_on_summary_info_fields`.

3. Verify test count: should be 16 (placeholder deleted) + 1 new = 17.

**Verify:** `dotnet test src/SimpleWindowsInstallerCleaner.Tests --verbosity normal`

**Commit:** `test: update tests for round 2 changes`

---

## Summary of changes

| Task | Type | What |
|------|------|------|
| 1 | Refactor | Extract `FormatSize` to shared helper, fix GB precision |
| 2 | Bug fix | Dual-ListBox selection highlight in OrphanedFilesWindow |
| 3 | Bug fix | Exception handling for `SettingsService.Save` |
| 4 | Cleanup | Delete dead code, move interface to own file |
| 5 | Feature | Exclusion filters match on MSI summary info fields |
| 6 | Feature | Registered files master-detail with patches (PatchCleaner parity) |
| 7 | Feature | File Size and Comment in orphaned detail panel |
| 8 | Chore | Framework-dependent publish profile |
| 9 | Test | Update tests for new architecture |

---

## What's NOT in this plan (deferred)

- App icon (needs a designer or a sourced icon — not code work)
- README / documentation (will write when features are stable)
- Per-file selection in orphaned details window (significant complexity, deferred)
- CLI interface (v2 feature)
- Code signing (SignPath.io setup — separate task)
- Version update checking (v2 feature)

---

## Execution notes for Sonnet

1. **Do tasks in order.** Each task builds on the previous.
2. **Build and test after every task.** The plan says when to verify — do it.
3. **Commit after every task.** Small, focused commits.
4. **Don't add features not in this plan.** No extras, no "improvements".
5. **British English in all user-facing strings.** Sentence case. No Oxford comma.
6. **Follow existing code patterns.** CommunityToolkit.Mvvm `[ObservableProperty]` / `[RelayCommand]` pattern, the P/Invoke patterns, the `ShowDialog()` patterns — all established.
7. **Task 6 is the hardest task.** The grouping logic and three-panel XAML layout need care. Refer to the PatchCleaner screenshots for the target layout.
8. **When in doubt, match PatchCleaner's behaviour.** Look at the screenshots in `docs/PatchCleaner-screenshots/`.
