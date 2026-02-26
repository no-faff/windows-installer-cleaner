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

## What's next

The code is structurally sound. Remaining work is UX and release preparation:

1. **Real-world testing** — run the app on a real machine, click everything, find
   what feels wrong. No substitute for actual usage.

2. **Error states** — what happens when C:\Windows\Installer is inaccessible?
   Move destination full? File locked? Admin rights missing?

3. **Admin elevation** — clear prompting, graceful failure without elevation.

4. **Progress feedback** — long scans need visible progress. UI responsiveness
   during scanning.

5. **Accessibility** — keyboard navigation, screen reader support, high contrast.

6. **Branding** — app icon, version info, about dialog, GitHub link.

7. **Distribution** — single-exe publish, installer vs portable, auto-update.

## How to run

```
dotnet run --project src/SimpleWindowsInstallerCleaner
```

Needs admin rights to scan C:\Windows\Installer properly. To run as admin,
open an elevated terminal (right-click PowerShell → Run as administrator)
then run the command above.
