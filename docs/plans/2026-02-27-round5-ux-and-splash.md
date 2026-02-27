# Round 5 — UX fixes and splash screen

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.
> Each task ends with a build/test step and a commit. Do not skip verification steps.
> Read CLAUDE.md before starting — British English, sentence case, no Oxford comma.

## Progress (updated by executor)

| Task | Commit | Status |
|------|--------|--------|
| 1 — Fix keyboard/mouse in orphaned files window | dfd7ef9 | ✓ done |
| 2 — Show full digital signature | 119a91b | ✓ done |
| 3 — Make details panel scrollable | bdeee03 | ✓ done |
| 4 — Better exclusion filter explanation | 1f3ed5c | ✓ done |
| 5 — Splash screen with progress steps | 99d5ccf | ✓ done |
| 6 — Verify UAC prompt from compiled exe | e4e0cbe | ✓ done |

## Author: Opus, 27 February 2026

**Goal:** Fix usability issues found during real-world testing and add a startup splash screen with progress steps (like PatchCleaner).

**Context:** Rounds 1–4 complete. The app works correctly and fast. These are UX improvements from hands-on testing and comparison with PatchCleaner.

---

## Task 1: Fix keyboard and mouse wheel in orphaned files window

**Why:** In the orphaned files window, the first item is auto-selected but:
- Arrow keys don't work until you click in the list first
- Mouse wheel doesn't scroll the list

The root cause is the architecture: two ListBoxes (`ActionableList` and `ExcludedList`) sit inside a `StackPanel` inside a `ScrollViewer`. The ListBoxes have `ScrollViewer.VerticalScrollBarVisibility="Disabled"`, which disables their internal scrolling. But the ListBoxes' internal `ScrollViewer` still captures mouse wheel events (swallowing them), and the ListBoxes don't have keyboard focus on load.

In the registered files window, the `ListView` is NOT nested inside an outer ScrollViewer, so mouse wheel and keyboard work fine.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/OrphanedFilesWindow.xaml`** — restructure the left panel. Remove the outer `ScrollViewer` and `StackPanel` wrapper. Instead, use a `Grid` with two rows:
   - Row 0 (`*`): the actionable ListBox (with its own scrolling re-enabled)
   - Row 1 (`Auto`): the excluded section (only visible when there are excluded files)

   The actionable ListBox should have its scrolling enabled:
   ```xml
   <ListBox x:Name="ActionableList"
            ItemsSource="{Binding ActionableFiles}"
            SelectedItem="{Binding SelectedFile, Mode=TwoWay}"
            GotFocus="ActionableList_GotFocus"
            Background="Transparent"
            BorderThickness="0"
            Padding="10,8,10,4"
            HorizontalContentAlignment="Stretch">
   ```

   Remove these attributes from ActionableList:
   - `ScrollViewer.VerticalScrollBarVisibility="Disabled"`
   - `ScrollViewer.HorizontalScrollBarVisibility="Disabled"`

   For the excluded section, keep it as a collapsed panel below the main list. The excluded ListBox should also have scrolling re-enabled (remove the `Disabled` attributes). Give the excluded section a fixed max height or `*` row so it doesn't push the main list off screen.

   A good layout:
   ```xml
   <Grid>
       <Grid.RowDefinitions>
           <RowDefinition Height="*"/>        <!-- actionable files -->
           <RowDefinition Height="Auto"/>     <!-- excluded header + description -->
           <RowDefinition Height="Auto"       <!-- excluded files (max ~150px) -->
                          MaxHeight="150"/>
       </Grid.RowDefinitions>

       <ListBox Grid.Row="0" x:Name="ActionableList" ... />

       <!-- Excluded header + description -->
       <StackPanel Grid.Row="1" ... Visibility binding ... >
           <Border ...>Excluded by filters</Border>
           <TextBlock ...>These files match...</TextBlock>
       </StackPanel>

       <!-- Excluded list -->
       <ListBox Grid.Row="2" x:Name="ExcludedList" ... Visibility binding ... />
   </Grid>
   ```

   Wrap the excluded StackPanel and ListBox each in their own visibility binding to `HasExcludedFiles`.

2. **`src/SimpleWindowsInstallerCleaner/OrphanedFilesWindow.xaml.cs`** — set keyboard focus on load. Add a `Loaded` event handler:

   ```csharp
   private void Window_Loaded(object sender, RoutedEventArgs e)
   {
       ActionableList.Focus();
   }
   ```

   Wire it up in XAML: `Loaded="Window_Loaded"`.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `fix: keyboard navigation and mouse wheel in orphaned files window`

---

## Task 2: Show full digital signature instead of just CN

**Why:** PatchCleaner shows the full certificate subject (e.g., `CN=Python Software Foundation, O=Python Software Foundation, L=Beaverton, S=Oregon, C=US`). Ours shows only the CN portion (`Python Software Foundation`). The full string is more useful for identifying the signer.

**File to modify:**

- **`src/SimpleWindowsInstallerCleaner/Services/MsiFileInfoService.cs`** — simplify `GetDigitalSignature` to return the full subject:

  ```csharp
  private static string GetDigitalSignature(string filePath)
  {
      try
      {
          var cert = X509Certificate.CreateFromSignedFile(filePath);
          return cert.Subject;
      }
      catch (Exception)
      {
          return string.Empty;
      }
  }
  ```

  This removes the CN-parsing logic (lines 76–83) and returns `cert.Subject` directly.

**Verify:** `dotnet test src/SimpleWindowsInstallerCleaner.Tests`

**Commit:** `fix: show full digital signature subject instead of just CN`

---

## Task 3: Make details panels scrollable

**Why:** The details panel (right side) in both orphaned and registered files windows has a fixed-size grid. If a Comment field is very long, it'll wrap but eventually get clipped at the bottom of the panel. The details should scroll when content overflows.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/OrphanedFilesWindow.xaml`** — wrap the details `Grid` (the one with Author/Title/Subject/Signature/Size/Comment rows) in a `ScrollViewer`:

   ```xml
   <ScrollViewer VerticalScrollBarVisibility="Auto"
                 HorizontalScrollBarVisibility="Disabled"
                 Visibility="{Binding ShowDetails, Converter={StaticResource BoolToVis}}">
       <Grid>
           <!-- existing details rows -->
       </Grid>
   </ScrollViewer>
   ```

   Move the `Visibility` binding from the `Grid` to the `ScrollViewer`.

2. **`src/SimpleWindowsInstallerCleaner/RegisteredFilesWindow.xaml`** — same change for the product details panel. Wrap the details Grid in a ScrollViewer.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `fix: make detail panels scrollable for long content`

---

## Task 4: Better exclusion filter explanation

**Why:** Round 4 Task 3 was partial — the tooltip on the excluded row doesn't help discoverability. The user needs to understand WHY files are excluded without hovering. A better approach: show the active filter names inline.

**Files to modify:**

1. **`src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs`** — add a property that shows the active filter names:

   ```csharp
   [ObservableProperty]
   [NotifyPropertyChangedFor(nameof(ExcludedFilterDisplay))]
   private int _excludedFileCount;

   public string ExcludedFilterDisplay =>
       _settings.ExclusionFilters.Count > 0
           ? string.Join(", ", _settings.ExclusionFilters)
           : string.Empty;
   ```

   Also notify `ExcludedFilterDisplay` when settings are reloaded (in `OpenSettings()`). After `_settings = _settingsService.Load();`, add:
   ```csharp
   OnPropertyChanged(nameof(ExcludedFilterDisplay));
   ```

2. **`src/SimpleWindowsInstallerCleaner/MainWindow.xaml`** — change the excluded row text from "X files excluded by filters" to show the filter names. Replace the static text:

   **Before:**
   ```xml
   <TextBlock Text=" files excluded by filters" Foreground="#666" FontSize="13"/>
   ```

   **After:**
   ```xml
   <TextBlock Foreground="#666" FontSize="13">
       <Run Text=" files excluded ("/>
       <Run Text="{Binding ExcludedFilterDisplay, Mode=OneWay}"/>
       <Run Text=")"/>
   </TextBlock>
   ```

   This produces: `● 2 files excluded (Adobe, Acrobat)` — self-explanatory, no tooltip needed.

   Remove the `ToolTip` attribute from the inner StackPanel (it was the partial fix from round 4).

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `fix: show active filter names in excluded row instead of tooltip`

---

## Task 5: Splash screen with progress steps

**Why:** PatchCleaner shows a splash screen during startup with numbered steps and a progress bar. It looks professional and gives confidence the scan is thorough. Our app currently shows the main window immediately with a semi-transparent overlay (which was suppressed for fast scans in round 4). A dedicated splash screen is better — the main window appears already populated.

**Design:** A borderless, dark window (~450×180) centred on screen (like PatchCleaner's splash):
- App name at top: "Simple Windows installer cleaner" (white, 16pt, semi-bold)
- Progress bar (indeterminate, Windows accent blue)
- Step text below the bar (light grey, 12pt), e.g. "Step 2/5: Enumerating installed products..."
- Version at bottom right ("v0.1.0-alpha", small grey text)

**Steps to show** (matches PatchCleaner's "Starting Up..." + 5 numbered steps):
- "Starting up..." (initial display, before scan begins)
- Step 1/5: "Checking system status..." (pending reboot check)
- Step 2/5: "Enumerating installed products..." (product enumeration)
- Step 3/5: "Enumerating patches..." (patch enumeration per product)
- Step 4/5: "Finding installation files..." (disk scan of C:\Windows\Installer)
- Step 5/5: "Calculating orphaned files..." (cross-reference + filter)

**How it works:**

The scan progress messages already come through `IProgress<string>`. The splash screen listens to these and maps them to step numbers. The mapping:
- Messages starting with "Enumerating installed products" → Step 2/5
- Messages starting with "Found" and containing "product" → Step 3/5
- Messages starting with "Scanning installer" → Step 4/5
- Messages starting with "Found" and containing "orphaned" → Step 5/5

**Files to create:**

1. **`src/SimpleWindowsInstallerCleaner/SplashWindow.xaml`** — the splash window:
   - `WindowStyle="None"` (no title bar or border)
   - `AllowsTransparency="True"` (for rounded corners if desired, or just flat)
   - `Background="#404040"` (dark grey, similar to PatchCleaner)
   - `Width="450" Height="180"`
   - `WindowStartupLocation="CenterScreen"`
   - `ResizeMode="NoResize"`
   - `ShowInTaskbar="False"`
   - `Topmost="True"`

   Layout:
   ```xml
   <Grid Margin="24,20">
       <Grid.RowDefinitions>
           <RowDefinition Height="Auto"/>   <!-- app name -->
           <RowDefinition Height="16"/>     <!-- spacer -->
           <RowDefinition Height="Auto"/>   <!-- progress bar -->
           <RowDefinition Height="8"/>      <!-- spacer -->
           <RowDefinition Height="Auto"/>   <!-- step text -->
           <RowDefinition Height="*"/>      <!-- spacer -->
           <RowDefinition Height="Auto"/>   <!-- version -->
       </Grid.RowDefinitions>

       <TextBlock Grid.Row="0"
                  Text="Simple Windows installer cleaner"
                  Foreground="White" FontSize="16" FontWeight="SemiBold"/>

       <ProgressBar Grid.Row="2"
                    IsIndeterminate="True" Height="4"/>

       <TextBlock Grid.Row="4"
                  x:Name="StepText"
                  Text="Starting up..."
                  Foreground="#AAAAAA" FontSize="12"/>

       <TextBlock Grid.Row="6"
                  Text="v0.1.0-alpha"
                  Foreground="#666666" FontSize="10"
                  HorizontalAlignment="Right"/>
   </Grid>
   ```

2. **`src/SimpleWindowsInstallerCleaner/SplashWindow.xaml.cs`** — minimal code-behind with a method to update the step text:

   ```csharp
   public partial class SplashWindow : Window
   {
       public SplashWindow()
       {
           InitializeComponent();
       }

       public void UpdateStep(string message)
       {
           StepText.Text = message;
       }
   }
   ```

3. **`src/SimpleWindowsInstallerCleaner/App.xaml.cs`** — rewrite the startup flow:

   ```csharp
   protected override async void OnStartup(StartupEventArgs e)
   {
       base.OnStartup(e);

       try
       {
           // Show splash screen immediately.
           var splash = new SplashWindow();
           splash.Show();

           // Wire up services.
           var settingsService = new SettingsService();
           var queryService = new InstallerQueryService();
           var scanService = new FileSystemScanService(queryService);
           var moveService = new MoveFilesService();
           var deleteService = new DeleteFilesService();
           var exclusionService = new ExclusionService();
           var rebootService = new PendingRebootService();
           var msiInfoService = new MsiFileInfoService();

           var viewModel = new MainViewModel(
               scanService, moveService, deleteService,
               exclusionService, settingsService, rebootService, msiInfoService);

           // Run the scan with splash progress updates.
           var stepNumber = 0;
           var progress = new Progress<string>(msg =>
           {
               if (msg.StartsWith("Checking") || msg.StartsWith("Starting"))
                   splash.UpdateStep($"Step 1/5: {msg}");
               else if (msg.StartsWith("Enumerating installed"))
               {
                   stepNumber = 2;
                   splash.UpdateStep($"Step 2/5: {msg}");
               }
               else if (msg.StartsWith("Found") && msg.Contains("product"))
               {
                   stepNumber = 3;
                   splash.UpdateStep($"Step 3/5: Enumerating patches...");
               }
               else if (msg.StartsWith("Scanning"))
               {
                   stepNumber = 4;
                   splash.UpdateStep($"Step 4/5: Finding installation files...");
               }
               else if (msg.StartsWith("Found") && msg.Contains("orphaned"))
               {
                   stepNumber = 5;
                   splash.UpdateStep($"Step 5/5: Calculating orphaned files...");
               }
               else if (stepNumber == 2)
               {
                   // Per-product name updates during enumeration
                   splash.UpdateStep($"Step 2/5: {msg}");
               }
           });

           splash.UpdateStep("Step 1/5: Checking system status...");
           await viewModel.ScanWithProgressAsync(progress);

           // Show main window, close splash.
           var window = new MainWindow(viewModel);
           window.Show();
           splash.Close();
       }
       catch (UnauthorizedAccessException)
       {
           MessageBox.Show(
               "This app requires administrator privileges.\n\nPlease right-click and choose 'Run as administrator'.",
               "Administrator rights required",
               MessageBoxButton.OK,
               MessageBoxImage.Warning);
           Shutdown();
       }
       catch (Exception ex)
       {
           MessageBox.Show(
               $"Failed to start: {ex.Message}",
               "Startup error",
               MessageBoxButton.OK,
               MessageBoxImage.Error);
           Shutdown();
       }
   }
   ```

4. **`src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs`** — add a new method `ScanWithProgressAsync` that accepts an external `IProgress<string>` and does the scan + filter + summary updates, but does NOT set `IsScanning` (since the splash handles the visual feedback). This method is called from App.xaml.cs during startup. The existing `ScanAsync` (used by Refresh) keeps working as before.

   ```csharp
   public async Task ScanWithProgressAsync(IProgress<string> progress)
   {
       var sw = Stopwatch.StartNew();

       if (_settings.CheckPendingReboot)
           HasPendingReboot = _rebootService.HasPendingReboot();

       _lastScanResult = await _scanService.ScanAsync(progress);

       _lastFilteredResult = _exclusionService.ApplyFilters(
           _lastScanResult.OrphanedFiles, _settings.ExclusionFilters, _msiInfoService);

       RegisteredFileCount = _lastScanResult.RegisteredPackages.Count;
       RegisteredSizeDisplay = DisplayHelpers.FormatSize(_lastScanResult.RegisteredTotalBytes);

       ExcludedFileCount = _lastFilteredResult.Excluded.Count;
       ExcludedSizeDisplay = DisplayHelpers.FormatSize(_lastFilteredResult.Excluded.Sum(f => f.SizeBytes));

       OrphanedFileCount = _lastFilteredResult.Actionable.Count;
       OrphanedSizeDisplay = DisplayHelpers.FormatSize(_lastFilteredResult.Actionable.Sum(f => f.SizeBytes));

       sw.Stop();
       ScanProgress = $"Scan complete ({sw.Elapsed.TotalSeconds:F1}s)";
       HasScanned = true;
   }
   ```

   Remove the auto-scan call (`_ = viewModel.ScanCommand.ExecuteAsync(null);`) from App.xaml.cs — it's replaced by `ScanWithProgressAsync`.

**Important:** The `ScanAsync` relay command still works for Refresh. It keeps its existing overlay logic. Only the initial startup scan uses the splash screen.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: add startup splash screen with progress steps`

---

## Task 6: Verify UAC prompt from compiled exe

**Why:** The user wants the app to show a UAC prompt when launched (like PatchCleaner). The `app.manifest` with `requireAdministrator` is already in place. The UAC prompt appears when running the compiled `.exe` directly, but NOT when using `dotnet run` (because `dotnet.exe` is the actual process being launched).

**What to do:**
1. Build the exe: `dotnet build src/SimpleWindowsInstallerCleaner`
2. Run the exe directly: `src/SimpleWindowsInstallerCleaner/bin/Debug/net8.0-windows/SimpleWindowsInstallerCleaner.exe`
3. Verify that Windows shows the UAC elevation prompt.

If it works: update the status doc (`docs/status.md`) with a note about how to run:
- Development: `dotnet run --project src/SimpleWindowsInstallerCleaner` (from elevated terminal)
- Production: run the `.exe` directly — UAC will prompt automatically

If it doesn't work: investigate the manifest binding. The `.csproj` already has `<ApplicationManifest>app.manifest</ApplicationManifest>` so it should be embedded.

**Verify:** manual test (run the exe from a non-elevated Explorer window)

**Commit:** `docs: update status with UAC and launch instructions` (or no commit if just verification)

---

## Summary of changes

| Task | Type | What |
|------|------|------|
| 1 | Bug fix | Keyboard navigation and mouse wheel in orphaned files |
| 2 | Enhancement | Full digital signature subject |
| 3 | Enhancement | Scrollable detail panels for long content |
| 4 | UX | Inline filter names in excluded row |
| 5 | Feature | Splash screen with 4-step progress |
| 6 | Verification | UAC prompt from compiled exe |

---

## Execution notes for Sonnet

1. **Do tasks in order.** Tasks 1–4 are independent fixes. Task 5 is the big one.
2. **Build and test after every task.** The plan says when to verify — do it.
3. **Commit after every task.** Small, focused commits.
4. **Don't add features not in this plan.** No extras, no "improvements".
5. **British English in all user-facing strings.** Sentence case. No Oxford comma.
6. **Task 1 is the trickiest.** The ListBox-inside-ScrollViewer pattern is a well-known WPF pain point. The solution is to restructure the layout so each ListBox manages its own scrolling. Test with keyboard arrows and mouse wheel.
7. **Task 5 requires care with the startup flow.** The splash screen shows first, the scan runs behind it, then the main window appears. Make sure error handling still works (especially `UnauthorizedAccessException`).
8. **Task 6 might be a no-op.** Just verify the manifest works. If UAC appears when running the `.exe`, update the docs and move on.
9. **The version is 0.1.0-alpha** everywhere. Don't change it.
