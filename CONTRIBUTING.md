# Contributing to InstallerClean

Thanks for your interest in contributing. InstallerClean is MIT-licensed and
welcomes pull requests.

## Build and test

```
dotnet build src/InstallerClean/InstallerClean.csproj
dotnet test src/InstallerClean.Tests/
```

The app requires **administrator privileges** to run because it accesses
`C:\Windows\Installer` and the Windows Installer API. You can run it from an
elevated terminal with `dotnet run --project src/InstallerClean` or launch the
built exe (which triggers a UAC prompt).

## Commit conventions

Use a prefix: `feat:` / `fix:` / `refactor:` / `chore:` / `test:` / `docs:`

Always run both `dotnet build` and `dotnet test` before committing.

## Known constraints

### Antivirus heuristic sensitivity

This project has been through VirusTotal scanning and two patterns are known to
trigger heuristic antivirus detections. PRs that reintroduce either pattern will
be rejected.

**1. No interface-wrapped MessageBox**

ViewModels call `MessageBox.Show` directly rather than going through an
`IDialogService` interface. This is intentional. The interface-plus-implementation
pattern (extracting an `IDialogService` with a concrete `DialogService` class) was
tested and triggered antivirus heuristic detection, confirmed by VirusTotal scan.

`Func<>` delegates were also tested and trigger the same heuristic. Any
alternative pattern **must pass a clean VirusTotal scan** before it can be merged.

**2. No full paths in console loop output**

Console output must not print full system directory paths
(e.g. `C:\Windows\Installer\filename.msi`) in a loop. This also triggers
heuristic AV detection. Print filenames only, without the directory prefix.

## Filing issues

If you find a bug or have a feature idea, open an issue. Please include:

- What you expected to happen
- What actually happened
- Your Windows version and .NET version (`dotnet --version`)

## Pull requests

- Keep PRs focused on a single change
- Include a short description of what the PR does and why
- Make sure the build and tests pass

All contributions are appreciated.
