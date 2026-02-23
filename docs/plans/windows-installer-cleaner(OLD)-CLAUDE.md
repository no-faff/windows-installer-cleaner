# Simple Windows installer cleaner

A modern, open source replacement for PatchCleaner (last released 3/3/2016).
Target release: 3 March 2026 — 10 years to the day.

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
- **UI:** WPF (.NET 8)
- **Windows API:** Windows Installer COM (msi.dll) via P/Invoke
  - `MsiEnumProducts()`, `MsiGetProductInfo()`, `MsiSourceListEnumSources()`
  - `MsiEnumPatches()` for .msp patch files
- **Distribution:** TBD (standalone .exe vs requiring .NET runtime)
- **Licence:** Open source (MIT or similar)

---

## Brand / conventions

- **Studio:** No faff (`github.com/no-faff`)
- **Tool name:** Simple Windows installer cleaner
- **Sentence case throughout** — no title case anywhere
- **British English** — colour, organise, etc.
- No Oxford comma

---

## Environment

- Platform: Windows 11
- Terminal: PowerShell — use `$env:VAR = "value"` not `export`
- IDE: TBD

---

## Key decisions

- Move orphaned files (don't delete) as the safe default — user can delete from the move destination later
- Adobe products can appear orphaned when they aren't — flag these with a warning
- Show both products (.msi) and patches (.msp)
- Must run as administrator to access Windows Installer API fully

---

## Opus sessions

Use Opus for:
- Windows Installer API data layer (MsiEnumProducts, MsiSourceListEnumSources etc.)
- Any complex P/Invoke interop work

Opus prompt for the API data layer:
```
We're building Simple Windows installer cleaner — a C# / WPF (.NET 8) app that
scans C:\Windows\Installer, queries the Windows Installer COM API to enumerate
all registered .msi/.msp files, cross-references against files physically on
disk, and presents orphaned files for safe move or deletion.

Your job: design the data layer. Specifically how to use MsiEnumProducts(),
MsiGetProductInfo(), MsiSourceListEnumSources(), and related P/Invoke calls to
build a complete list of files Windows Installer considers registered. Account
for both products and patches (.msp). The result should be a clean C# service
class I can build the WPF UI against.
```

---

## Other No faff projects (for context)

| Tool | Location | Stack |
|---|---|---|
| Simple video downloader | `C:\Simple-claude` | Firefox extension + Python server |
| Transcribe app | `C:\transcribe-app` | PyQt6, faster-whisper, CUDA |
| immijjenerator | `C:\immijjenerator` | PyQt6, diffusers |
