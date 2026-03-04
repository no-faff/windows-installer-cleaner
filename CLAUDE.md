# InstallerClean

A modern, open source replacement for PatchCleaner (last released 3/3/2016).
Released: 3 March 2026 — 10 years to the day.

Part of the **No faff** suite of small Windows utilities (github.com/no-faff).

---

## What it does

1. Scans `C:\Windows\Installer` for `.msi` and `.msp` files
2. Queries the Windows Installer API to enumerate all registered/needed files
3. Cross-references — anything not registered = orphaned
4. Presents orphaned files for safe move or deletion

The "move, don't delete" approach is the key safety feature — files go to a
user-chosen location so they can be restored if anything breaks.

---

## Tech stack

- **Language:** C#
- **UI:** WPF (.NET 8), dark theme inspired by Upscayl
- **Font:** Poppins (bundled)
- **Windows API:** Windows Installer COM (msi.dll) via P/Invoke
  - `MsiEnumProductsEx()`, `MsiGetProductInfo()`, `MsiSourceListEnumSources()`
  - `MsiEnumPatches()` for .msp patch files
- **Licence:** MIT

---

## Brand / conventions

- **Studio:** No faff (`github.com/no-faff`)
- **App name:** InstallerClean
- **British English** — colour, organise, etc.
- No Oxford comma
- No em dashes in UI text
- No gradient "AI slop" — solid colours only

---

## Environment

- Platform: Windows 11
- Terminal: PowerShell — use `$env:VAR = "value"` not `export`
- Build: `dotnet build src/InstallerClean/InstallerClean.csproj`
- Test: `dotnet test src/InstallerClean.Tests/`
- Run (elevated terminal): `dotnet run --project src/InstallerClean`
- Run (explorer): `src/InstallerClean/bin/Debug/net8.0-windows/InstallerClean.exe` (triggers UAC)

---

## Architecture

MVVM with CommunityToolkit.Mvvm. Manual composition in App.xaml.cs (no DI container).

**Startup flow:** splash screen → scan → main window. Dark titlebar set via
DwmSetWindowAttribute class handler. App icon set on all windows via same handler.

**Services:** FileSystemScanService, InstallerQueryService, MoveFilesService,
DeleteFilesService, ExclusionService, SettingsService, PendingRebootService,
MsiFileInfoService.

**Windows:** MainWindow, RegisteredFilesWindow, OrphanedFilesWindow,
SettingsWindow (titled "Filters"), AboutWindow, SplashWindow,
ConfirmDeleteWindow, ConfirmMoveWindow.

**Styles (App.xaml):** Pill buttons (AccentPill, PrimaryPill, GhostPill),
PillTextBox, Card/CardNoPad, LinkButton, SubtleLink, WarningTooltip,
custom thin ScrollBar (8px, rounded), dark ListViewItem/ListBoxItem selection.

**Text colour tiers:**
| Tier | Colour | Usage |
|------|--------|-------|
| Heading | #f8fafc | Titles, headings, primary labels |
| Body | #cbd5e1 | Body text, descriptions, section labels |
| Muted | #94a3b8 | Secondary info, size displays, footer |
| Dim | #64748b | Metadata labels in detail panels |

**22 tests passing (xUnit + Moq).**

---

## Key decisions

- Move orphaned files (don't delete) as the safe default
- Delete sends to Recycle Bin (not permanent)
- Adobe/Acrobat 32-bit filter — their patches appear orphaned but aren't
- Move and delete both have confirmation dialogs
- Cancel button on move/delete progress overlay
- Exclusion filtering runs on background thread (async)
- Atomic settings save (write .tmp then rename)
- Must run as administrator to access Windows Installer API fully

---

## Commit conventions

- Prefixes: `feat:` / `fix:` / `refactor:` / `chore:` / `test:` / `docs:`
- Always: `dotnet test` + `dotnet build` before committing

---

## Project structure

```
src/InstallerClean/                         # Main WPF app
src/InstallerClean.Tests/                   # xUnit tests
docs/                                       # Briefs, plans, screenshots
docs/icons/                                 # Icon source files
```

---

## Remaining work

- **README** for GitHub launch
- **GitHub repo** setup (fresh clean repo under no-faff org)
- **Portfolio:** ko-fi/PayPal, Reddit account, landing pages
- Extract MessageBox from ViewModels (testability)
- Scan subdirectories of C:\Windows\Installer
- More test coverage
