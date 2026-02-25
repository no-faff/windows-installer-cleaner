I want to build a better open-source replacement for PatchCleaner
(homedev.com.au/free/patchcleaner), a Windows utility last released on
3 March 2016 that cleans up orphaned .msi and .msp files from
C:\Windows\Installer. I want to release on 3 March 2026 — exactly 10 years
later.

I've done significant research and already have a working data layer. I've
written an exhaustive brief (below) covering PatchCleaner's features,
its known bugs, my current app's state, what's wrong with it, and a list
of things I genuinely don't know.

**I'm not asking you to validate my thinking. I'm asking you to:**

1. Read everything carefully and tell me what I've missed, got wrong, or not
   thought about deeply enough
2. Tell me whether my approach (MSI P/Invoke API instead of WMI/VBScript) is
   actually better or just different
3. Give me your honest view on each open question in section 8
4. Design the best possible simple app that does what PatchCleaner does but
   better — including the right UX, the right features, and what to leave out
5. Produce a concrete implementation plan that Sonnet can execute

The goal is a simple app that just works. PatchCleaner has 870,000+ downloads
because it solves a real problem simply. Don't make it complicated. But do
make it genuinely better where "better" can be justified.

Be sceptical of my assumptions. Be direct about things that are wrong.
Tell me if any of my "improvements" are actually not improvements.

---

# Simple Windows installer cleaner — exhaustive project brief

> Written for handoff to Opus. This document captures everything known
> about PatchCleaner, our current app, what's wrong, and what's uncertain.
> It is deliberately not a finished plan — that's Opus's job.

---

## 1. Background and objective

PatchCleaner (homedev.com.au) was last released on 3 March 2016. We want to
release a better open-source replacement on 3 March 2026 — exactly 10 years
later.

The target user is the same as PatchCleaner's: a non-technical Windows user
who has noticed their C:\Windows\Installer folder is large and wants to safely
reclaim the space. 870,000+ downloads shows there is demand.

The user of *our* app uses PatchCleaner and it works fine for them. The goal
is not to fix something broken — it is to make a genuinely better version and
release it as open source.

---

## 2. PatchCleaner — complete feature inventory

### 2.1 Main window (656×238 px — very small)

- Three summary lines:
  - "86 files still used, 2.36 Gb   details..."
  - "0 files are excluded by filters, 0.00 B"
  - "1 files are orphaned, 8.05 Mb   details..."
- "details..." is a clickable link opening a separate window
- Move Location: text field showing the saved destination path (e.g. D:\PatchCleaner)
- Browse button — opens folder picker, persists between sessions
- Delete button — permanently deletes ALL orphaned files (not excluded ones)
- Move button — moves ALL orphaned files to Move Location
- Refresh button — re-runs the scan
- Donate button (heart/$ icon, bottom left) — links to PayPal
- Settings button (gear icon, bottom right)
- Info button (ℹ icon, bottom right)
- URL link at bottom: http://www.homedev.com.au

### 2.2 Details — Products window

Shows two panels:
- **Products** list (top): Name, File Name, File Size, Patches count
  - Lists all products whose MSI is still registered/needed
- **Patches** list (bottom left): File Name column
  - Shows patches associated with the selected product
- **Product Details** panel (bottom right):
  - Author, Title, Subject, Digital Signature, File Size, Comment
  - These come from reading the MSI file's OLE Summary Information Stream
- Close button

### 2.3 Details — Orphaned Files window

- **File list** (left): File Name column
  - Highlighted items = excluded by filters
- **Details panel** (right):
  - Author, Title, Subject, Digital Signature, File Size, Comment
  - Same OLE Summary Information Stream data as above
- Close button

### 2.4 Settings window

- **Deep Scan** toggle (On/Off radio buttons)
  - "The deep scan reads the contents of digital certificates to find extra
    information to allow deeper filtering, but it uses memory and kills
    performance. You can turn it off here but it will mean you may get false
    positives."
  - When off: exclusion filters cannot match on digital certificate data
- **Exclusion Filter**
  - Text input + add (+) and remove (×) buttons
  - List of current filters — pre-populated with "Acrobat"
  - Applied as case-insensitive "contains" check against: Author, Title,
    Subject, Digital Signature of each orphaned file
  - Excluded files appear in the "excluded" count, shown highlighted in the
    Orphaned Files details window but NOT offered for delete/move
- **Perform Missing Files Check on startup** checkbox
  - When enabled: checks that registered MSI/MSP files actually exist on disk;
    flags any that are missing
- Save / Cancel buttons

### 2.5 Command-line interface (added v1.3.0)

    /d              delete action
    /m              move to saved default location
    /m [FilePath]   move to specified path

Output goes to console and Windows Event Log.

### 2.6 Version checking

On startup, checks homedev.com.au for newer version; shows download link if
one is available.

---

## 3. PatchCleaner — algorithm

Uses VBScript to make WMI calls:
- Win32_Product → gets list of products and their LocalPackage paths
- Win32_SoftwarePatch (or similar) → gets patch paths
- Compares resulting path list against all .msi/.msp files in C:\Windows\Installer
- Anything on disk but not in the WMI list = orphaned

The "deep scan" additionally opens each MSI/MSP file as an OLE Structured
Storage document and reads the Summary Information Stream (IPropertySetStorage)
to extract: Author, Title, Subject, Comments, and Digital Signature information.
This is used by the exclusion filter.

---

## 4. PatchCleaner — known issues and limitations

### 4.1 False positives (files wrongly called orphaned)

- **Adobe Acrobat/Reader**: Adobe's auto-updater uses multiple prior patch
  versions. PatchCleaner only retains the "last" one; removing earlier patches
  breaks Adobe's updater. Worked around with the "Acrobat" exclusion filter.

- **Office 2010 Service Pack 2**: A large (~450MB) MSP file for Office 2010
  SP2 is incorrectly flagged as orphaned. If deleted, Windows Update for
  Office 2010 stops working entirely. Office 2013+ is unaffected (uses
  Click-to-Run). This false positive was never properly fixed.

- **Patches registered only in registry**: Some patches are registered under
  `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Patches`
  but not returned by WMI. PatchCleaner calls these orphaned; they may not be.
  State=2 patches are inactive, but the dev's explanation was not fully
  convincing to the reporter.

- **Pending file rename operations**: If Windows Updates have installed but not
  yet rebooted, some files are in a "pending rename" state. Running PatchCleaner
  before reboot could be unsafe. A user suggested checking
  `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\PendingFileRenameOperations`
  before scanning; the dev added it to the backlog but never shipped it.

### 4.2 Technical/reliability issues

- **VBScript dependency**: Broken VBScript = app completely non-functional.
  Common on heavily locked-down corporate machines and some misconfigured
  consumer machines.
- **Crash history**: Required 4 hotfix releases in Feb-Mar 2016. Crashes
  related to: null DateTime conversion, Shell32 issues, missing files, duplicate
  patches, UI object not found, non-English Windows.
- **No code signing**: Triggers SmartScreen warnings. Developer couldn't
  justify cost for free software.
- **.NET Framework 4.5.2**: Old. Works on Win 10/11 but relies on inbox .NET.
- **Non-English Windows**: Had bugs fixed in v1.4.1 but history of issues.
- **CLI output redirection broken**: `patchcleaner /d > log.txt` doesn't work.
  Process hangs when run under SYSTEM account (SCCM/PSExec).
- **Upgrade instability**: Direct upgrade from v1.3→v1.4 left stale files,
  caused IndexOutOfRangeException on startup. Required uninstall+reinstall.
- **Settings not persisted between versions**: Move location was not saved
  between sessions in early versions (fixed v1.2).

### 4.3 Functionality gaps

- No per-file selection — Delete/Move operates on ALL orphaned files at once
- No sorting or filtering of the file lists
- No way to see which product "owns" a still-used file from the orphan details
- CLI: output redirection doesn't work; hangs under SYSTEM account
- No indication of scan progress (just shows a "searching" splash screen)

---

## 5. Our current app — what exists

### 5.1 Data layer (solid, should keep)

```
src/SimpleWindowsInstallerCleaner/
  Interop/
    MsiConstants.cs          — enums, error codes, MSI constants
    MsiNativeMethods.cs      — P/Invoke: MsiEnumProductsEx, MsiEnumPatchesEx,
                               MsiEnumComponentsEx, MsiGetProductInfoEx,
                               MsiGetPatchInfoEx, MsiGetComponentPathEx
  Models/
    RegisteredPackage.cs     — record: LocalPackagePath, ProductName,
                               ProductCode, IsAdobeWarning
    OrphanedFile.cs          — record: FullPath, SizeBytes, IsPatch,
                               IsAdobeWarning; computed: FileName, SizeDisplay
  Services/
    IInstallerQueryService.cs
    InstallerQueryService.cs — full MSI API implementation (548 lines)
    IFileSystemScanService.cs
    FileSystemScanService.cs — cross-references disk vs registered
    IMoveFilesService.cs
    MoveFilesService.cs      — moves files, handles name collisions
  ViewModels/
    OrphanedFileViewModel.cs — wraps OrphanedFile, adds IsSelected
    MainViewModel.cs         — scan, select all/none, choose destination, move
```

### 5.2 UI (currently wrong)

- Large window with a Scan button (user must click to start)
- Full ListView of orphaned files with checkboxes and columns
- Status bar with Select all / Select none / Choose destination / Move selected
- Progress overlay during scan

### 5.3 Test coverage

7 passing unit tests covering:
- FileSystemScanService: orphan detection, case-insensitive path matching,
  Adobe warning propagation
- MoveFilesService: move to destination, name collision handling (appends " (1)"),
  error handling for missing source file

### 5.4 Tech stack

- C# 12, WPF, .NET 8 Windows
- CommunityToolkit.Mvvm 8.x
- xUnit + Moq
- app.manifest: requireAdministrator

---

## 6. What's wrong with the current app

### 6.1 Critical: scan performance

InstallerQueryService has two phases:

**Phase 1** (fast): MsiEnumProductsEx → product codes → MsiGetProductInfoEx
for LocalPackage path → MsiEnumPatchesEx for each product.

**Phase 2** (broken): MsiEnumComponentsEx → for each component whose path is
in C:\Windows\Installer → calls FindOwningProduct() → which calls
MsiEnumProductsEx again from scratch for every component.

This is O(components_in_installer × total_products) MSI API calls. On a
typical machine with hundreds of products and thousands of components this can
take many minutes. The user's machine took 3+ minutes with no end in sight.

PatchCleaner does not do anything equivalent to Phase 2. It uses WMI which
returns a flat list of registered packages; it then relies on exclusion filters
to handle Adobe.

### 6.2 Wrong UX

- Should auto-scan on startup (like PatchCleaner), not require a Scan button
- Should show a compact summary, not a full list as the main view
- Full list should be in a details popup
- Should be small (PatchCleaner is 656×238 px)

### 6.3 Missing features vs PatchCleaner

- No "files still used" count and details
- No "excluded files" count
- No exclusion filters / settings page
- No delete option (deliberate design decision — may be wrong)
- No Refresh button
- No donate link
- No details views (Author/Title/Subject/Digital Signature/Comment per file)
- No reading of OLE Summary Information Stream from MSI files
- No "missing files" check
- No pending-reboot check (PendingFileRenameOperations)
- No version checking / update notification
- No CLI
- Move location not persisted between sessions

### 6.4 Things deliberately left out (may need revisiting)

- **Delete option**: CLAUDE.md says "move, don't delete". PatchCleaner has
  both. The question is whether removing Delete is genuinely safer UX or
  just removes a feature users expect.

---

## 7. Approach differences from PatchCleaner

### 7.1 MSI API vs WMI

We use MsiEnumProductsEx / MsiGetProductInfoEx / MsiEnumPatchesEx via P/Invoke.
PatchCleaner uses WMI (via VBScript).

**Claimed advantage**: MSI API is more accurate. WMI (Win32_Product) has a
known side effect of repairing installations when queried. MSI API does not.
Also no VBScript dependency.

**Uncertainty**: We haven't verified whether our approach actually catches the
Office 2010 SP2 case that PatchCleaner gets wrong. We don't know if there are
cases where WMI returns registrations that MsiEnumProductsEx doesn't, or
vice versa.

### 7.2 Adobe handling

PatchCleaner: exclusion filter (pre-configured "Acrobat") — Adobe files are
completely hidden from the user.

Our approach (planned): show Adobe-related orphans with a warning label "check
before moving". User can still see them and make a choice.

**Uncertainty**: Our Phase 2 (component scan) was the mechanism for detecting
Adobe files. If we drop Phase 2 (which we probably should given the
performance), we need a different way to identify files that might be Adobe.
Options: name-based heuristic, reading MSI metadata, or just adopting
exclusion filters like PatchCleaner.

### 7.3 Reading MSI file metadata

PatchCleaner reads Author/Title/Subject/Comment/Digital Signature from each
MSI/MSP file via OLE Structured Storage (IPropertySetStorage / Summary
Information Stream). We don't do this at all.

This is needed for:
- The details view (showing what each file is)
- The exclusion filter (matching on those fields)
- Possibly: better Adobe/Office detection

Implementing this requires either COM interop (IStorage, IPropertySetStorage)
or a managed library. It is non-trivial P/Invoke work.

---

## 8. Open questions (things I genuinely don't know)

1. **Does MSI API (MsiEnumProductsEx) catch the Office 2010 SP2 MSP file
   correctly?** If not, we have the same false positive as PatchCleaner.

2. **Should we drop Phase 2 entirely?** Phase 2 was intended to handle Adobe
   registrations via component records. PatchCleaner doesn't do this and
   handles Adobe with exclusion filters instead. What does Opus think is
   the right approach?

3. **Delete button — yes or no?** Safety argument for no: user could
   accidentally delete something needed. Counterargument: PatchCleaner has
   it, users expect it, and the move-first recommendation is already in the
   UI text. What's the right call?

4. **MSI OLE Summary Information Stream** — is this worth implementing?
   It unlocks: proper details views, exclusion filter matching, better Adobe
   identification. But it's significant P/Invoke work.

5. **Exclusion filters vs warnings** — which is better UX? PatchCleaner
   hides excluded files (bad: user doesn't know they exist). Our warning
   approach shows them but flags them. Is there a better model?

6. **Pending reboot check** — should we warn if PendingFileRenameOperations
   is set? PatchCleaner never shipped this but it seems like a good safety
   feature.

7. **Office 2010 / older products** — is there a reliable way to detect that
   removing a particular MSP would break Windows Update for Office 2010?
   Or do we just have to document "if you have Office 2010, be careful"?

8. **Distribution** — MSIX? Self-contained single-file exe? Installer? The
   self-contained exe approach (.NET 8 publish) is simplest but large (~60MB).
   Framework-dependent is small but requires .NET 8 runtime.

9. **Code signing** — free options exist (e.g. SignPath.io free tier for OSS,
   or a self-signed cert that at least shows a publisher name rather than
   "unknown"). Worth investigating.

10. **CLI** — PatchCleaner added it in v1.3 and it was popular (SCCM
    deployments etc) but had bugs. Is it in scope for our v1.0?

11. **Auto-scan on startup** — PatchCleaner does this. Is there a case for
    NOT doing it? (e.g. first run with no move location set, or machine
    with very many products where scan is slow)

12. **Settings persistence** — where to store? AppData\Roaming? Registry?
    AppData\Local? User preference?

13. **Non-English Windows** — PatchCleaner had bugs with this. Does our
    MSI API approach have similar issues? Path separators, locale-dependent
    string handling?

14. **Windows Server support** — PatchCleaner worked on Server 2008 R2 /
    2012 R2. Do we need to support Windows Server? .NET 8 supports it.

15. **What happens when the app is run without admin elevation?** Currently
    the manifest forces elevation. But what's the graceful failure if the
    user somehow bypasses this? PatchCleaner had crashes in this scenario.

---

## 9. Technical constraints and facts

- **Language**: C#
- **UI**: WPF, .NET 8
- **Target OS**: Windows 10/11 (possibly Server)
- **Release date**: 3 March 2026 (hard deadline)
- **Licence**: MIT open source
- **Distribution**: under the "No faff" GitHub org (github.com/no-faff)
- **Donate button**: yes, like PatchCleaner (PayPal or similar)
- **British English**, sentence case throughout
- Admin elevation required (app.manifest already set)
- Current data layer uses P/Invoke (not WMI) — this is an advantage

---

## 10. What "better" means (our working definition)

This should be revisited by Opus — these are working assumptions:

1. No VBScript/WMI dependency — more reliable on locked-down machines
2. Open source (MIT) — PatchCleaner is closed-source donationware
3. Modern .NET 8 — no .NET 4.5.2 requirement
4. More accurate orphan detection (MSI API vs WMI) — unverified
5. Better Adobe handling — warn rather than silently exclude (debatable)
6. Possibly: reads pending reboot state before scanning
7. Possibly: fixes the Office 2010 SP2 false positive
8. Same simple UX as PatchCleaner — small window, auto-scan on startup
9. Code signed (if feasible for free/OSS)

---

## 11. What I haven't thought about (honest gaps)

- I don't know if there are other products besides Adobe and Office 2010 that
  cause false positives with either PatchCleaner's approach or ours
- I don't know the exact WMI query PatchCleaner uses (raymond.cc article was
  inaccessible via Wayback Machine)
- I don't know how PatchCleaner handles per-user vs per-machine installations
  (our code handles both)
- I haven't thought about what happens on machines with hundreds of user
  profiles (our AllUsersSid scan — is that right for all cases?)
- I don't know if reading MSI Summary Information Stream is the right approach
  for file identification or if there's something better
- I haven't thought through the full first-run experience
- I haven't thought about whether the app should handle MST (transform) files,
  which appear in the Installer folder alongside MSI/MSP
- I don't know if there are other file types in C:\Windows\Installer besides
  .msi and .msp that matter
- I don't know what the right window size/layout is for modern Windows 11
  (PatchCleaner's 656×238 may feel tiny on a 4K display)

---

## 12. Current git state

Branch: main
Tag: v0.1.0-alpha (the broken prototype described above)

The data layer is solid. The UI prototype exists but is wrong. All 7 unit
tests pass. The app technically builds and runs but the scan takes 3+ minutes
and the UX is completely wrong.

---

## Current data layer (for reference)

The following files exist and work correctly:

**InstallerQueryService.cs** (548 lines) — P/Invoke to MSI API:
- Phase 1: MsiEnumProductsEx → per product: MsiGetProductInfoEx for
  LocalPackage path, then MsiEnumPatchesEx for each product's patches
- Phase 2: MsiEnumComponentsEx → for components whose path is in
  C:\Windows\Installer → FindOwningProduct (THIS IS THE SLOW PART — it
  re-enumerates all products from scratch for each component)
- Handles per-machine and per-user installations (AllUsersSid = "s-1-1-0")
- Handles both .msi products and .msp patches

**FileSystemScanService.cs** — enumerates C:\Windows\Installer, calls
InstallerQueryService, cross-references, returns OrphanedFile list.

**MoveFilesService.cs** — moves files, handles name collisions (appends " (1)")

**Models**: RegisteredPackage (product/patch from MSI API), OrphanedFile
(file on disk that's not registered)

**7 unit tests passing** (FileSystemScanService and MoveFilesService)

---

## The one thing I'm most uncertain about

Phase 2 of the scan (the component enumeration — the slow bit) was added
to handle Adobe files that register via component records rather than
INSTALLPROPERTY_LOCALPACKAGE. PatchCleaner doesn't do this — it just uses
exclusion filters. I don't know if Phase 2 is necessary, clever, or a
mistake. Opus: what do you think?
