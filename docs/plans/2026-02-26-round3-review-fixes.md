# Round 3 — code review fixes

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.
> Each task ends with a build/test step and a commit. Do not skip verification steps.
> Read CLAUDE.md before starting — British English, sentence case, no Oxford comma.

## Progress (updated by executor)

| Task | Commit | Status |
|------|--------|--------|
| 1 — Fix patches "(none)" placeholder | 0ebaa4f | ✓ done |
| 2 — Remove dead Visibility attribute in OrphanedFilesWindow | ae12b42 | ✓ done |
| 3 — Proper pluralisation (remove "(s)" pattern) | 73fa7d5 | ✓ done |
| 4 — Unit tests for RegisteredFilesViewModel grouping | 85efcff | ✓ done |
| 5 — Narrow bare catch blocks in MsiFileInfoService | 307eb90 | ✓ done |

## Author: Opus, 26 February 2026

**Goal:** Address all issues raised in the Opus code review of round 2.

**Context:** Round 2 (9 tasks) is complete. Opus code review found 2 important issues and 3 nice-to-have suggestions. This round fixes all of them.

---

## Task 1: Fix patches "(none)" placeholder in RegisteredFilesWindow

**Why:** The "(none)" text in the patches panel only shows when `SelectedProduct` is null. When a product IS selected but has 0 patches, the user sees a blank panel. The plan for round 2, task 6 explicitly says: "Show '(none)' placeholder when the list is empty."

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/ViewModels/RegisteredFilesViewModel.cs`** — add a computed property:

   ```csharp
   public bool HasPatches => SelectedProduct is not null && SelectedProduct.Patches.Count > 0;
   ```

   Add `[NotifyPropertyChangedFor(nameof(HasPatches))]` to `_selectedProduct`.

2. **`src/SimpleWindowsInstallerCleaner/RegisteredFilesWindow.xaml`** — change the patches panel visibility logic.

   The "(none)" placeholder (lines 120-136) currently triggers only on `SelectedProduct == null`. Change it to show when EITHER no product is selected OR the selected product has 0 patches. Replace the DataTrigger:

   **Before:**
   ```xml
   <DataTrigger Binding="{Binding SelectedProduct}" Value="{x:Null}">
       <Setter Property="Visibility" Value="Visible"/>
   </DataTrigger>
   ```

   **After:** Use a `MultiDataTrigger` — or simpler: bind to a new ViewModel property. The simplest approach is to invert the new `HasPatches` property. Change the placeholder's style to:
   ```xml
   <Style TargetType="TextBlock">
       <Setter Property="Visibility" Value="Visible"/>
       <Style.Triggers>
           <DataTrigger Binding="{Binding HasPatches}" Value="True">
               <Setter Property="Visibility" Value="Collapsed"/>
           </DataTrigger>
       </Style.Triggers>
   </Style>
   ```

   And the ListBox style (lines 143-151) should also use `HasPatches`:
   ```xml
   <Style TargetType="ListBox">
       <Setter Property="Visibility" Value="Collapsed"/>
       <Style.Triggers>
           <DataTrigger Binding="{Binding HasPatches}" Value="True">
               <Setter Property="Visibility" Value="Visible"/>
           </DataTrigger>
       </Style.Triggers>
   </Style>
   ```

   This way: no selection → "(none)" shows, patches list hidden. Product selected with 0 patches → "(none)" shows, patches list hidden. Product selected with patches → "(none)" hidden, patches list shows.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `fix: show "(none)" in patches panel when selected product has no patches`

---

## Task 2: Remove dead Visibility attribute in OrphanedFilesWindow

**Why:** In `OrphanedFilesWindow.xaml` line 155, the "Select a file to view details." placeholder has both an inline `Visibility` attribute with an unsupported `ConverterParameter=Inverse` AND a Style with DataTriggers controlling the same property. The Style wins at runtime, so the inline attribute is dead/misleading markup.

**File to modify:**
- `src/SimpleWindowsInstallerCleaner/OrphanedFilesWindow.xaml`

**Change:** Remove the inline `Visibility` attribute from line 155. The TextBlock currently reads:

```xml
<TextBlock Text="Select a file to view details."
           FontSize="12"
           Foreground="#888"
           FontStyle="Italic"
           VerticalAlignment="Center"
           HorizontalAlignment="Center"
           TextAlignment="Center"
           TextWrapping="Wrap"
           Visibility="{Binding HasSelection, Converter={StaticResource BoolToVis}, ConverterParameter=Inverse}">
```

Remove the entire `Visibility="{Binding HasSelection, Converter={StaticResource BoolToVis}, ConverterParameter=Inverse}"` line. The Style triggers handle the visibility correctly.

Result should be:
```xml
<TextBlock Text="Select a file to view details."
           FontSize="12"
           Foreground="#888"
           FontStyle="Italic"
           VerticalAlignment="Center"
           HorizontalAlignment="Center"
           TextAlignment="Center"
           TextWrapping="Wrap">
```

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `fix: remove dead Visibility attribute from orphaned files placeholder`

---

## Task 3: Proper pluralisation (remove "(s)" pattern)

**Why:** The `"file(s)"` and `"error(s)"` patterns are functional but awkward. Replace with proper conditional plurals throughout.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/Helpers/DisplayHelpers.cs`** — add a helper:

   ```csharp
   internal static string Pluralise(int count, string singular, string plural) =>
       count == 1 ? singular : plural;
   ```

2. **`src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs`** — replace all `file(s)` and `error(s)` occurrences. There are 6 instances (lines 180, 188, 189, 212, 221, 229, 230):

   - Line 180: `$"Moving {filePaths.Count} file(s)..."` → `$"Moving {filePaths.Count} {DisplayHelpers.Pluralise(filePaths.Count, "file", "files")}..."`
   - Line 188: `$"Moved {result.MovedCount} file(s) to {MoveDestination}."` → `$"Moved {result.MovedCount} {DisplayHelpers.Pluralise(result.MovedCount, "file", "files")} to {MoveDestination}."`
   - Line 189: `$"Moved {result.MovedCount} file(s). {result.Errors.Count} error(s)."` → `$"Moved {result.MovedCount} {DisplayHelpers.Pluralise(result.MovedCount, "file", "files")}. {result.Errors.Count} {DisplayHelpers.Pluralise(result.Errors.Count, "error", "errors")}."`
   - Line 212: `$"Permanently delete {count} file(s) ({sizeDisplay})?\n\nThis cannot be undone."` → `$"Permanently delete {count} {DisplayHelpers.Pluralise(count, "file", "files")} ({sizeDisplay})?\n\nThis cannot be undone."`
   - Line 221: `$"Deleting {filePaths.Count} file(s)..."` → `$"Deleting {filePaths.Count} {DisplayHelpers.Pluralise(filePaths.Count, "file", "files")}..."`
   - Line 229: `$"Deleted {result.DeletedCount} file(s)."` → `$"Deleted {result.DeletedCount} {DisplayHelpers.Pluralise(result.DeletedCount, "file", "files")}."`
   - Line 230: `$"Deleted {result.DeletedCount} file(s). {result.Errors.Count} error(s)."` → `$"Deleted {result.DeletedCount} {DisplayHelpers.Pluralise(result.DeletedCount, "file", "files")}. {result.Errors.Count} {DisplayHelpers.Pluralise(result.Errors.Count, "error", "errors")}."`

3. **`src/SimpleWindowsInstallerCleaner/ViewModels/RegisteredFilesViewModel.cs`** — line 89:

   `$"{packages.Count} registered file(s) ({DisplayHelpers.FormatSize(totalBytes)})"` → `$"{packages.Count} registered {DisplayHelpers.Pluralise(packages.Count, "file", "files")} ({DisplayHelpers.FormatSize(totalBytes)})"`

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `fix: use proper pluralisation instead of "(s)" pattern`

---

## Task 4: Unit tests for RegisteredFilesViewModel grouping

**Why:** The grouping-by-ProductCode logic is the most complex new code from round 2 and has zero test coverage. Edge cases include: product with only patches and no MSI, empty ProductName, and multiple products.

**File to create:**
- `src/SimpleWindowsInstallerCleaner.Tests/ViewModels/RegisteredFilesViewModelTests.cs`

**Test setup:** Create a `Mock<IMsiFileInfoService>` that returns `null` for all calls (we're testing grouping, not metadata).

Helper to create test data:
```csharp
private static RegisteredPackage Pkg(string path, string name, string code) =>
    new(path, name, code);

private static Mock<IMsiFileInfoService> NullInfoService()
{
    var mock = new Mock<IMsiFileInfoService>();
    mock.Setup(s => s.GetSummaryInfo(It.IsAny<string>())).Returns((MsiSummaryInfo?)null);
    return mock;
}
```

**Tests:**

1. **`Groups_products_by_ProductCode`** — pass 2 packages with different ProductCodes, both `.msi`. Verify `Products.Count == 2` and each has `PatchCount == 0`.

2. **`Counts_patches_per_product`** — pass 1 `.msi` and 2 `.msp` all sharing the same ProductCode. Verify `Products.Count == 1`, `Products[0].PatchCount == 2`, `Products[0].Patches.Count == 2`.

3. **`Handles_product_with_only_patches_and_no_msi`** — pass 2 `.msp` with the same ProductCode and no `.msi`. Verify `Products.Count == 1`. The product row should still exist — `FileName` will be the first patch's filename, since the fallback is `items.First().LocalPackagePath`.

4. **`Empty_product_name_becomes_unknown`** — pass 1 `.msi` with `ProductName = ""`. Verify `Products[0].ProductName == "(unknown)"`.

5. **`Summary_shows_total_count_and_size`** — pass 2 packages, totalBytes = 1_048_576. Verify `Summary == "2 registered files (1.0 MB)"`.

**Note:** The `GetSizeDisplay` method calls `new FileInfo(path).Length` which will throw for fake paths. Since `GetSizeDisplay` has a `catch` that returns `string.Empty`, the test will work — `SizeDisplay` will just be empty. That's fine for testing the grouping logic.

**Verify:** `dotnet test src/SimpleWindowsInstallerCleaner.Tests`

**Commit:** `test: add unit tests for RegisteredFilesViewModel grouping logic`

---

## Task 5: Narrow bare catch blocks in MsiFileInfoService

**Why:** The bare `catch` blocks in `MsiFileInfoService.cs` (lines 29 and 85) catch everything including critical CLR exceptions like `OutOfMemoryException`. For P/Invoke code this is common practice, but `catch (Exception)` is marginally safer — it still catches all managed exceptions but lets critical runtime exceptions propagate.

**File to modify:**
- `src/SimpleWindowsInstallerCleaner/Services/MsiFileInfoService.cs`

**Changes:**

1. Line 29: `catch` → `catch (Exception)`
2. Line 85: `catch` → `catch (Exception)`

That's it. No logic changes.

**Verify:** `dotnet test src/SimpleWindowsInstallerCleaner.Tests`

**Commit:** `fix: narrow bare catch blocks to catch (Exception) in MsiFileInfoService`

---

## Summary of changes

| Task | Type | What |
|------|------|------|
| 1 | Bug fix | Patches "(none)" placeholder for empty patch lists |
| 2 | Cleanup | Remove dead Visibility attribute from orphaned files XAML |
| 3 | Polish | Proper pluralisation instead of "(s)" pattern |
| 4 | Test | Unit tests for RegisteredFilesViewModel grouping logic |
| 5 | Cleanup | Narrow bare catch blocks to catch (Exception) |

---

## Execution notes for Sonnet

1. **Do tasks in order.** Tasks are independent but should be committed in order.
2. **Build and test after every task.** The plan says when to verify — do it.
3. **Commit after every task.** Small, focused commits.
4. **Don't add features not in this plan.** No extras, no "improvements".
5. **British English in all user-facing strings.** Sentence case. No Oxford comma.
6. **Task 3 has many string replacements — be precise.** Count all `file(s)` and `error(s)` in `MainViewModel.cs` and replace them all.
7. **Task 4 tests use fake file paths** so `GetSizeDisplay` will return empty. This is expected — we're testing grouping, not file I/O.
