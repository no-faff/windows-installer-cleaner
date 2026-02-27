# Project status — 27 February 2026

## What's done

Seven rounds of development and review are complete:

- **Round 1** (16 tasks): Full UI redesign — scanning, orphan detection, move/delete,
  settings, exclusion filters, registered files window, orphaned files window
- **Round 2** (9 tasks): Polish — shared helpers, service extraction, master-detail
  layouts, metadata display, exclusion filter matching, publish profile
- **Round 3** (5 tasks + 1 follow-up): Code review fixes — XAML trigger bugs,
  dead markup, proper pluralisation, ViewModel tests, catch block narrowing
- **Round 4** (6 tasks): UX improvements — about dialog, auto-select first item,
  exclusion row visibility, scan overlay suppression, scan duration display,
  version in title bar
- **Round 5** (6 tasks): Keyboard/mouse fixes in orphaned files window, full digital
  signatures, scrollable detail panels, inline filter names on excluded row,
  startup splash screen with 5-step progress, UAC verification
- **Post-round 5 fixes**: Dialog owner lifecycle fix (969605b), splash minimum
  display time (af6ad34)
- **Round 6** (4 tasks): All splash steps visible with uniform 400ms delays,
  Adobe exclusion filter explanation in settings window, shared scan logic
  extracted to `RunScanCoreAsync`, status.md updated
- **Round 7** (11 tasks): Full UI redesign — light/dark theme resource dictionaries
  with design token system, Windows system theme detection (ThemeService reads
  `AppsUseLightTheme` registry key), custom window chrome with hand-built title bar
  on all windows, rounded-corner card layouts, accent-blue splash screen with
  determinate progress bar, structured move/delete progress overlay with file count
  and percentage. Zero hardcoded colours outside theme dictionaries.

**22 tests passing, 0 warnings.**

## Architecture

MVVM with CommunityToolkit.Mvvm. No DI container — manual composition in App.xaml.cs.

Key services: `FileSystemScanService`, `MoveFilesService`, `DeleteFilesService`,
`ExclusionService`, `SettingsService`, `PendingRebootService`, `MsiFileInfoService`,
`ThemeService` (static, called once at startup).

Six windows: `MainWindow` (scan + actions), `RegisteredFilesWindow` (products/patches/details),
`OrphanedFilesWindow` (file list + metadata details), `SettingsWindow`, `AboutWindow`,
`SplashWindow`.

Theming: `Themes/Light.xaml` and `Themes/Dark.xaml` resource dictionaries loaded at
startup based on Windows system setting. `DynamicResource` for brushes, `StaticResource`
for typography/spacing/styles. Custom title bar via `Controls/TitleBar.xaml` UserControl
with `WindowChrome`.

Startup: theme applied → splash screen shown → `ScanWithProgressAsync` runs → main
window shown, splash closed. Splash drives its own step transitions explicitly (not
from service progress messages). Refresh uses `ScanAsync` relay command.

## Bugs found and fixed

- **Infinite loop without admin** (FIXED, 0d2751c): `MsiEnumProductsEx` returns
  `ERROR_ACCESS_DENIED` instead of `ERROR_NO_MORE_ITEMS` when not elevated,
  causing the enumeration loop to spin forever. Now throws
  `UnauthorizedAccessException` which the existing handler catches cleanly.
- **5-minute scan hang** (FIXED, 4189b8b): `GetEnumeratedSid` made 2 extra
  `MsiEnumProductsEx` calls per product. Fixed with pre-allocated SID buffer.
- **Dialog owner crash** (FIXED, 969605b): WPF sets `Application.MainWindow`
  to the first window shown (the splash). After splash closes, dialogs with
  `Owner = Application.Current.MainWindow` crashed. Fixed by setting
  `Application.Current.MainWindow = window` explicitly before showing main window.
- **Splash invisible on fast machines** (FIXED, af6ad34 + round 6): Scan
  completes in under 200ms. Fixed with explicit per-step delays (400ms each).

## What's next

Remaining practical work:

1. **Real-world testing** — compare results against PatchCleaner
2. **Error states** — move destination full, file locked, disk read errors
3. **Accessibility** — keyboard navigation, screen reader support, high contrast
4. **Distribution** — single-exe publish, installer vs portable, auto-update

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
