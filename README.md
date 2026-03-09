[![Licence: MIT](https://img.shields.io/badge/licence-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![CI](https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg)](https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml)
[![Windows 10/11](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg)](https://github.com/no-faff/InstallerClean/releases)
[![VirusTotal](https://img.shields.io/badge/VirusTotal-0%2F70-brightgreen.svg)](https://www.virustotal.com/gui/file/3d1cb9368fcabc9cf674b437ae32eb67488b09647077281d36665f2985350827)
[![GitHub Release](https://img.shields.io/github/v/release/no-faff/InstallerClean)](https://github.com/no-faff/InstallerClean/releases/latest)

# InstallerClean

**Safely clean up `C:\Windows\Installer`, the hidden Windows folder that quietly eats your disk space.**

![Screenshot of InstallerClean](docs/InstallerClean-done.png)

- **What:** Finds and removes unneeded files from `C:\Windows\Installer`, the hidden folder Windows never cleans up.
- **How much space:** Depends on your software. People report 20-50 GB; with Adobe Acrobat it can pass 100 GB.
- **Is it safe:** Yes. Only removes files Windows itself says it no longer needs. Delete sends to Recycle Bin. Move lets you keep them somewhere safe.
- **Get it:** [Download the latest release](../../releases/latest), run it, done.

---

## The folder nobody tells you about

There's a hidden folder on every Windows PC called `C:\Windows\Installer`. Every time you install software that uses the Windows Installer system, or apply a patch to Microsoft Office, Adobe Acrobat, Visual Studio or any other `.msi`-based application, a copy of that installer or `.msp` patch file goes into this folder. And stays there.

When you uninstall the software, the files stay. When a newer patch replaces an older one, both stay. Windows never cleans them up. Disk Cleanup doesn't touch them. DISM is for a different folder entirely. Over the years, the folder grows: 10 GB, 30 GB, 50 GB. On machines with Adobe Acrobat, it can reach [well over 100 GB](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/).

These aren't temp files that get recreated the moment you close a cleaning tool. They're genuine dead weight: old installers from software you uninstalled years ago and patches that have been replaced three times over. Once they're gone, they don't come back.

**If you're looking for an easy way to free up disk space on Windows, this folder is one of the best places to start.** InstallerClean finds the unneeded files and removes them safely.

## The search for help

If you've ever searched for help with this folder, you know how it goes. Someone asks how to clean it. They're told to run Disk Cleanup. They try it. It frees up [600 MB of a 180 GB folder](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb). The thread goes quiet.

> *"All of the threads I've found tend to recommend the same things which don't solve the problem, and then go dead."*
>
> ksparks519, r/Windows10

Or they're told not to touch it at all. In one thread, someone with a 60 GB Installer folder was told to ["don't mess with it."](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/) When they asked what they should do instead, the reply was: *"I just told you."*

The standard advice confuses deleting files at random (which genuinely is dangerous) with removing files that Windows itself says it no longer needs (which isn't). InstallerClean does the latter.

[PatchCleaner](https://www.homedev.com.au/free/patchcleaner) was the answer for a long time, and it helped a lot of people. But it hasn't been updated since 2016, it's closed source, and it excludes Adobe files by default. On machines where Adobe is the biggest offender, that means PatchCleaner leaves the vast majority of removable files untouched:

> *"I've downloaded Patchcleaner to delete the orphaned .msp files... 29 GB of the files are 'excluded by filters', so Patchcleaner doesn't seem to help."*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/)

InstallerClean handles Adobe patches properly by detecting which ones have been superseded by newer updates.

## What it does

1. **Scans** `C:\Windows\Installer` for `.msi` and `.msp` files
2. **Queries** the Windows Installer API to find which files are still registered
3. **Shows** what's needed and what's not, with sizes
4. **Removes** the unneeded files: delete to the Recycle Bin, or move to a folder you choose

No wizards, no subscriptions, no account required.

## How it knows what's safe to remove

InstallerClean identifies two kinds of unneeded files:

**Orphaned files** are installers and patches left behind after you uninstall software. Windows no longer references them, but the files sit in the folder taking up space. These are safe to remove.

**Superseded patches** are old `.msp` patches that have been replaced by newer ones. Windows marks them as superseded in its own database but never deletes them. This is especially common with Adobe Acrobat, which delivers roughly 1.1 GB patch files and accumulates superseded ones indefinitely. InstallerClean reads the patch state directly from the Windows Installer API and flags these as removable too.

This is something PatchCleaner can't do. PatchCleaner excludes Adobe by default because Adobe patches appear registered even when they've been superseded. InstallerClean goes deeper: it checks whether a patch has actually been replaced, regardless of the manufacturer.

## Is it safe?

Yes. We query the same database Windows itself uses to track what's installed. If Windows says a file is no longer needed, we trust it. We don't guess based on filenames or dates.

- **Delete** sends files to the Recycle Bin, so you can restore them if needed
- **Move** copies files to a location you choose first, if you'd rather be cautious
- Nothing is touched until you click Delete or Move and confirm
- The app warns you if Windows has pending updates that could affect results
- [VirusTotal scan](https://www.virustotal.com/gui/file/3d1cb9368fcabc9cf674b437ae32eb67488b09647077281d36665f2985350827): 0/70 detections. Source code is all on GitHub

## Getting started

1. Download **InstallerClean-setup.exe** from the [releases page](../../releases/latest) and run the installer
2. Windows SmartScreen may say "Unknown publisher". Click **More info** then **Run anyway**. This is normal for any unsigned open source app. The app requires administrator access
3. The app scans automatically on startup
4. Review the results, then click **Delete** or **Move**. Delete sends to the Recycle Bin, so you can restore if anything goes wrong. Move is safer still - it copies the files somewhere you choose first

> **Prefer not to install?** Download **InstallerClean-portable.exe** instead. It's a single file, no install needed. Just download, run and delete it when you're done.

> **Tip:** If Windows has pending updates, the app will warn you to restart and install them first. A pending update might reference files that appear removable but aren't yet fully registered.

## Compared to PatchCleaner

PatchCleaner has served the community well, and it still works. But ten years on from its last release, InstallerClean picks up where it left off.

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| Last updated | 2026 (active) | 3 March 2016 |
| Source code | Open source (MIT) | Closed source |
| Runtime | .NET 8 (self-contained) | .NET + VBScript |
| API | Windows Installer COM (direct) | WMI (`Win32_Product`) |
| Superseded patch detection | Yes | No |
| Adobe handling | Detects superseded patches | Excludes by default |
| UI | Modern dark theme (WPF) | Windows Forms |
| Data collection | None | None |

> **A note on WMI:** PatchCleaner uses `Win32_Product`, which is known to [trigger MSI repair operations](https://gregramsey.net/2012/02/20/win32_product-is-evil/) during enumeration. InstallerClean calls the Windows Installer COM interface directly with no side effects.

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) also offers Installer cleanup as part of its System Booster module, but it's a paid tool ($15-25) and the cleanup is one small feature inside a much larger application. InstallerClean is free, focused and open source.

## Command line

InstallerClean supports headless operation for scripting and sysadmin use:

```
InstallerClean - clean up C:\Windows\Installer

Usage:
  InstallerClean.exe          Launch the GUI
  InstallerClean.exe /s       Scan only - list removable files
  InstallerClean.exe /d       Delete removable files (Recycle Bin)
  InstallerClean.exe /m       Move to saved default location
  InstallerClean.exe /m PATH  Move to specified path
```

Also accepts `--help`, `/?` and `-h`.

`/s` is a dry run: it scans, lists what it would remove with filenames and sizes, then exits. Useful for auditing before cleanup. Exit code is always 0. All files are in `C:\Windows\Installer`.

`/d` and `/m` scan and then act. `/d` sends removable files to the Recycle Bin. `/m` moves them to a folder (either one you specify on the command line, or the default saved from the GUI). Exit code is 0 on success, 1 if any files failed.

All three require an elevated (administrator) command prompt.

## Features

- **Delete or move.** Delete sends to the Recycle Bin. Move lets you keep files somewhere safe.
- **Superseded patch detection.** Finds patches Windows itself has marked as replaced.
- **Detail views.** Inspect individual files with product name, size, reason and digital signature.
- **Pending reboot detection.** Warns if pending updates might affect scan results.
- **Subfolder cleanup.** Prunes empty subfolders left behind by old installer operations.
- **Command line mode.** `/s` to scan, `/d` to delete, `/m` to move - for scripting and automation.
- **No installer needed.** Download, run, done.
- **No data collection.** Doesn't phone home, collect data or require an account.

## Under the hood

InstallerClean calls the Windows Installer COM interface directly via P/Invoke:

- `MsiEnumProductsEx` to enumerate every installed product across all user contexts
- `MsiEnumPatchesEx` to find all registered patches for each product
- `MsiGetPatchInfoEx` to read patch state (applied, superseded or obsoleted)

Any `.msi` or `.msp` file in `C:\Windows\Installer` that isn't claimed by a registered product is orphaned. Any patch marked as superseded and not required for uninstall is flagged as removable.

If the API returns incomplete data (which can happen with corrupted installer state), we fall back to reading the registry (`HKLM\Software\Microsoft\Windows\CurrentVersion\Installer`). The fallback is conservative: it only adds files to the "still needed" set, never to the "removable" set.

We never call `Win32_Product`. That WMI class triggers MSI consistency checks on every installed product during enumeration.

## Requirements

- Windows 10 or 11
- Administrator privileges (to access `C:\Windows\Installer`)
- The setup installer and portable exe are around 72-76 MB because they bundle the .NET 8 runtime so nothing else needs to be installed. Choose portable unless you want an installer
- Already have [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)? You probably do if you have Visual Studio installed. Grab **InstallerClean-slim.exe** (7.7 MB) from the releases page instead

## Troubleshooting

- **SmartScreen blocks it.** Click "More info" then "Run anyway". This is normal for unsigned open source software.
- **Something broke after cleanup.** If you used Delete, check the Recycle Bin and restore the file. If you used Move, the files are in whatever folder you chose.
- **Scan results look wrong.** If Windows has pending updates, restart first and run again. Pending updates can make files appear removable when they aren't yet.

## Building from source

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean/InstallerClean.csproj
```

Run the tests:

```
dotnet test src/InstallerClean.Tests/
```

## Contributing

Found a bug or have a suggestion? [Open an issue](../../issues) or start a [discussion](../../discussions). Pull requests welcome. Please run the tests before submitting.

## Part of the No Faff suite

InstallerClean is part of [No Faff](https://github.com/no-faff), a collection of small, useful Windows utilities. No fuss, no bloat, no accounts.

## Support the project

If InstallerClean helped, consider [buying me a cuppa](https://ko-fi.com/nofaff) or leaving a star on GitHub.

## Licence

[MIT](LICENSE)
