# Round 7 — UI redesign

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.
> Each task ends with a build step and a commit. Do not skip verification steps.
> Read CLAUDE.md before starting — British English, sentence case, no Oxford comma.

**Goal:** Transform the app from functional-but-plain WPF into a modern, airy,
"child-like simple" interface that follows the Windows system theme (light/dark)
with custom window chrome, rounded-corner cards, generous white space and a
confident splash screen.

**Architecture:** All theming goes into resource dictionaries (`Themes/Light.xaml`,
`Themes/Dark.xaml`) loaded at startup based on `SystemParameters.HighContrast`
and the Windows `AppsUseLightTheme` registry key. A `ThemeService` detects the
current theme and loads the right dictionary. Every window gets custom chrome
(borderless + hand-built title bar). Light is the primary design target; dark is
derived from it.

**Tech stack:** Pure WPF (.NET 8). No NuGet UI libraries. Custom control templates,
resource dictionaries, `WindowChrome` class.

**Design direction:** Reference Audin Rushow (light dashboard — airy, white cards,
clean lines). Febriansyah Nursan (splash — colourful, branded, alive). Naveen
Yellamelli (progress dialogs — Windows 11 file transfer concept). The app should
feel simple, clean, almost cartoonish. Beautiful ≠ complicated.

---

## Progress (updated by executor)

| Task | Commit | Status |
|------|--------|--------|
| 1 — Design tokens and theme dictionaries | | pending |
| 2 — Theme detection service | | pending |
| 3 — Custom window chrome base | | pending |
| 4 — Main window redesign | | pending |
| 5 — Splash screen redesign | | pending |
| 6 — Orphaned files window redesign | | pending |
| 7 — Registered files window redesign | | pending |
| 8 — Settings window redesign | | pending |
| 9 — About window redesign | | pending |
| 10 — Move/delete progress overlay redesign | | pending |
| 11 — Final polish and consistency pass | | pending |

---

## Design system

Before diving into tasks, here is the complete design system. Every task
references these tokens. The executor should not invent new values — use these.

### Colour tokens

All colours are defined as `SolidColorBrush` resources in the theme dictionaries.
Light values shown first, dark in parentheses.

| Token name | Light | Dark | Use |
|---|---|---|---|
| `WindowBackground` | `#FFFFFF` | `#1E1E1E` | Window and page background |
| `CardBackground` | `#FFFFFF` | `#2D2D2D` | Card/panel surfaces |
| `CardBorder` | `#E8E8E8` | `#3D3D3D` | Card borders (subtle) |
| `SurfaceBackground` | `#F7F7F8` | `#252525` | Slightly recessed areas (list backgrounds) |
| `AccentPrimary` | `#0066CC` | `#4DA3FF` | Primary buttons, links, highlights |
| `AccentPrimaryHover` | `#0052A3` | `#66B3FF` | Hover state for primary accent |
| `AccentDanger` | `#C4314B` | `#F47171` | Orphaned count, delete actions |
| `AccentDangerHover` | `#A12B40` | `#F99` | Delete hover |
| `AccentWarning` | `#F5C518` | `#F5C518` | Warning banner icon (same in both) |
| `WarningBackground` | `#FFF8E1` | `#3D3520` | Reboot warning banner |
| `WarningBorder` | `#FFE082` | `#5C4E2A` | Warning banner border |
| `WarningText` | `#6D5500` | `#FFD54F` | Warning banner text |
| `TextPrimary` | `#1A1A1A` | `#E8E8E8` | Headings, primary content |
| `TextSecondary` | `#555555` | `#AAAAAA` | Secondary text, descriptions |
| `TextTertiary` | `#888888` | `#707070` | Labels, placeholders, hints |
| `TextOnAccent` | `#FFFFFF` | `#FFFFFF` | Text on accent-coloured backgrounds |
| `BorderSubtle` | `#E0E0E0` | `#404040` | Dividers, separators |
| `ScanOverlayBackground` | `#CCFFFFFF` | `#CC1E1E1E` | Semi-transparent scan overlay |
| `ScanOverlayText` | `#1A1A1A` | `#E8E8E8` | Text on scan overlay |
| `SplashBackground` | `#0066CC` | `#0066CC` | Splash window background (always accent) |
| `SplashText` | `#FFFFFF` | `#FFFFFF` | Splash text (always white on accent) |
| `SplashTextDim` | `#BBDDFF` | `#BBDDFF` | Splash secondary text |
| `TitleBarBackground` | `#F7F7F8` | `#252525` | Custom title bar |
| `TitleBarText` | `#333333` | `#CCCCCC` | Title bar text |
| `TitleBarButtonHover` | `#E5E5E5` | `#3D3D3D` | Title bar button hover |
| `TitleBarCloseHover` | `#E81123` | `#E81123` | Close button hover (always red) |
| `TitleBarCloseText` | `#FFFFFF` | `#FFFFFF` | Close button text on hover |

### Typography

| Token | Value |
|---|---|
| `FontFamily` | `Segoe UI Variable, Segoe UI` (native Windows 11) |
| `HeadingSize` | `20` |
| `SubheadingSize` | `14` |
| `BodySize` | `13` |
| `SmallSize` | `12` |
| `CaptionSize` | `11` |

### Spacing

| Token | Value |
|---|---|
| `WindowPadding` | `24` |
| `CardPadding` | `20` |
| `CardRadius` | `8` |
| `ButtonRadius` | `6` |
| `ItemSpacing` | `8` |
| `SectionSpacing` | `16` |

### Elevation (light only — dark uses border contrast instead)

| Level | Effect |
|---|---|
| Card | `DropShadowEffect` — BlurRadius 8, ShadowDepth 1, Opacity 0.06, Color #000 |
| Elevated card | BlurRadius 16, ShadowDepth 2, Opacity 0.10, Color #000 |

Dark theme: no shadows. Cards differentiated by `CardBackground` vs `SurfaceBackground`.

### Common control styles

These named styles are defined in each theme dictionary and used throughout:

| Style key | Applies to | Description |
|---|---|---|
| `PrimaryButton` | Button | Accent fill, white text, CardRadius corners, 24,10 padding |
| `SecondaryButton` | Button | CardBorder outline, TextPrimary text, CardRadius corners |
| `DangerButton` | Button | AccentDanger fill, white text |
| `GhostButton` | Button | Transparent, AccentPrimary text, underline on hover |
| `LinkButton` | Button | AccentPrimary text, underline, no border, hand cursor |
| `CardBorder` | Border | CardBackground fill, CardBorder stroke, CardRadius corners, shadow |
| `SectionHeader` | TextBlock | SubheadingSize, SemiBold, TextPrimary |
| `BodyText` | TextBlock | BodySize, TextSecondary |
| `CaptionText` | TextBlock | CaptionSize, TextTertiary |

---

## Task 1: Design tokens and theme dictionaries

**Why:** Everything else depends on having the design system in resource
dictionaries. Build the foundation first.

**Files to create:**

### 1. `src/SimpleWindowsInstallerCleaner/Themes/Light.xaml`

A `ResourceDictionary` containing every colour token, typography value, spacing
constant and control style from the design system above.

Structure:
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ── Colours ──────────────────────────────────────── -->
    <SolidColorBrush x:Key="WindowBackground" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="CardBackground" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="CardBorder" Color="#E8E8E8"/>
    <SolidColorBrush x:Key="SurfaceBackground" Color="#F7F7F8"/>
    <SolidColorBrush x:Key="AccentPrimary" Color="#0066CC"/>
    <SolidColorBrush x:Key="AccentPrimaryHover" Color="#0052A3"/>
    <SolidColorBrush x:Key="AccentDanger" Color="#C4314B"/>
    <SolidColorBrush x:Key="AccentDangerHover" Color="#A12B40"/>
    <SolidColorBrush x:Key="AccentWarning" Color="#F5C518"/>
    <SolidColorBrush x:Key="WarningBackground" Color="#FFF8E1"/>
    <SolidColorBrush x:Key="WarningBorder" Color="#FFE082"/>
    <SolidColorBrush x:Key="WarningText" Color="#6D5500"/>
    <SolidColorBrush x:Key="TextPrimary" Color="#1A1A1A"/>
    <SolidColorBrush x:Key="TextSecondary" Color="#555555"/>
    <SolidColorBrush x:Key="TextTertiary" Color="#888888"/>
    <SolidColorBrush x:Key="TextOnAccent" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="BorderSubtle" Color="#E0E0E0"/>
    <SolidColorBrush x:Key="ScanOverlayBackground" Color="#CCFFFFFF"/>
    <SolidColorBrush x:Key="ScanOverlayText" Color="#1A1A1A"/>
    <SolidColorBrush x:Key="SplashBackground" Color="#0066CC"/>
    <SolidColorBrush x:Key="SplashText" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="SplashTextDim" Color="#BBDDFF"/>
    <SolidColorBrush x:Key="TitleBarBackground" Color="#F7F7F8"/>
    <SolidColorBrush x:Key="TitleBarText" Color="#333333"/>
    <SolidColorBrush x:Key="TitleBarButtonHover" Color="#E5E5E5"/>
    <SolidColorBrush x:Key="TitleBarCloseHover" Color="#E81123"/>
    <SolidColorBrush x:Key="TitleBarCloseText" Color="#FFFFFF"/>

    <!-- ── Typography ──────────────────────────────────── -->
    <FontFamily x:Key="AppFont">Segoe UI Variable, Segoe UI</FontFamily>
    <sys:Double x:Key="HeadingSize"
                xmlns:sys="clr-namespace:System;assembly=mscorlib">20</sys:Double>
    <sys:Double x:Key="SubheadingSize"
                xmlns:sys="clr-namespace:System;assembly=mscorlib">14</sys:Double>
    <sys:Double x:Key="BodySize"
                xmlns:sys="clr-namespace:System;assembly=mscorlib">13</sys:Double>
    <sys:Double x:Key="SmallSize"
                xmlns:sys="clr-namespace:System;assembly=mscorlib">12</sys:Double>
    <sys:Double x:Key="CaptionSize"
                xmlns:sys="clr-namespace:System;assembly=mscorlib">11</sys:Double>

    <!-- ── Spacing ─────────────────────────────────────── -->
    <Thickness x:Key="WindowPadding">24</Thickness>
    <Thickness x:Key="CardPadding">20</Thickness>
    <CornerRadius x:Key="CardRadius">8</CornerRadius>
    <CornerRadius x:Key="ButtonRadius">6</CornerRadius>
    <sys:Double x:Key="ItemSpacing"
                xmlns:sys="clr-namespace:System;assembly=mscorlib">8</sys:Double>
    <sys:Double x:Key="SectionSpacing"
                xmlns:sys="clr-namespace:System;assembly=mscorlib">16</sys:Double>

    <!-- ── Shadows (light theme only) ──────────────────── -->
    <DropShadowEffect x:Key="CardShadow"
                      BlurRadius="8" ShadowDepth="1" Opacity="0.06"
                      Color="Black" Direction="270"/>
    <DropShadowEffect x:Key="ElevatedShadow"
                      BlurRadius="16" ShadowDepth="2" Opacity="0.10"
                      Color="Black" Direction="270"/>

    <!-- ── Button styles ───────────────────────────────── -->
    <!-- PrimaryButton: accent fill, white text, rounded -->
    <Style x:Key="PrimaryButton" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource AccentPrimary}"/>
        <Setter Property="Foreground" Value="{StaticResource TextOnAccent}"/>
        <Setter Property="FontFamily" Value="{StaticResource AppFont}"/>
        <Setter Property="FontSize" Value="{StaticResource BodySize}"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Padding" Value="24,10"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            Padding="{TemplateBinding Padding}"
                            CornerRadius="{StaticResource ButtonRadius}">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background"
                                    Value="{StaticResource AccentPrimaryHover}"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Opacity" Value="0.4"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- SecondaryButton: outlined, text-coloured -->
    <Style x:Key="SecondaryButton" TargetType="Button">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimary}"/>
        <Setter Property="FontFamily" Value="{StaticResource AppFont}"/>
        <Setter Property="FontSize" Value="{StaticResource BodySize}"/>
        <Setter Property="Padding" Value="20,9"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="BorderBrush" Value="{StaticResource CardBorder}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            Padding="{TemplateBinding Padding}"
                            CornerRadius="{StaticResource ButtonRadius}">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background"
                                    Value="{StaticResource SurfaceBackground}"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Opacity" Value="0.4"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- DangerButton: red fill, white text -->
    <Style x:Key="DangerButton" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource AccentDanger}"/>
        <Setter Property="Foreground" Value="{StaticResource TextOnAccent}"/>
        <Setter Property="FontFamily" Value="{StaticResource AppFont}"/>
        <Setter Property="FontSize" Value="{StaticResource BodySize}"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Padding" Value="20,9"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            Padding="{TemplateBinding Padding}"
                            CornerRadius="{StaticResource ButtonRadius}">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background"
                                    Value="{StaticResource AccentDangerHover}"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Opacity" Value="0.4"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- GhostButton: transparent, accent text -->
    <Style x:Key="GhostButton" TargetType="Button">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource AccentPrimary}"/>
        <Setter Property="FontFamily" Value="{StaticResource AppFont}"/>
        <Setter Property="FontSize" Value="{StaticResource BodySize}"/>
        <Setter Property="Padding" Value="8,6"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            Padding="{TemplateBinding Padding}"
                            CornerRadius="{StaticResource ButtonRadius}">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background"
                                    Value="{StaticResource SurfaceBackground}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- LinkButton: inline text link -->
    <Style x:Key="LinkButton" TargetType="Button">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource AccentPrimary}"/>
        <Setter Property="FontFamily" Value="{StaticResource AppFont}"/>
        <Setter Property="FontSize" Value="{StaticResource SmallSize}"/>
        <Setter Property="Padding" Value="2,0"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <TextBlock x:Name="LinkText"
                               Text="{TemplateBinding Content}"
                               Foreground="{TemplateBinding Foreground}"
                               FontSize="{TemplateBinding FontSize}"
                               FontFamily="{TemplateBinding FontFamily}"/>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="LinkText"
                                    Property="TextDecorations" Value="Underline"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- TitleBarButton: window chrome buttons -->
    <Style x:Key="TitleBarButton" TargetType="Button">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource TitleBarText}"/>
        <Setter Property="FontFamily" Value="Segoe MDL2 Assets"/>
        <Setter Property="FontSize" Value="10"/>
        <Setter Property="Width" Value="46"/>
        <Setter Property="Height" Value="32"/>
        <Setter Property="Padding" Value="0"/>
        <Setter Property="Cursor" Value="Arrow"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="WindowChrome.IsHitTestVisibleInChrome" Value="True"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background"
                                    Value="{StaticResource TitleBarButtonHover}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- TitleBarCloseButton: close button with red hover -->
    <Style x:Key="TitleBarCloseButton" TargetType="Button"
           BasedOn="{StaticResource TitleBarButton}">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="Bg" Background="Transparent">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Bg" Property="Background"
                                    Value="{StaticResource TitleBarCloseHover}"/>
                            <Setter Property="Foreground"
                                    Value="{StaticResource TitleBarCloseText}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

</ResourceDictionary>
```

### 2. `src/SimpleWindowsInstallerCleaner/Themes/Dark.xaml`

Same structure as Light.xaml but with the dark colour values from the design
system table. Copy Light.xaml entirely and change only the `Color` attributes
on each `SolidColorBrush`. Also:

- Remove the `CardShadow` and `ElevatedShadow` effects (replace with empty
  `DropShadowEffect` instances that have Opacity 0 — this means bindings
  using `{DynamicResource CardShadow}` still work, they just do nothing).
- The splash colours (`SplashBackground`, `SplashText`, `SplashTextDim`)
  are identical in both themes — the splash is always accent-on-blue.

### 3. `src/SimpleWindowsInstallerCleaner/App.xaml`

Add a `ResourceDictionary.MergedDictionaries` that loads the light theme by
default. The `ThemeService` (task 2) will swap this at runtime.

```xml
<Application x:Class="SimpleWindowsInstallerCleaner.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Themes/Light.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: add light and dark theme resource dictionaries with design tokens`

---

## Task 2: Theme detection service

**Why:** The app should follow the Windows system theme (light/dark). We need a
service that detects the current setting and loads the right dictionary.

**Files to create:**

### 1. `src/SimpleWindowsInstallerCleaner/Services/ThemeService.cs`

```csharp
using System.Windows;
using Microsoft.Win32;

namespace SimpleWindowsInstallerCleaner.Services;

internal static class ThemeService
{
    private static readonly Uri LightThemeUri = new("Themes/Light.xaml", UriKind.Relative);
    private static readonly Uri DarkThemeUri = new("Themes/Dark.xaml", UriKind.Relative);

    /// <summary>
    /// Detects the Windows theme and loads the matching resource dictionary.
    /// Call once at startup, before any windows are shown.
    /// </summary>
    internal static void ApplySystemTheme()
    {
        var isDark = IsSystemDarkTheme();
        var uri = isDark ? DarkThemeUri : LightThemeUri;

        var merged = Application.Current.Resources.MergedDictionaries;
        merged.Clear();
        merged.Add(new ResourceDictionary { Source = uri });
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            // 0 = dark, 1 = light, missing = assume light
            return value is int i && i == 0;
        }
        catch
        {
            return false; // assume light on failure
        }
    }
}
```

### 2. Modify `src/SimpleWindowsInstallerCleaner/App.xaml.cs`

Add a single call at the very start of `OnStartup`, before the splash window
is created:

```csharp
ThemeService.ApplySystemTheme();
```

Place it immediately after `base.OnStartup(e);`, before `var splash = new SplashWindow();`.

**Note on DynamicResource vs StaticResource:** Because the theme dictionary is
loaded once at startup (before any windows are created), `StaticResource` will
work fine for all resource lookups. We don't need `DynamicResource` since we're
not switching themes at runtime — we detect once and stick with it. However, if
you want to be safe for future runtime switching, use `DynamicResource` for
colour/brush references in XAML. The executor should use **`DynamicResource`**
for all brush/colour references and **`StaticResource`** for typography, spacing
and styles (which are identical in both themes).

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: add theme detection service that follows Windows system theme`

---

## Task 3: Custom window chrome base

**Why:** The single biggest "pro vs amateur" signal in a desktop app is the
title bar. Default WPF chrome screams "developer project." Custom chrome with
a hand-built title bar says "designed product."

**Approach:** Use WPF's `WindowChrome` class to remove the default title bar
while keeping window management (drag, resize, snap). Build a reusable title
bar UserControl that every window includes.

**Files to create:**

### 1. `src/SimpleWindowsInstallerCleaner/Controls/TitleBar.xaml`

A `UserControl` that provides the custom title bar. The window title is passed
in via a dependency property.

```xml
<UserControl x:Class="SimpleWindowsInstallerCleaner.Controls.TitleBar"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Background="{DynamicResource TitleBarBackground}"
            Height="32">
        <DockPanel LastChildFill="True">
            <!-- Window control buttons (right side) -->
            <StackPanel DockPanel.Dock="Right" Orientation="Horizontal">
                <Button x:Name="MinimiseButton"
                        Style="{DynamicResource TitleBarButton}"
                        Content="&#xE921;"
                        Click="MinimiseButton_Click"/>
                <Button x:Name="MaximiseButton"
                        Style="{DynamicResource TitleBarButton}"
                        Content="&#xE922;"
                        Click="MaximiseButton_Click"/>
                <Button x:Name="CloseButton"
                        Style="{DynamicResource TitleBarCloseButton}"
                        Content="&#xE8BB;"
                        Click="CloseButton_Click"/>
            </StackPanel>

            <!-- Title text (left side, centred vertically) -->
            <TextBlock x:Name="TitleText"
                       Text="{Binding WindowTitle, RelativeSource={RelativeSource AncestorType=UserControl}}"
                       Foreground="{DynamicResource TitleBarText}"
                       FontFamily="{StaticResource AppFont}"
                       FontSize="{StaticResource SmallSize}"
                       VerticalAlignment="Center"
                       Margin="12,0,0,0"/>
        </DockPanel>
    </Border>
</UserControl>
```

### 2. `src/SimpleWindowsInstallerCleaner/Controls/TitleBar.xaml.cs`

```csharp
using System.Windows;
using System.Windows.Controls;

namespace SimpleWindowsInstallerCleaner.Controls;

public partial class TitleBar : UserControl
{
    public static readonly DependencyProperty WindowTitleProperty =
        DependencyProperty.Register(
            nameof(WindowTitle), typeof(string), typeof(TitleBar),
            new PropertyMetadata(string.Empty));

    public string WindowTitle
    {
        get => (string)GetValue(WindowTitleProperty);
        set => SetValue(WindowTitleProperty, value);
    }

    public static readonly DependencyProperty ShowMaximiseProperty =
        DependencyProperty.Register(
            nameof(ShowMaximise), typeof(bool), typeof(TitleBar),
            new PropertyMetadata(true, OnShowMaximiseChanged));

    public bool ShowMaximise
    {
        get => (bool)GetValue(ShowMaximiseProperty);
        set => SetValue(ShowMaximiseProperty, value);
    }

    public static readonly DependencyProperty ShowMinimiseProperty =
        DependencyProperty.Register(
            nameof(ShowMinimise), typeof(bool), typeof(TitleBar),
            new PropertyMetadata(true, OnShowMinimiseChanged));

    public bool ShowMinimise
    {
        get => (bool)GetValue(ShowMinimiseProperty);
        set => SetValue(ShowMinimiseProperty, value);
    }

    public TitleBar()
    {
        InitializeComponent();
    }

    private void MinimiseButton_Click(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)!.WindowState = WindowState.Minimized;

    private void MaximiseButton_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this)!;
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)!.Close();

    private static void OnShowMaximiseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TitleBar tb)
            tb.MaximiseButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void OnShowMinimiseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TitleBar tb)
            tb.MinimiseButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
    }
}
```

### 3. How to apply custom chrome to a window

Every window that gets custom chrome needs these additions. **Don't do this yet
for all windows** — we'll apply it window by window in tasks 4–9. But the
pattern is:

```xml
<Window ...
        WindowStyle="None"
        AllowsTransparency="False"
        Background="{DynamicResource WindowBackground}">
    <WindowChrome.WindowChrome>
        <WindowChrome CaptionHeight="32"
                      ResizeBorderThickness="6"
                      GlassFrameThickness="0"
                      CornerRadius="0"
                      UseAeroPeek="True"/>
    </WindowChrome.WindowChrome>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- title bar -->
            <RowDefinition Height="*"/>     <!-- content -->
        </Grid.RowDefinitions>

        <controls:TitleBar Grid.Row="0" WindowTitle="Window title here"
                           ShowMaximise="False"/>

        <!-- Window content goes in Grid.Row="1" -->
    </Grid>
</Window>
```

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: add custom title bar control with window chrome support`

---

## Task 4: Main window redesign

**Why:** The main window is the first thing users see after the splash. It needs
to feel airy, clean and confident. Currently 700×320, grey background, dense
text, old-style bottom bar.

**Files to modify:**

### 1. `src/SimpleWindowsInstallerCleaner/MainWindow.xaml`

Complete rewrite. The new layout:

```
┌──────────────────────────────────────────────────┐
│ [title bar: "Simple Windows installer cleaner"]  │
├──────────────────────────────────────────────────┤
│                                                  │
│   ┌──────────────────────────────────────────┐   │
│   │  SCAN RESULTS CARD                       │   │
│   │                                          │   │
│   │  ● 142 files still used        1.2 GB   │   │
│   │  ● 3 files excluded (Adobe)    48 MB    │   │
│   │  ● 12 files orphaned           340 MB   │   │
│   │                                          │   │
│   │  [details...]                            │   │
│   └──────────────────────────────────────────┘   │
│                                                  │
│   ┌──────────────────────────────────────────┐   │
│   │  ACTIONS CARD                            │   │
│   │                                          │   │
│   │  Move to: [C:\Installer-Backup] [Browse] │   │
│   │                                          │   │
│   │  [  Move  ]  [Delete]   Scan: 1.2s      │   │
│   └──────────────────────────────────────────┘   │
│                                                  │
│   ⚙ Settings   ↺ Refresh   ℹ About    ♥ Donate │
│                                                  │
└──────────────────────────────────────────────────┘
```

**Key design changes from current:**

1. **Custom title bar** (from task 3). No maximise button (main window is
   fixed-ish — `ResizeMode="CanMinimize"` as before).
2. **White background** (`WindowBackground`), not `#F5F5F5`.
3. **Two rounded cards** with subtle shadow, generous padding (20px).
4. **Bigger summary text** — the counts are larger (SubheadingSize 14pt) and
   the card has breathing room.
5. **"details..." links** sit below the summary lines, not inline.
6. **Actions card** groups the move destination and buttons together.
7. **Bottom nav** is just text links with spacing — no grey bar, no border.
   Sits at the bottom of the window padding, not in a separate strip.
8. **Warning banner** for pending reboot is inside the top card, not a
   separate row. Rounded corners, softer colours from design tokens.
9. **Window size:** 720×400 (taller, allowing more breathing room).
   MinWidth 520, MinHeight 360.
10. **Scan overlay:** Light frosted glass (`ScanOverlayBackground`) instead of
    dark semi-transparent. Text uses `ScanOverlayText`. Progress bar gets the
    accent colour.

**Specific XAML guidance:**

- Remove the old `MoveButton` and `LinkButton` styles from Window.Resources
  (they're now in the theme dictionary).
- Use `DynamicResource` for all brush references.
- Use `{StaticResource AppFont}` for FontFamily.
- The summary bullet `●` uses `AccentPrimary` for "still used", `TextTertiary`
  for "excluded" and `AccentDanger` for "orphaned".
- The summary count text is `SubheadingSize` + `SemiBold`.
- The description text ("files still used") is `BodySize` + `TextSecondary`.
- The size display is `BodySize` + `TextTertiary`, right-aligned.
- The "details..." button uses `LinkButton` style.

**Important:** Do NOT change any bindings. All binding paths (`RegisteredFileCount`,
`OrphanedFileCount`, `ScanProgress`, `IsScanning`, `MoveDestination` etc.) must
remain exactly as they are. This is a visual-only change.

**Also modify:** `MainWindow.xaml.cs` — no changes needed (it's already minimal).

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: redesign main window with cards, custom chrome and theme support`

---

## Task 5: Splash screen redesign

**Why:** The splash screen is the "whizz-bang moment." Currently it's a dark
grey box with white text. Reference: Febriansyah Nursan — colourful, branded,
alive.

**Design:** The splash becomes a bold accent-blue (`SplashBackground`) window
with white text. It should feel confident and purposeful.

**Files to modify:**

### 1. `src/SimpleWindowsInstallerCleaner/SplashWindow.xaml`

Complete rewrite. The new layout:

```
┌──────────────────────────────────────┐
│                                      │
│   Simple Windows                     │
│   installer cleaner                  │
│                                      │
│   ████████░░░░░░░░░░░░  (progress)  │
│   Step 2/5: Enumerating products...  │
│                                      │
│                          v0.1.0-α    │
└──────────────────────────────────────┘
```

**Key changes:**
- Background: `SplashBackground` (accent blue `#0066CC`) — always blue regardless
  of light/dark theme.
- No window border, no title bar. `WindowStyle="None"`, `AllowsTransparency="True"`.
- **Rounded corners:** Wrap the content in a `Border` with `CornerRadius="12"`.
  Set the window `Background="Transparent"` and put the blue background on the
  inner border. This gives the splash rounded corners.
- App name: `HeadingSize` (20pt), white, SemiBold. Split across two lines for
  visual weight: "Simple Windows" on line 1, "installer cleaner" on line 2.
- Progress bar: Determinate, white track on lighter blue. Height 3px. The
  progress value doesn't need to be accurate — it can step at 0%, 20%, 40%,
  60%, 80%, 100% matching the 5 steps.
- Step text: `SmallSize`, `SplashTextDim` (`#BBDDFF`).
- Version: `CaptionSize`, `SplashTextDim`, bottom-right.
- Size: 480×200 (slightly wider and taller for breathing room).
- **No Topmost.** Remove `Topmost="True"` — it's annoying if the user clicks
  elsewhere during loading.

### 2. `src/SimpleWindowsInstallerCleaner/SplashWindow.xaml.cs`

Add a method to update both the step text and the progress bar value:

```csharp
public void UpdateStep(string message, double progressPercent)
{
    StepText.Text = message;
    SplashProgress.Value = progressPercent;
}
```

Keep the old `UpdateStep(string)` overload for backward compatibility, defaulting
progress to 0.

### 3. `src/SimpleWindowsInstallerCleaner/App.xaml.cs`

Update the step calls to pass progress percentages:

```csharp
splash.UpdateStep("Step 1/5: Checking system status...", 10);
await Task.Delay(400);

splash.UpdateStep("Step 2/5: Enumerating installed products...", 20);
var scanTask = viewModel.ScanWithProgressAsync(null);
await Task.WhenAll(scanTask, Task.Delay(400));

splash.UpdateStep("Step 3/5: Enumerating patches...", 50);
await Task.Delay(400);

splash.UpdateStep("Step 4/5: Finding installation files...", 70);
await Task.Delay(400);

splash.UpdateStep("Step 5/5: Calculating results...", 90);
await Task.Delay(400);
```

The progress bar jumps from 10→20→50→70→90. It reaches 100% implicitly when
the splash closes. This looks natural — step 2 (the big one) takes the longest,
then steps 3–5 blaze through.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: redesign splash screen with accent-blue background and progress bar`

---

## Task 6: Orphaned files window redesign

**Why:** This is the most data-dense window. It needs the redesign treatment
while staying functional.

**Files to modify:**

### 1. `src/SimpleWindowsInstallerCleaner/OrphanedFilesWindow.xaml`

Apply the same design language: custom chrome, `WindowBackground`, cards with
rounded corners, theme-aware colours.

**Key changes:**
- Custom title bar: "Orphaned files". No maximise button (ShowMaximise=False
  is fine — let users resize but the content works at default size).
  Actually, keep maximise — this window has variable content.
- Window size: 800×500 (slightly bigger for more breathing room).
- The file list and details panel sit in `CardBorder` styled borders with
  `CardRadius` corners and `CardShadow`.
- List item text uses `TextPrimary` for filenames, `TextSecondary` for sizes,
  `TextTertiary` for type labels.
- The excluded section header uses a subtle `BorderSubtle` divider.
- The details panel labels use `TextTertiary`, values use `TextPrimary`.
- Close button uses `SecondaryButton` style.
- Summary text uses `SmallSize`, `TextSecondary`.
- All brush references use `DynamicResource`.
- **Do NOT change any bindings or code-behind logic.** Same binding paths,
  same focus/selection behaviour.

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: redesign orphaned files window with themed cards and custom chrome`

---

## Task 7: Registered files window redesign

**Why:** Same treatment as orphaned files.

**Files to modify:**

### 1. `src/SimpleWindowsInstallerCleaner/RegisteredFilesWindow.xaml`

Same design language. Custom chrome, cards, themed colours.

**Key changes:**
- Custom title bar: "Registered files".
- Window size: 850×600 (slightly bigger).
- Products list, patches panel and details panel all in `CardBorder` borders.
- The `ListView` + `GridView` for products: style the column headers with
  `TextTertiary` colour, `CaptionSize`. Cells use `TextPrimary`/`TextSecondary`.
  **Note:** GridView column headers require a custom `GridViewColumnHeader`
  style. Add one to the Window.Resources:
  ```xml
  <Style TargetType="GridViewColumnHeader">
      <Setter Property="Background" Value="{DynamicResource SurfaceBackground}"/>
      <Setter Property="Foreground" Value="{DynamicResource TextTertiary}"/>
      <Setter Property="FontSize" Value="{StaticResource CaptionSize}"/>
      <Setter Property="FontFamily" Value="{StaticResource AppFont}"/>
      <Setter Property="Padding" Value="8,6"/>
      <Setter Property="BorderThickness" Value="0,0,0,1"/>
      <Setter Property="BorderBrush" Value="{DynamicResource BorderSubtle}"/>
  </Style>
  ```
- Section headers ("Products", "Patches", "Product details") use `SectionHeader`
  style equivalent: `SubheadingSize`, `SemiBold`, `TextPrimary`.
- Close button uses `SecondaryButton`.
- **Do NOT change bindings.**

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: redesign registered files window with themed cards and custom chrome`

---

## Task 8: Settings window redesign

**Why:** Same treatment.

**Files to modify:**

### 1. `src/SimpleWindowsInstallerCleaner/SettingsWindow.xaml`

**Key changes:**
- Custom title bar: "Settings". No minimise or maximise buttons
  (`ShowMinimise="False"` `ShowMaximise="False"`).
- Window size: 480×420 (slightly bigger for breathing room).
- The exclusion filter description text uses `TextSecondary` and `TextTertiary`
  from theme.
- The filter list sits in a `CardBorder` styled border.
- The TextBox for new filter input gets rounded corners (wrap in a Border with
  `ButtonRadius`, or use a `TextBox` style with a custom template).
- Add button uses `SecondaryButton` style (not primary — "Save" is primary).
- Save button uses `PrimaryButton` style.
- Cancel button uses `SecondaryButton` style.
- The `×` remove button on each filter row: use `TextTertiary` colour, hover
  to `AccentDanger`.
- Checkbox text uses `BodySize`, `TextPrimary`.
- **Do NOT change bindings.**

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: redesign settings window with themed controls and custom chrome`

---

## Task 9: About window redesign

**Why:** Same treatment. The about window is small and simple.

**Files to modify:**

### 1. `src/SimpleWindowsInstallerCleaner/AboutWindow.xaml`

**Key changes:**
- Custom title bar: "About". No minimise or maximise buttons.
- Window size: 420×220 (slightly bigger).
- No resize (`ResizeMode="NoResize"` remains).
- App name uses `HeadingSize`, `TextPrimary`.
- Version, tagline, licence text use `SmallSize`, `TextTertiary`.
- Hyperlink uses `AccentPrimary` colour.
- Close button uses `SecondaryButton` style.
- Centre the content more generously with `WindowPadding`.
- **Do NOT change code-behind.**

**Verify:** `dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: redesign about window with themed layout and custom chrome`

---

## Task 10: Move/delete progress overlay redesign

**Why:** The current overlay is a dark semi-transparent rectangle with white
text. Reference: Naveen Yellamelli — clean Windows 11 file transfer dialogs
with percentage, file count, progress bars.

This is the biggest behavioural change in the redesign. Currently, move/delete
operations show an indeterminate progress bar with a text message. We want to
show a proper progress card with:
- Operation name ("Moving files..." / "Deleting files...")
- File count progress ("3 of 12 files")
- A determinate progress bar
- Percentage text

**This requires ViewModel changes.** The move/delete services already report
progress via `IProgress<string>`. We need to change them to report structured
progress (file index, total count) instead of just a string message.

**Files to modify:**

### 1. `src/SimpleWindowsInstallerCleaner/Models/OperationProgress.cs` (new)

```csharp
namespace SimpleWindowsInstallerCleaner.Models;

public sealed record OperationProgress(
    int CurrentFile,
    int TotalFiles,
    string CurrentFileName);
```

### 2. `src/SimpleWindowsInstallerCleaner/Services/IMoveFilesService.cs`

Add an overload or change the existing signature to accept
`IProgress<OperationProgress>` instead of `IProgress<string>`. **Or better:**
keep both. Add a new method:

```csharp
Task<MoveResult> MoveFilesAsync(
    IReadOnlyList<string> filePaths,
    string destination,
    IProgress<OperationProgress>? progress);
```

### 3. `src/SimpleWindowsInstallerCleaner/Services/MoveFilesService.cs`

Implement the new overload. In the loop that moves files, report:
```csharp
progress?.Report(new OperationProgress(i + 1, filePaths.Count, Path.GetFileName(filePaths[i])));
```

### 4. `src/SimpleWindowsInstallerCleaner/Services/IDeleteFilesService.cs`

Same pattern — add `IProgress<OperationProgress>` overload.

### 5. `src/SimpleWindowsInstallerCleaner/Services/DeleteFilesService.cs`

Implement the new overload.

### 6. `src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs`

Add new observable properties for structured progress:

```csharp
[ObservableProperty] private int _operationCurrentFile;
[ObservableProperty] private int _operationTotalFiles;
[ObservableProperty] private string _operationCurrentFileName = string.Empty;
[ObservableProperty] private double _operationProgressPercent;
```

Update `MoveAllAsync` and `DeleteAllAsync` to use the new progress overloads:

```csharp
var progress = new Progress<OperationProgress>(p =>
{
    OperationCurrentFile = p.CurrentFile;
    OperationTotalFiles = p.TotalFiles;
    OperationCurrentFileName = p.CurrentFileName;
    OperationProgressPercent = (double)p.CurrentFile / p.TotalFiles * 100;
    OperationProgress = $"{p.CurrentFile} of {p.TotalFiles} files";
});
```

### 7. `src/SimpleWindowsInstallerCleaner/MainWindow.xaml`

Replace the operating overlay with a proper progress card:

```
┌──────────────────────────────────┐
│  ░░░░░░░░░░░░░░░░░░ (frosted)  │
│                                  │
│     ┌────────────────────┐       │
│     │  Moving files...   │       │
│     │                    │       │
│     │  ████████░░░░ 67%  │       │
│     │  8 of 12 files     │       │
│     └────────────────────┘       │
│                                  │
└──────────────────────────────────┘
```

The progress card is a `CardBorder` styled border centred in the overlay.
It contains:
- Operation title ("Moving files..." / "Deleting files...")
- A determinate progress bar (accent colour) bound to `OperationProgressPercent`
- Percentage text
- File count text

The scanning overlay stays as-is (indeterminate) since we don't have file-level
progress for the scan.

### 8. Update tests

The move/delete service interfaces changed (new overloads). Update the Moq
setups in the test project if any tests mock these services. The existing
`IProgress<string>` overloads should still exist, so existing tests should
still compile. But verify.

**Verify:** `dotnet test src/SimpleWindowsInstallerCleaner.Tests` then
`dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `feat: add structured progress tracking for move and delete operations`

---

## Task 11: Final polish and consistency pass

**Why:** After all individual windows are redesigned, do a pass to ensure
everything is consistent.

**Checklist:**

1. **Build and run** — verify every window opens without exceptions.
2. **Check all `DynamicResource` references** — make sure no hardcoded hex
   colours remain in any XAML file. Every colour should come from the theme
   dictionary. Search for `#` in XAML files — the only hex values allowed are
   in the theme dictionaries themselves.
3. **Check all FontFamily references** — every text element should use
   `{StaticResource AppFont}` or inherit from a parent that does. Set
   `FontFamily="{StaticResource AppFont}"` on each window's root element so
   all children inherit it.
4. **Check spacing consistency** — `WindowPadding` on all window content grids,
   `CardPadding` inside all cards, `SectionSpacing` between sections.
5. **Check button styles** — primary actions use `PrimaryButton`, secondary
   use `SecondaryButton`, destructive use `DangerButton`, inline links use
   `LinkButton`.
6. **Remove any dead styles** — the old `MoveButton` and `LinkButton` styles
   in MainWindow.xaml Resources should be gone (they're in the theme now).
7. **Test dark theme** — temporarily change `IsSystemDarkTheme()` to return
   `true` and rebuild. Verify all windows look correct in dark. Then revert.
8. **Verify tests still pass** — `dotnet test src/SimpleWindowsInstallerCleaner.Tests`

**Verify:** `dotnet test src/SimpleWindowsInstallerCleaner.Tests` then
`dotnet build src/SimpleWindowsInstallerCleaner`

**Commit:** `chore: final polish pass for round 7 UI redesign`

---

## Summary of changes

| Task | Type | What |
|------|------|------|
| 1 | Foundation | Light + dark theme resource dictionaries with all design tokens |
| 2 | Foundation | Theme detection service (follows Windows system theme) |
| 3 | Foundation | Custom title bar UserControl with window chrome |
| 4 | UI | Main window: cards, spacing, themed colours, custom chrome |
| 5 | UI | Splash: accent-blue, rounded corners, determinate progress bar |
| 6 | UI | Orphaned files: cards, themed colours, custom chrome |
| 7 | UI | Registered files: cards, themed colours, custom chrome |
| 8 | UI | Settings: themed controls, rounded inputs, custom chrome |
| 9 | UI | About: themed layout, custom chrome |
| 10 | Feature | Structured move/delete progress with file count and percentage |
| 11 | Polish | Consistency pass, remove hardcoded colours, verify dark theme |

---

## Execution notes for Sonnet

1. **Do tasks in order.** Tasks 1–3 are foundations. Tasks 4–9 are independent
   window rewrites that depend on 1–3. Task 10 changes behaviour. Task 11 is
   the final pass.
2. **Build after every task.** The plan says when to verify — do it.
3. **Commit after every task.** Small, focused commits.
4. **Don't add features not in this plan.** No animations, no transitions, no
   extra controls. Keep it simple.
5. **British English in all user-facing strings.** Sentence case. No Oxford comma.
6. **Use DynamicResource for brushes.** StaticResource for everything else.
7. **The design system is the source of truth.** Don't invent new colours, font
   sizes or spacing values. If something isn't in the design system, use the
   closest token.
8. **Preserve all bindings.** The ViewModels don't change (except task 10).
   Every binding path must remain identical.
9. **Task 5 (splash) is the showstopper.** It's the first thing users see.
   Make it look good. The rounded corners on a borderless window require
   `AllowsTransparency="True"` and a transparent window background with the
   blue on an inner Border.
10. **Task 10 is the most complex.** It changes interfaces and view models.
    Take care with the service interface changes — add overloads, don't break
    existing signatures.
11. **Light theme is the primary target.** Get light right first. Dark is
    derived by swapping colour values. If something looks wrong in dark,
    adjust the Dark.xaml colours — don't add conditional logic.

---

## What this does NOT include (future rounds)

- **Animations/transitions** — no entrance animations, no fade-in/fade-out.
  That's a separate enhancement.
- **Custom scrollbar styling** — default WPF scrollbars. Can be styled later.
- **System tray** — not needed for this app.
- **Real-time theme switching** — the app detects the theme at startup. If the
  user changes Windows theme while the app is running, they need to restart.
  Runtime switching is a possible future enhancement.
- **Icon/logo** — the app needs an icon. That's a separate task.
