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

## Origin

This project started by looking at PatchCleaner and asking: can we make a
better version? PatchCleaner works fine — this is not fixing something broken.
The improvements are: no VBScript dependency, open source (MIT), .NET 8,
potentially more accurate detection via MSI API rather than WMI.

The full research brief is at `docs/project-brief.md`. Read it before
making significant design decisions. The current app (v0.1.0-alpha) has the
right data layer but a completely wrong UI and a broken Phase 2 scan that
takes 3+ minutes.

## Known open questions (see docs/project-brief.md section 8 for full list)

- Does Phase 2 (component scan, the slow part) need to exist at all?
- Should there be a Delete button or move-only?
- Office 2010 SP2 false positive — does our MSI API approach fix this?
- MSI OLE Summary Information Stream — implement or skip?

## Opus sessions

Opus is available whenever it would give better results. Sonnet will write a
detailed prompt for Opus at the time. The Opus prompt template lives at
`docs/opus-prompt.md`.

---

## Other No faff projects (for context)

| Tool | Location | Stack |
|---|---|---|
| Simple video downloader | `C:\Simple-claude` | Firefox extension + Python server |
| Transcribe app | `C:\transcribe-app` | PyQt6, faster-whisper, CUDA |
| immijjenerator | `C:\immijjenerator` | PyQt6, diffusers |
