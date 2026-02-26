# Opus analysis — 26 February 2026

> Saved from the Opus session so it survives terminal crashes.

---

## Honest assessment

### What you've got right

1. **MSI API over WMI is genuinely better**, not just different. `Win32_Product` WMI queries trigger a consistency check on every installed product — this is a well-documented Microsoft gotcha that can cause repair operations to fire. Your P/Invoke approach avoids this entirely. The Unicode "W" entry points also avoid the locale issues PatchCleaner hit.

2. **The data layer is solid.** Phase 1 is clean, correct, and fast. The double-call buffer pattern for MSI properties is the right approach.

3. **The model design is good.** `RegisteredPackage` and `OrphanedFile` as records, the service interfaces for testability, the internal test constructor on `FileSystemScanService` — all sensible.

### What you've got wrong or haven't thought through

1. **Phase 2 is a mistake.** Not "possibly" — definitively. Drop it entirely. Here's why:
   - PatchCleaner has 870,000 downloads without doing anything like Phase 2
   - The Adobe problem it tries to solve is better handled by the approach PatchCleaner proved works (exclusion filters)
   - The O(components × products) complexity isn't fixable without caching the entire product enumeration, and even then you're making thousands of unnecessary API calls
   - Component registration paths pointing to `C:\Windows\Installer` don't mean "this file is needed" — they mean "this is where the cached source is." Phase 1 already catches the actual registrations that matter

2. **`IsAdobeWarning` on `OrphanedFile` is always `false`.** Your model has the field but `FileSystemScanService` never sets it. The Adobe warning concept only worked via Phase 2, which you're dropping. You need a different mechanism.

3. **The "warn don't exclude" approach for Adobe is worse UX than PatchCleaner's.** Here's why: your target user is non-technical. Showing them a file with an amber warning saying "check before moving" gives them a decision they can't make — they have no way to know if moving the file is safe. PatchCleaner's exclusion filter is better because it makes the safe choice automatically while still being user-configurable. The right approach: **exclude by default, show the excluded count, let advanced users override.**

4. **You haven't thought about what "still used" means to the user.** PatchCleaner shows "86 files still used, 2.36 GB" — this is important context. It tells the user "the tool understands what's on your system." Your current design only shows orphans. You need to also report registered files count and size.

---

## Answers to every open question

**1. Does MsiEnumProductsEx catch the Office 2010 SP2 MSP correctly?**
Probably yes, via `MsiEnumPatchesEx`. The Office 2010 SP2 patch should be enumerated as a patch applied to the Office product. PatchCleaner's bug was likely in how WMI/VBScript enumerated patches, not a fundamental API limitation. But Office 2010 has been end-of-life since October 2020. By March 2026 it's been unsupported for 5.5 years. Don't special-case it. If someone reports the bug, fix it then.

**2. Drop Phase 2?**
Yes, unequivocally. Delete `EnumerateComponentPathsInInstallerFolder()`, `FindOwningProduct()`, `GetComponentPath()`, and all Phase 2 code. Phase 1 alone is what PatchCleaner does (via different APIs) and it's proven sufficient.

**3. Delete button — yes or no?**
**Yes.** Include it. Reasons:
- PatchCleaner has it and 870k users didn't revolt
- "Move only" is paternalistic for a tool targeting people who already know their Installer folder is bloated
- The right UX: make Move the primary/prominent action and Delete secondary. The button ordering and visual weight does the safety work, not removing functionality
- Users who want to delete will find a way (deleting from the move destination, or using a different tool). Better to keep them in your app where you control the experience

**4. OLE Summary Information Stream?**
**Skip for v1.0.** Here's the calculus:
- It's significant COM interop work (`IStorage`, `IPropertySetStorage`)
- PatchCleaner uses it for: details view, exclusion filter matching, and Adobe detection
- You can do exclusion filters on filename patterns (which is what most users configure anyway — "Acrobat" appears in Adobe filenames)
- The details view is nice-to-have, not core functionality
- Ship without it. Add it in v1.1 if users want it. You have a release deadline.

**5. Exclusion filters vs warnings?**
Exclusion filters, matching PatchCleaner. Pre-populate with "Adobe" and "Acrobat". Apply as case-insensitive substring match against the filename. Show the excluded count. Let users see excluded files in the details view (greyed out). This is proven UX.

**6. Pending reboot check?**
**Yes.** It's trivial to implement — single registry read of `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\PendingFileRenameOperations`. If the key exists and has values, show a warning banner: "Windows has pending updates. Restart before moving files." This is a genuine safety improvement over PatchCleaner.

**7. Office 2010 detection?**
Don't bother. EOL product. If your Phase 1 correctly enumerates it, great. If not, it falls under the exclusion filter mechanism. Don't write special-case code for a product Microsoft abandoned.

**8. Distribution?**
**Framework-dependent single-file exe.** Reasons:
- Self-contained is 60+ MB for a utility — absurd for what this does
- .NET 8 runtime is increasingly common on Windows 10/11
- The installer/first-run can check for .NET 8 and link to the download
- Alternatively: publish as a single-file with `PublishTrimmed` and `PublishSingleFile` — trimmed WPF apps get down to ~15-20 MB, which is acceptable
- I'd go trimmed single-file. No runtime dependency, reasonable size, zero friction.

**9. Code signing?**
**SignPath.io free tier for OSS.** It's legitimate, gives you an EV-equivalent certificate for open-source projects, and eliminates SmartScreen warnings. Worth the setup time. But don't let it block the release — ship unsigned if SignPath approval takes too long.

**10. CLI?**
**Not v1.0.** PatchCleaner's CLI had bugs (output redirection, SYSTEM account hangs). A CLI done right needs proper console allocation, structured output, and error handling that's different from the GUI path. Ship the GUI first. The SCCM crowd can wait for v1.1.

**11. Auto-scan on startup?**
**Yes, always.** With Phase 2 dropped, Phase 1 completes in seconds even on heavily-loaded machines. Show a brief scanning state on startup. No reason to make the user click.

**12. Settings persistence?**
**`AppData\Local\NoFaff\SimpleWindowsInstallerCleaner\settings.json`**. Reasons:
- `Local` not `Roaming` — these settings are machine-specific (move destination path)
- JSON not registry — easier to debug, back up, reset
- Namespace under `NoFaff` for the suite of tools

**13. Non-English Windows?**
Your MSI API approach is fine. You're using the Unicode "W" entry points, paths are compared case-insensitively with `OrdinalIgnoreCase` (not culture-dependent). The main risk is display strings (`ProductName`) but those are just shown to the user, not used for logic. You're good.

**14. Windows Server?**
Support it implicitly. .NET 8 runs on Server. Don't test for it, don't block it, don't special-case it. If it works (and it should), great.

**15. No admin elevation?**
The manifest handles it — Windows won't launch the exe without elevation. If someone hacks around it (e.g., running from a service), MSI API calls will return `ERROR_ACCESS_DENIED` (5). Catch this at the scan level and show a message: "This app requires administrator privileges." Don't over-engineer this.

---

## Additional things you missed

1. **MST (transform) files**: Skip. PatchCleaner ignores them. They're enterprise deployment artefacts and aren't "orphaned" in the same way — they're created by admins, not by Windows Installer.

2. **Other file types in `C:\Windows\Installer`**: There are also `$PatchCache$` folders and random temp files. Ignore everything that isn't `.msi` or `.msp`. This matches PatchCleaner.

3. **Per-user installations**: Your `AllUsersSid = "s-1-1-0"` approach enumerates across all contexts, which is correct for an admin-elevated tool. No issue here.

4. **Hundreds of user profiles**: Not a real concern. `MsiEnumProductsEx` with `s-1-1-0` handles this internally. The API was designed for this use case.

5. **First-run experience**: On first launch, the move destination is empty. Auto-scan runs, shows results. The "Move" button should be disabled until a destination is chosen. When the user clicks "Move" with no destination, prompt them. Simple.

6. **Undo/restore**: Consider a simple log file in the move destination recording what was moved and when. Not a v1.0 feature, but worth noting. PatchCleaner doesn't have this either.

---

## The right design

### Main window (compact, ~700×300, DPI-aware)

```
┌─ Simple Windows installer cleaner ─────────────────────── ─ □ ×─┐
│                                                                   │
│   ● 86 files still used                           2.36 GB        │
│   ● 2 files excluded by filters                  14.8 MB         │
│   ● 12 files orphaned                           847.2 MB  ›      │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │ Move to: D:\InstallerBackup                     [Browse...] │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  [ Move ]                                           [ Delete ]    │
│                                                                   │
│   ⚙ Settings                                    no-faff.github.io│
└───────────────────────────────────────────────────────────────────┘
```

Key design decisions:
- **Three summary lines** like PatchCleaner — users instantly understand their system
- **"›" on the orphaned line** opens a details flyout/window with the full file list (checkboxes, filename, size, type)
- **Move is primary** (left, accent colour). **Delete is secondary** (right, neutral/danger colour, with confirmation dialog)
- **Move/Delete operate on all orphaned (non-excluded) files by default.** In the details view, users can deselect individual files
- **Auto-scan on startup** — the three lines populate within seconds
- **Settings** opens a flyout/dialog for: exclusion filters, move destination persistence, pending reboot check toggle
- **Pending reboot warning** replaces the summary lines with a banner when detected

### Details window (orphaned files)

```
┌─ Orphaned files ──────────────────────────────────── ─ □ ×─┐
│                                                              │
│  ☑  filename.msi          14.2 MB    .msi                   │
│  ☑  patch123.msp           8.1 MB    .msp                   │
│  ☐  AcrobatPatch.msp       2.4 MB    .msp    [excluded]     │
│  ☑  update.msi            22.8 MB    .msi                   │
│                                                              │
│  Select all  ·  Select none  ·  12 selected (847.2 MB)      │
│                                                     [Close]  │
└──────────────────────────────────────────────────────────────┘
```

- Excluded files shown greyed out with `[excluded]` badge — visible but not selectable
- Sort by size (descending) by default
- Columns: checkbox, filename, size, type

### Settings

```
┌─ Settings ──────────────────────────────────── ─ □ ×─┐
│                                                       │
│  Exclusion filters                                    │
│  Files matching these terms won't be moved or deleted │
│                                                       │
│  ┌─────────────────────────────────────────┐  [Add]   │
│  │ Adobe                                   │          │
│  │ Acrobat                                 │ [Remove] │
│  └─────────────────────────────────────────┘          │
│                                                       │
│  ☑ Check for pending reboots before scanning          │
│                                                       │
│                              [Save]    [Cancel]       │
└───────────────────────────────────────────────────────┘
```

- Pre-populated: "Adobe", "Acrobat"
- Case-insensitive substring match against filename
- Pending reboot check enabled by default

---

## Implementation plan

This assumes Phase 2 is dropped, and aims for a clean, shippable v1.0 by 3 March 2026. Ordered by dependency — each step builds on the previous.

### Step 1: Strip Phase 2 from InstallerQueryService

- Delete `EnumerateComponentPathsInInstallerFolder()`, `FindOwningProduct()`, `GetComponentPath()`, and all Phase 2 calling code from `GetRegisteredPackagesCore()`
- Remove `MsiEnumComponentsEx` and `MsiGetComponentPathEx` from `MsiNativeMethods.cs`
- Remove related constants from `MsiConstants.cs`
- Remove `IsAdobeWarning` from `RegisteredPackage` (it was only set by Phase 2)
- Update `FileSystemScanService` to not reference `IsAdobeWarning` on registered packages
- Run existing tests — should still pass

### Step 2: Add exclusion filter support

- Create `Models/ExclusionFilter.cs` — simple list of string patterns
- Create `Services/ExclusionService.cs` — applies filters (case-insensitive substring on filename), returns `ExclusionResult { List<OrphanedFile> Orphaned, List<OrphanedFile> Excluded }`
- Update `FileSystemScanService` or create a new orchestrating service that returns both orphaned and excluded lists
- Pre-populate with "Adobe", "Acrobat"
- Add tests for exclusion logic

### Step 3: Add settings persistence

- Create `Services/SettingsService.cs` — reads/writes `settings.json` from `AppData\Local\NoFaff\SimpleWindowsInstallerCleaner\`
- Settings model: `MoveDestination` (string), `ExclusionFilters` (List\<string\>), `CheckPendingReboot` (bool)
- Default exclusion filters: ["Adobe", "Acrobat"]
- Default pending reboot check: true
- Load on startup, save on change

### Step 4: Add pending reboot check

- Create `Services/PendingRebootService.cs` — reads `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\PendingFileRenameOperations`
- Returns `bool HasPendingReboot`
- Interface for testability

### Step 5: Update scan to return registered file stats

- Modify `FileSystemScanService` (or create `ScanResultService`) to return a `ScanResult` record:
  ```csharp
  public record ScanResult(
      IReadOnlyList<OrphanedFile> OrphanedFiles,
      IReadOnlyList<OrphanedFile> ExcludedFiles,
      int RegisteredFileCount,
      long RegisteredTotalBytes);
  ```
- This requires enumerating the registered files' sizes from disk (quick `FileInfo` calls)

### Step 6: Redesign the UI — main window

- Compact window (~700×300), three summary lines, move destination, Move/Delete buttons
- Auto-scan on startup with brief progress indicator
- Move destination persisted via SettingsService
- Pending reboot warning banner
- Settings gear icon opening settings dialog

### Step 7: Add details window

- Modal or modeless window showing the orphaned file list
- Checkboxes for per-file selection
- Excluded files shown greyed out
- Select all / Select none
- Opens from the "›" link on the orphaned summary line

### Step 8: Add settings dialog

- Exclusion filter management (add/remove)
- Pending reboot check toggle
- Save/Cancel

### Step 9: Add delete functionality

- Confirmation dialog: "Permanently delete X files (Y MB)? This cannot be undone."
- `DeleteFilesService` parallel to `MoveFilesService`
- Wire to Delete button

### Step 10: Add "still used" details window (optional for v1.0)

- Shows registered products and their cached files
- Lower priority — the count in the main window is the important part

### Step 11: Polish and ship

- Remove `IsAdobeWarning` from `OrphanedFile` model (replaced by exclusion system)
- Error handling for non-admin scenarios
- Window icon
- About/info with version, licence, link to GitHub
- Donate button/link
- Final test pass

---

### What to leave out of v1.0

- OLE Summary Information Stream (v1.1)
- CLI (v1.1)
- Version update check (v1.1)
- "Missing files" check (v1.1 — low value, PatchCleaner's implementation was buggy)
- "Still used" details window (v1.1 — the count is enough for v1.0)

---

## User feedback on this analysis (26 Feb 2026)

### a) OLE Summary Information Stream
User wants feature parity with PatchCleaner by release. OLE Summary Information Stream should be included in v1.0, not deferred. There's time before 3 March.

### b) Office
Office should "just work" — the MSI API approach should handle modern Office correctly. Don't special-case Office 2010 but do make sure the general approach is solid for all current Windows Installer products.

### c) Distribution
User prefers small exe size (PatchCleaner is 1.26 MB). Leaning towards framework-dependent rather than trimmed single-file. Most users are on Windows 11 which likely has .NET 8 runtime available.

### d) CLI
User wants CLI included, at least in a later release. Keep the architecture CLI-friendly.
