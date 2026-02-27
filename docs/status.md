# Project status — 27 February 2026

## What's done

Three rounds of development and review are complete:

- **Round 1** (16 tasks): Full UI redesign — scanning, orphan detection, move/delete,
  settings, exclusion filters, registered files window, orphaned files window
- **Round 2** (9 tasks): Polish — shared helpers, service extraction, master-detail
  layouts, metadata display, exclusion filter matching, publish profile
- **Round 3** (5 tasks + 1 follow-up): Code review fixes — XAML trigger bugs,
  dead markup, proper pluralisation, ViewModel tests, catch block narrowing

**22 tests passing, 0 warnings.**

## Architecture

MVVM with CommunityToolkit.Mvvm. No DI container — manual composition in App.xaml.cs.

Key services: `FileSystemScanService`, `MoveFilesService`, `DeleteFilesService`,
`ExclusionService`, `SettingsService`, `PendingRebootService`, `MsiFileInfoService`.

Three windows: `MainWindow` (scan + actions), `RegisteredFilesWindow` (products/patches/details),
`OrphanedFilesWindow` (file list + metadata details).

## Bugs found during first real-world test (27 Feb)

- **Infinite loop without admin** (FIXED, 0d2751c): `MsiEnumProductsEx` returns
  `ERROR_ACCESS_DENIED` instead of `ERROR_NO_MORE_ITEMS` when not elevated,
  causing the enumeration loop to spin forever. Now throws
  `UnauthorizedAccessException` which the existing handler catches cleanly.

## What's next

The code is structurally sound. Remaining work is UX, real-world testing and
release preparation:

1. **Continue real-world testing** — compare results against PatchCleaner.
   Does ours find the same orphans? Is scan speed acceptable?

2. **Error states** — move destination full, file locked, disk read errors.

3. **Progress feedback** — PatchCleaner scans in ~2 seconds. Ours should be
   comparable. If not, profile and optimise the MSI API calls.

4. **Accessibility** — keyboard navigation, screen reader support, high contrast.

5. **Branding** — app icon, version info, about dialog, GitHub link.

6. **Distribution** — single-exe publish, installer vs portable, auto-update.

## How to run

**Development (from elevated terminal):**
```
dotnet run --project src/SimpleWindowsInstallerCleaner
```
Note: `dotnet run` does not trigger the UAC prompt — the `dotnet.exe` process
is the one being launched, not our exe. Run from an already-elevated terminal.

**Production (from Explorer or non-elevated terminal):**
```
src/SimpleWindowsInstallerCleaner/bin/Debug/net8.0-windows/SimpleWindowsInstallerCleaner.exe
```
Running the compiled `.exe` directly triggers the Windows UAC elevation prompt
automatically (the app manifest embeds `requireAdministrator`).
