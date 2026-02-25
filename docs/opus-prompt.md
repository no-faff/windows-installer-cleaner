# Prompt for Opus

---

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

## Full brief

[Paste the contents of docs/project-brief.md here]

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
