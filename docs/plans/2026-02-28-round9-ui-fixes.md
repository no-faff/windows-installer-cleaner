# Round 9 UI fixes — implementation plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix 5 UI issues: delete confirmation dialog, tooltip stickiness, registered files window, orphaned files window, settings window.

**Architecture:** All changes are XAML layout and style fixes. One new XAML file (custom delete confirmation dialog). No data layer changes. No new ViewModels except a trivial one for the confirm dialog.

**Tech Stack:** WPF (.NET 8), existing Upscayl-based dark theme in App.xaml.

---

## Context for the implementer

- **App name:** "Windows installer cleaner" (not "Simple Windows installer cleaner")
- **Theme:** Dark, based on Upscayl's colour palette. See App.xaml for all brushes and styles.
- **Colour tiers:** Heading #f8fafc, Body #cbd5e1, Muted #94a3b8, Dim #64748b
- **Cards:** CornerRadius=20, Base200 (#0f172a) background
- **Buttons:** AccentPill (indigo), PrimaryPill (slate), GhostPill (transparent)
- **Font:** Poppins (bundled)
- **Section labels:** Uppercase like Upscayl, e.g. "MOVE LOCATION". Use Muted (#94a3b8), FontSize 11, FontWeight SemiBold.
- **No em dashes** in UI text
- **British English, sentence case, no Oxford comma**
- **Build command:** `dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`
- **Test command:** `dotnet test src/SimpleWindowsInstallerCleaner.Tests/SimpleWindowsInstallerCleaner.Tests.csproj`
- **Min screen:** 1366x768 (smallest common Windows laptop). Design for 1080p, allow resize down gracefully.

---

### Task 1: Custom dark delete confirmation dialog

**Problem:** The delete button shows a standard Windows MessageBox — white, ugly, doesn't match the dark theme.

**Files:**
- Create: `src/SimpleWindowsInstallerCleaner/ConfirmDeleteWindow.xaml` + `.xaml.cs`
- Modify: `src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs:237-250` (DeleteAllAsync method)

**Step 1: Create ConfirmDeleteWindow.xaml**

A small, dark-themed modal dialog. No title bar clutter. Shows:
- Warning text: "Permanently delete {count} files ({size})?"
- Subtext: "This cannot be undone."
- Two buttons: "Delete" (AccentPill but with Danger colour #991b1b) and "Cancel" (PrimaryPill)

Window properties:
- `WindowStyle="None"` (no chrome — we draw our own)
- `AllowsTransparency="True"` + `Background="Transparent"` (for rounded corners)
- `SizeToContent="WidthAndHeight"`
- `WindowStartupLocation="CenterOwner"`
- `ResizeMode="NoResize"`
- Content in a Border with Style=Card, CornerRadius=20, Padding=28, MaxWidth=380
- Warning icon: use TextBlock with Unicode ⚠ (#f59e0b) like the reboot warning

The dialog sets `DialogResult = true` for Delete, `false` for Cancel. Escape closes as Cancel.

**Step 2: Create ConfirmDeleteWindow.xaml.cs**

Minimal code-behind: constructor takes count (int) and sizeDisplay (string), sets them as text. Wire Delete button to `DialogResult = true`, Cancel to `DialogResult = false`.

**Step 3: Update MainViewModel.DeleteAllAsync**

Replace `MessageBox.Show(...)` with:
```csharp
var dialog = new ConfirmDeleteWindow(count, sizeDisplay)
{
    Owner = Application.Current.MainWindow
};
if (dialog.ShowDialog() != true) return;
```

**Step 4: Build and test**

Run build. Run tests (existing 22 should still pass). Visually verify the dialog matches the dark theme.

**Step 5: Commit**

```
git add src/SimpleWindowsInstallerCleaner/ConfirmDeleteWindow.xaml src/SimpleWindowsInstallerCleaner/ConfirmDeleteWindow.xaml.cs src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs
git commit -m "feat: custom dark delete confirmation dialog"
```

---

### Task 2: Fix delete tooltip sticking after Cancel

**Problem:** The tooltip on the Delete button doesn't dismiss when the button is clicked and the MessageBox (or new dialog) appears. User has to click elsewhere.

**File:** `src/SimpleWindowsInstallerCleaner/MainWindow.xaml:148-157` (Delete button tooltip area)

**Step 1: Remove the tooltip from the Delete button entirely**

The tooltip says "Moving is safer than deleting. Moved files can be restored if something breaks." This message is now redundant because:
- The custom delete dialog (Task 1) already warns the user
- The Move button is visually prominent (AccentPill indigo) while Delete is secondary (PrimaryPill slate)
- The design already communicates that Move is preferred

Simply remove the `ToolTipService` properties and the `Button.ToolTip` block from the Delete button in MainWindow.xaml.

If we want to keep the tooltip, the alternative fix is to programmatically close it before showing the dialog by setting `ToolTipService.IsEnabled` to false then back to true. But removing it is cleaner.

**Step 2: Build and visually verify**

**Step 3: Commit**

```
git commit -m "fix: remove Delete tooltip (redundant with confirmation dialog)"
```

---

### Task 3: Fix registered files window

**Problems:**
1. White/light-blue selection highlight on ListView rows
2. Section headings should be uppercase labels (Upscayl style)
3. Product details panel too small — content gets cut off
4. Window too small overall

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner/RegisteredFilesWindow.xaml`
- Modify: `src/SimpleWindowsInstallerCleaner/App.xaml` (add ListView/ListBox selection style)

**Step 1: Add dark selection styles to App.xaml**

Add styles for ListViewItem and ListBoxItem that override the default white/blue highlight:

```xml
<!-- Dark selection for ListView items -->
<Style TargetType="ListViewItem">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="ListViewItem">
                <Border x:Name="Bd" Background="Transparent" Padding="4,6">
                    <GridViewRowPresenter/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsSelected" Value="True">
                        <Setter TargetName="Bd" Property="Background" Value="#334155"/>
                    </Trigger>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="Bd" Property="Background" Value="#1e293b"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>

<!-- Dark selection for ListBox items -->
<Style TargetType="ListBoxItem">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="ListBoxItem">
                <Border x:Name="Bd" Background="Transparent" Padding="2,4">
                    <ContentPresenter/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsSelected" Value="True">
                        <Setter TargetName="Bd" Property="Background" Value="#334155"/>
                    </Trigger>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="Bd" Property="Background" Value="#1e293b"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

Note: These are DEFAULT styles (no x:Key), so they apply globally. The ListBoxItem style will also fix the orphaned files window (Task 4). The ListViewItem padding of 4,6 gives compact rows. Verify visually — may need adjusting.

**Step 2: Update RegisteredFilesWindow.xaml**

Changes:
- Window size: `Width="950" Height="680"` (bigger to fit details)
- Section headings to uppercase labels:
  - "Products" → "PRODUCTS" with Foreground="#94a3b8" FontSize="11" FontWeight="SemiBold" (remove the old FontSize="15")
  - "Patches" → "PATCHES" same style
  - "Product details" → "PRODUCT DETAILS" same style
- Bottom section (Patches + Product details): Change the column split from `Width="220"` to `Width="250"` for patches, keeping `*` for details. This gives more room for the detail panel.
- The GridView column headers also have default white styling — they need dark styling too. Add to the ListView:
  ```xml
  <ListView.Resources>
      <Style TargetType="GridViewColumnHeader">
          <Setter Property="Background" Value="#0f172a"/>
          <Setter Property="Foreground" Value="#94a3b8"/>
          <Setter Property="FontSize" Value="11"/>
          <Setter Property="Padding" Value="8,6"/>
          <Setter Property="BorderThickness" Value="0"/>
      </Style>
  </ListView.Resources>
  ```

**Step 3: Build and visually verify**

Check: dark selection on product rows, uppercase section labels, details panel fits Calibre's info without scrollbar.

**Step 4: Commit**

```
git commit -m "fix: dark selection highlight and layout for registered files window"
```

---

### Task 4: Fix orphaned files window

**Problems:**
1. Too much vertical padding on list items
2. First column (filename) too wide — wastes space
3. Detail panel too narrow (230px) — digital signatures wrap horribly

**File:** `src/SimpleWindowsInstallerCleaner/OrphanedFilesWindow.xaml`

**Step 1: Fix the layout**

Changes:
- Window size: `Width="900" Height="540"` (wider for detail panel)
- Detail panel column: `Width="230"` → `Width="320"` (or use a proportional split like `Width="1.5*"` for the file list and `Width="*"` for details — but fixed widths are simpler here. Try `Width="*"` for file list and `Width="300"` for details.)
- Actually, better approach: change the column definitions to:
  ```xml
  <ColumnDefinition Width="*"/>       <!-- file list gets remaining space -->
  <ColumnDefinition Width="12"/>      <!-- gap -->
  <ColumnDefinition Width="320"/>     <!-- detail panel — was 230, now wider -->
  ```
- The filename column inside the ListBox item template has `Width="*"` which is fine, but the file list's overall width is now flexible.
- ListBox item vertical padding: The global ListBoxItem style from Task 3 sets `Padding="2,4"`. The existing item template has `Margin="0,1"` on the Grid. This should already be compact. If still too padded, check if the global style's padding is stacking with the template margin. May need to set the ListBoxItem padding to "2,2" instead.
- First column width: The file list template has `<ColumnDefinition Width="*"/>` for filename, which fills available space. This is correct — the issue is the detail panel is too narrow, making the list look too wide in comparison. Widening the detail panel to 320px should fix the proportion.

**Step 2: Build and visually verify**

Check: items are compact, detail panel shows full digital signature text for LibreOffice without horrible wrapping.

**Step 3: Commit**

```
git commit -m "fix: orphaned files layout — wider detail panel, compact items"
```

---

### Task 5: Rebuild settings window

**Problems:**
- Filter list box has egg-shaped corners (CornerRadius 20 on a short box = oval ends)
- Filter entries invisible (content clipped or hidden)
- Overall layout broken

**File:** `src/SimpleWindowsInstallerCleaner/SettingsWindow.xaml`

**Root cause analysis:**
- The egg-shaped corners are caused by **wpfui's ControlsDictionary** overriding border/control rendering — NOT simply CornerRadius vs height. This same issue affected other controls earlier in the project and was fixed by using explicit control templates that bypass wpfui's defaults.
- The filter entries (Adobe, Acrobat) are being clipped/hidden — likely wpfui's styling is interfering with the ItemsControl or ScrollViewer inside the CardNoPad border.
- The whole settings layout needs rebuilding with explicit templates, same approach that fixed controls elsewhere in the app.

**Step 1: Rewrite SettingsWindow.xaml**

Key changes:
- **Section heading:** "Exclusion filters" → "EXCLUSION FILTERS" with Muted style (matching "MOVE LOCATION")
- **Filter list:** Drop CardNoPad entirely. Use a plain Border with explicit Background="#0f172a", CornerRadius="12". Put the ItemsControl directly inside (no ScrollViewer wrapper — wpfui may be interfering with it). If scrolling is needed, use a ScrollViewer with an explicit template or set `ScrollViewer.CanContentScroll="False"`.
- **Ensure filter entries are visible:** Each entry should be a simple TextBlock + remove button row. Test that both "Adobe" and "Acrobat" appear and are fully readable.
- **Window size:** `Width="480" Height="460"` (slightly taller to give the list more room)
- **Checkbox:** Should use the same font and colour scheme. Currently `Foreground="#f8fafc"` which is fine. Ensure alignment. If wpfui is overriding the checkbox appearance, use an explicit checkbox template.
- **Save/Cancel:** Currently `HorizontalAlignment="Right"` which is correct. Keep them.
- **Reboot checkbox:** Separate it with a section heading "OPTIONS" above it (uppercase, muted style).

Complete new layout for the grid:

```
Row 0: "EXCLUSION FILTERS" heading
Row 1: spacing (8)
Row 2: Description text (both paragraphs)
Row 3: spacing (12)
Row 4: Text input + Add button
Row 5: spacing (8)
Row 6: Filter list (with CornerRadius 12, MinHeight 80)
Row 7: spacing (16)
Row 8: "OPTIONS" heading
Row 9: spacing (8)
Row 10: Checkbox
Row 11: spacing (16)
Row 12: Save/Cancel buttons
```

**Step 2: Build and visually verify**

Check: filter list shows both "Adobe" and "Acrobat" entries, no egg corners, no clipping. Checkbox aligned. Save/Cancel positioned correctly.

**Step 3: Commit**

```
git commit -m "fix: rebuild settings window layout — proper corners and visible filter entries"
```

---

### Task 6: Final commit and push

**Step 1:** Run full build and test suite.
**Step 2:** Commit any stragglers (settings.local.json etc).
**Step 3:** Push to origin.
**Step 4:** Update screenshot in docs/screenshot.png if user provides a new one.

---

## Execution order

Tasks 1-2 are quick wins (delete dialog + tooltip).
Task 3 (registered files) and Task 4 (orphaned files) share the selection style from App.xaml — do Task 3 first since it adds the global styles.
Task 5 (settings) is standalone.

Recommended order: **3 → 1 → 2 → 4 → 5 → 6**

(Start with 3 because the global ListViewItem/ListBoxItem styles it adds benefit Tasks 4 and 5.)
