# Sonnet prompt: build Simple browser picker

> **Context:** This prompt was written by Opus. You are Sonnet, building a
> greenfield app. Follow this spec carefully. If you hit something you think
> Opus should review (architecture ambiguity, tricky Windows API edge case,
> or anything you're less than 90% confident on), say so clearly and keep
> going with your best guess — Opus will review the finished product.

---

## Step 0 — repo setup (walk the user through this)

Before writing any code, walk the user through these steps interactively:

The user has already created the repo on GitHub at `no-faff/simple-browser-picker`
with description "Choose which browser opens each link. No faff." and MIT licence.

1. Clone it: `cd C:\ && gh repo clone no-faff/simple-browser-picker`
2. `cd C:\simple-browser-picker`
3. Create a `.gitignore` for .NET (use `dotnet new gitignore`)
4. Create the solution and project (commands below)
5. Confirm they've opened a new Claude Code chat in `C:\simple-browser-picker`

Only proceed to building once the repo exists and you're chatting from inside it.

---

## What we're building

**Simple browser picker** — part of the No faff suite (`github.com/no-faff`).

A tiny Windows app that registers as your default browser. When any app opens
a link, it intercepts it and shows a clean popup letting you pick which browser
**or browser profile** to use. You can also set rules so certain domains always
open in a specific browser/profile — and the easiest way to create a rule is
right from the picker itself: "always use this for [domain]?"

**Profiles are first-class citizens.** Each browser profile is its own entry in
the picker — "Vivaldi - Work" and "Vivaldi - Personal" appear as two separate
items, not as a browser with a sub-menu. This is the primary use case: the user
wants to click a link and pick which profile it opens in, just as easily as
picking which browser.

### Design principles

- **Zero faff** — single exe, no installer dependencies, walks you through
  setup on first run
- **Fast** — the picker window must appear instantly (<200ms perceived)
- **Safe defaults** — if anything goes wrong, fall back to opening in the
  first detected browser rather than failing silently
- **Sentence case throughout, British English** (colour, organise, etc.)
- **Modern but native** — should feel like a Windows 11 app, not a web app

---

## Tech stack

- **C# / .NET 8** (not 9 — broader runtime install base)
- **WPF** for the UI
- **Single-exe publishing** — `PublishSingleFile`, self-contained, trimmed
- **Config:** JSON file in `%LOCALAPPDATA%\SimpleBrowserPicker\config.json`
  (not the registry — portable, human-readable, easy to back up)

### Project structure

```
C:\simple-browser-picker\
├── SimpleBrowserPicker.sln
├── src\
│   └── SimpleBrowserPicker\
│       ├── SimpleBrowserPicker.csproj
│       ├── App.xaml / App.xaml.cs
│       ├── Models\
│       │   ├── Browser.cs              # detected browser/profile entry
│       │   ├── BrowserRule.cs           # domain → browser/profile mapping
│       │   └── AppConfig.cs            # full config model
│       ├── Services\
│       │   ├── BrowserDetector.cs      # finds installed browsers
│       │   ├── ConfigService.cs        # load/save JSON config
│       │   ├── ProtocolRegistrar.cs    # register/unregister as default browser
│       │   └── UrlParser.cs           # extract domain, unwrap safelinks
│       ├── ViewModels\
│       │   ├── PickerViewModel.cs
│       │   ├── SettingsViewModel.cs
│       │   └── FirstRunViewModel.cs
│       ├── Views\
│       │   ├── PickerWindow.xaml       # the main popup
│       │   ├── SettingsWindow.xaml      # rules + config
│       │   └── FirstRunWindow.xaml      # setup wizard
│       ├── Converters\                 # WPF value converters
│       ├── Resources\
│       │   └── Styles.xaml             # app-wide theming
│       └── Assets\
│           └── icon.ico
├── CLAUDE.md
├── README.md
└── .gitignore
```

---

## Detailed behaviour spec

### 1. First run experience

When the app launches with no config file:

1. Show a **first run window** explaining what the app does (2-3 sentences)
2. Auto-detect installed browsers and show them in a list with icons
3. A single button: **"Set as default browser"** which:
   - Registers the app as a browser in the registry (see §5)
   - Opens `ms-settings:defaultapps` so the user can select it
   - Shows a note: "Select 'Simple browser picker' in the settings window that just opened"
4. A "Skip for now" link underneath
5. Save config and close the first run window

### 2. Picker window (the core UI)

This appears when a URL is received. Design:

- **Borderless window**, with subtle rounded corners and drop shadow
- Appears **centred on the active monitor** (multi-monitor aware)
- **Top section:** shows the URL being opened (truncated with ellipsis if long,
  full URL on hover tooltip). The domain should be visually emphasised
  (bold or different colour) with the rest of the URL in muted text.
- **Middle section:** grid/list of detected browsers, each showing:
  - Browser icon (extracted from the exe)
  - Browser name
  - Keyboard shortcut hint (1-9)
- **Bottom section:** a checkbox — "Always use [selected browser] for
  [domain]" — this only appears/enables after hovering or focusing a browser.
  Clicking the browser with this checked launches AND saves the rule.
- **Escape** closes the window without opening anything
- **Click anywhere outside** the window also closes it
- The window should feel snappy — no animations on open, minimal visual weight

**Colour scheme / theming:**
- Detect Windows light/dark mode and follow it
- Light: white background (#FFFFFF), subtle border (#E0E0E0)
- Dark: dark surface (#2D2D2D), subtle border (#404040)
- Accent colour from Windows system accent for highlights
- Browser list items highlight on hover with a subtle background change

### 3. URL handling

When the app receives a URL (as a command-line argument):

1. Parse the URL
2. **Unwrap known redirect wrappers:**
   - Outlook SafeLinks (`safelinks.protection.outlook.com`) — extract the
     original URL from the `url=` query parameter
   - Google redirect (`google.com/url?q=`) — extract from `q=` parameter
3. Check against saved rules (domain match)
4. If a rule matches → launch that browser immediately (no picker shown)
5. If no rule matches → show the picker window
6. If launched with no URL argument → show the settings window

### 4. Browser detection

Scan these registry locations to find installed browsers:

```
HKLM\SOFTWARE\Clients\StartMenuInternet\*
HKLM\SOFTWARE\WOW6432Node\Clients\StartMenuInternet\*
HKCU\SOFTWARE\Clients\StartMenuInternet\*
```

For each entry, read:
- Display name (default value or `shell\open\command`)
- Exe path (from `shell\open\command`)
- Icon (extract from the exe using `Icon.ExtractAssociatedIcon`)

**Profile detection (critical feature):**

Browser profiles are first-class. Each profile appears as its own entry in the
picker with its own icon, name and keyboard shortcut. This is the killer
feature — no existing tool does this well.

For **Chromium-based browsers** (Chrome, Edge, Brave, Vivaldi, Opera, Arc):
- Read profiles from `%LOCALAPPDATA%\<BrowserDir>\User Data\Local State`
  (JSON file, key `profile.info_cache` — each entry has `name`, `shortcut_name`,
  `gaia_picture_file_name` etc.)
- Launch with `--profile-directory="Profile 1"` argument
- Each profile becomes a separate `Browser` entry: "Vivaldi - Work",
  "Vivaldi - Personal", etc.
- Use the profile's display name from `Local State`, not the folder name

For **Firefox-based browsers** (Firefox, Zen):
- Read profiles from `%APPDATA%\Mozilla\Firefox\profiles.ini`
- Launch with `-P "profile-name"` argument
- Each profile becomes a separate entry

**Other browsers** without profile support just appear as a single entry.

If only one browser (or profile) is detected, still show the picker (user
might want to add custom entries later).

Cache detected browsers/profiles on first run, re-scan if the user clicks
"refresh" in settings.

### 5. Protocol registration

To appear as a selectable browser in Windows Settings, register under:

```
HKCU\SOFTWARE\Clients\StartMenuInternet\SimpleBrowserPicker\
    (Default) = "Simple browser picker"
    Capabilities\
        ApplicationDescription = "Choose which browser opens each link"
        ApplicationName = "Simple browser picker"
        URLAssociations\
            http = "SimpleBrowserPickerURL"
            https = "SimpleBrowserPickerURL"
    shell\open\command\
        (Default) = "\"<exe path>\" \"%1\""

HKCU\SOFTWARE\Classes\SimpleBrowserPickerURL\
    (Default) = "Simple browser picker URL"
    shell\open\command\
        (Default) = "\"<exe path>\" \"%1\""

HKCU\SOFTWARE\RegisteredApplications\
    SimpleBrowserPicker = "SOFTWARE\\Clients\\StartMenuInternet\\SimpleBrowserPicker\\Capabilities"
```

This uses HKCU (no admin required). Provide a method to **unregister** too
(remove all these keys) accessible from settings.

### 6. Rules engine

Simple domain-based matching:
- Exact domain: `github.com` matches `github.com` and `www.github.com`
  (strip leading `www.`)
- Subdomain wildcard: `*.google.com` matches `mail.google.com`,
  `docs.google.com`, etc.
- Store as a list of `{ domain, browserExePath, browserName, profileArgs }` in config
  (profileArgs holds e.g. `--profile-directory="Profile 1"` so rules work per-profile)

No regex — keep it simple. The primary way to add rules is via the checkbox
in the picker. Rules can also be managed in the settings window.

### 7. Settings window

Accessible by:
- Launching the exe with no arguments
- System tray icon right-click → "Settings" (if we add a tray icon — optional,
  skip for v1)

Contains:
- **Browsers tab:** list of detected browsers, "refresh" button, ability to
  add custom browser (name + exe path + arguments)
- **Rules tab:** list of saved rules, each showing domain → browser, with
  delete button
- **About tab:** app name, version, link to GitHub, "unregister as browser"
  button

### 8. Launching browsers

```csharp
Process.Start(new ProcessStartInfo
{
    FileName = browser.ExePath,
    Arguments = $"{browser.AdditionalArgs} \"{url}\"",
    UseShellExecute = true
});
```

- Always quote the URL
- Close the picker window immediately after launching
- If the process fails to start, show a brief error toast/message and fall
  back to showing the picker again

---

## What to build first (order of implementation)

1. **Project scaffolding** — solution, project, folder structure, CLAUDE.md
2. **Models** — Browser, BrowserRule, AppConfig
3. **ConfigService** — JSON load/save with sensible defaults
4. **BrowserDetector** — registry scanning, icon extraction
5. **UrlParser** — domain extraction, SafeLinks unwrapping
6. **ProtocolRegistrar** — registry read/write for protocol handler
7. **PickerWindow** — the core UI, keyboard shortcuts, "always use" checkbox
8. **App.xaml.cs** — entry point logic (URL arg → picker, no arg → settings)
9. **SettingsWindow** — browsers list, rules list, about
10. **FirstRunWindow** — setup wizard
11. **Styling** — light/dark mode, Windows accent colour
12. **Build config** — single-exe publish profile

Do an initial commit after step 1, then commit at logical checkpoints.

---

## CLAUDE.md for the new repo

Create this as `C:\simple-browser-picker\CLAUDE.md`:

```markdown
# Simple browser picker

Part of the **No faff** suite of small Windows utilities (github.com/no-faff).

## What it does

Registers as your default browser. When any app opens a link, it shows a
clean picker letting you choose which browser to use. Supports rules to
auto-route domains to specific browsers.

## Tech stack

- C# / .NET 8 / WPF
- Single-exe distribution (self-contained, trimmed)
- Config: JSON in %LOCALAPPDATA%\SimpleBrowserPicker\config.json

## Brand / conventions

- Sentence case throughout — no title case
- British English — colour, organise, etc.
- No Oxford comma

## Project structure

- `src/SimpleBrowserPicker/` — main application
- Models, Services, ViewModels, Views pattern (MVVM)

## Building

```shell
dotnet build src/SimpleBrowserPicker/SimpleBrowserPicker.csproj
```

## Publishing (single exe)

```shell
dotnet publish src/SimpleBrowserPicker/SimpleBrowserPicker.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

## Key decisions

- HKCU registration (no admin required)
- JSON config (not registry) — portable, human-readable
- No installer — single exe, first-run wizard handles setup
- Move-don't-delete philosophy: no destructive defaults
```

---

## Notes for Sonnet

- **Don't over-engineer.** This is a small utility. No dependency injection
  frameworks, no MVVM toolkit libraries. Hand-rolled MVVM is fine for 3 views.
- **Windows-only.** Don't add cross-platform abstractions.
- **Test by running.** After building, test with:
  `dotnet run --project src/SimpleBrowserPicker -- "https://example.com"`
- **If you're unsure about something,** flag it with a `// TODO: Opus review —`
  comment and keep going.
- **Icon:** Use a placeholder — just set the app icon to a default .NET icon
  for now. We'll design a proper one later.
- **Commit messages:** sentence case, no emoji, concise.
- When you've finished, tell the user to start a new Opus chat in the same
  folder for review.
