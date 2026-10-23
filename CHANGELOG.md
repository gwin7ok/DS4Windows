# Changelog

All notable changes to this project are recorded in this file.

## [Unreleased]

## [4.0.1] - 2026-10-23

### Changed
- The default app theme is now the light theme (it used to follow the Windows apps theme mode). The dark theme can still be chosen in Settings.
- Dark theme: the background is now a dark gray, and buttons, combo boxes, column headers and inactive tabs use a lighter surface color. Borders (buttons, group boxes, lists, tab pages, scroll bars) and the pane divider in the profile editor are brighter, so their edges are easy to see.
- Dark theme: all text is white (disabled controls are only slightly dimmed).
- Dark theme: text that cannot be changed (disabled) uses one light gray (`#C0C0C0`) everywhere, including number boxes, text boxes and labels; expanders, tool bars and color pickers now follow the theme.
- Dark theme: links (Settings tab, About, welcome dialogs), warning messages (red) and the Controller Readings labels (L2/R2 values, input delay) now use colors that are readable on the dark background; disabled menu items use the same light gray as other disabled text.
- Dark theme: tabs that cannot be switched to (disabled) are shown with the page background and the light gray disabled text color, so they are distinguishable from the other tabs.
- Dark theme: hyperlinks (Settings tab and others) use a lighter blue that is easier to read.
- Dark theme: the output picker window ("Select action for ...") now follows the theme (keyboard keys, number boxes and the Extra panel were light gray / white), and the selected output uses a green that white text is readable on.
- Dark theme: lists have alternating row shading and more row padding, and column headers have separators (easier to read, closer to the light theme).

### Fixed
- Dark theme: the text of a hovered tab was unreadable (white on white), list text was dark blue and the Special Actions names were black.

## [4.0.0] - 2026-10-23

This is a major release. The internal architecture was rebuilt on dependency injection, and the Controls and Special Actions editors were unified. The unreleased 3.11.7 changes are included.

**Back up your profile folder (`Profiles` and `Actions.xml`) before updating.** Profiles and special actions saved by 4.0.0 may not be readable by 3.x.

Assignment editor
- Controls tab and Special Actions (SA) tab now share one assignment editor and one output picker. Macro, Launch Program, Load Profile, Disconnect BT, Calibrate gyro / sixaxis, Check Battery and Multi-action can be assigned from either tab.
- Switch Mode (Press / Toggle) is available for every assignment. A Toggle set on a Default assignment is now kept instead of being discarded.
- SA can now have Extras (an additional key output that is active while the SA is satisfied).
- Added a "Reset all to Default" button on the Controls tab. Default assignments are shown as Output Key / Mouse / Button, and "Default (No matching button)" is distinguished from "Unbound".
- Added a Duplicate button on the Special Actions tab (the copy is added disabled, named "<name> (Copy)"). Double-clicking an SA row opens its editor.
- Touchpad and Gyro tabs were moved into the left pane; the Controls list order for LS/RS Outer and L2/R2 Full Pull was adjusted.
- SA trigger detection for analog inputs (triggers, stick axes) is now digital (no threshold). When a lone trigger drives an analog output, the output follows the input value.
- Special actions and profiles are now referenced by fixed IDs instead of names.

Fixes
- Fixed a crash when clicking the gyro calibration button while no controller was connected in that slot.
- Fixed a possible memory corruption in asynchronous HID I/O (pending reads/writes could write into moved memory).
- Fixed a Key staying pressed after a two-stage trigger was released.
- The profile applied on reconnect is now managed per controller (MAC address) instead of per slot, so a different controller no longer receives another controller's profile.
- Fixed the command-line query always returning an empty result.
- Log rotation and diagnostic logging now run after the single-instance check, so a second instance no longer disturbs the running one.
- UI scrolling with the mouse wheel now follows the Windows "scroll one screen at a time" setting.
- Fixed the link to the virtual controller (Moonlight) support document, which pointed to a branch that does not exist.

Removed features
- Removed "360 Steering Wheel" emulation (gyro steering wheel, its calibration special action, and the related profile and controller settings).
- Removed the XboxGameDVR special action.

Internal
- Rebuilt on dependency injection and a layered structure: `Global` and the static service holders were removed, `Mapping.cs` and `ControlService.cs` were split into focused components, and special actions run on a unified trigger-dispatch engine.
- Repository references (update check, release page, updater) are centralized in `ReleaseSource`.
- `ds4w.bat` and `scripts/release-and-publish.ps1` no longer hard-code the repository.

## [3.11.6] - 2025-12-14

Administrator privileges
- Starting with this release, the application always launches in administrator (elevated) mode.

Update functionality
- Customized and integrated `DS4Updater.exe` so that the updater works with this fork.

Automatic update check on startup
- Fixed behavior so that setting the interval number (days or hours) to `0` causes an update check to run on every application startup.

## [3.11.5] - 2025-12-11

Features / UI
- (a47a68e8) Split original "Selected Profile" column into "Linked Profile" and "Selected Profile" columns, separating functionality between them.
- (49c618c4) Fix DisconnectBT action auto-check processing; activate based on profile actions.
 - (b3a335bb, f48cd019) Add controller tab column width save/restore functionality with adjusted default values.
 - (11e77fad, e172e98d) Add an "Apply" button to language selection so users can apply language changes at runtime without restarting the application; the apply button becomes enabled when a new language is selected.

Profile Management
- (9a421270) Update all device settings after profile save and adjust backlight colors to match new profile.

Logging improvements
- (d9384cfb) Add log settings with max archive file count and minimum log level properties. Add UI bindings for log settings and implement log configuration updates on setting changes.

Window placement / DPI
- (ee5b7e29) Improve per-monitor DPI handling for window placement: add logic converting between logical and physical pixels when calling Win32 APIs and verify placement after SetWindowPlacement.

UI / Localization
- (e172e98d, cbbb46fc, 11e77fad, 18e447ad, ec5290ba) Add/adjust language application methods so language selection is applied at first run and can be persisted; update localization application for the welcome/installer dialogs.

Docs
- (779fa4a3) Add a new DS4Windows introduction guide covering installation, setup, controllers, troubleshooting and FAQ.

Bug fixes
- (30a50d1c) Fix crash when clicking the Finished button on the driver/setup welcome window: strengthen stopping/disposal of the internal monitor timer and ensure the dialog closes even if exceptions occur.

## [3.11.4] - 2025-12-09

Features / UI
- (e768ac1c) Change special action save/delete key from index to action name; add duplicate name check on save to prevent duplicate special actions from appearing in the list.
- (6e208281) Sort special action list after saving changes (new/edit) to maintain consistent ordering.
- (afd07be9) Fix issue where editing special actions while a controller is connected caused continuous "ActionDone list size mismatch" output and system slowdown.
- (bdca5fad) Normalize special action names and use case-insensitive comparison; update current profile references when deleting actions.

Bug fixes / Build improvements
- (6dec4505) Fix build errors and warnings; limit to x64 platform and remove duplicate Russian language file registration.
- (b906590a) Suppress error display in Problems tab for issues that don't occur at build stage.

## [3.11.3] - 2025-11-12

Features / UI
- (ceb2b330) Display "(invalid special action)" in the action column and gray out invalid special action names in the Special Actions list; users can remove them by unchecking and saving.
- (4c33ea94) When an invalid special action is removed from a profile file, show an info-style GUI/tray notification after the file is written: "Profile '{displayProfile}' removed invalid special action '{name}' from its action list."
- (777420ba) Fix saving and restoring of column widths and left/right area widths so layout changes persist across runs.
- (bbd837c2) Set the default checkbox column width to 32 and make it restorable via the window init button.
- (134544ee) Improve column header sort indicators and behavior; clicking a header toggles ascending/descending sort and shows ▲/▼.
- (5f0a39d7) Increase default window and profile-editor left-area widths for a better default layout.

Bug fixes
- (204ab347) Fix incorrect column-name object assignment when closing the Profile Editor to prevent layout/assignment issues.
- (a5bb8fb5) Fix a bug where deleted or renamed special action names left in profiles produced continuous debug-level errors.
- (8d44d589) and related commits: stabilize initial display/sort behavior and fix cases where sort indicators were inconsistent at first display.

Internationalization / Messages
- (31a85068) Replace Japanese hard-coded log/UI text with English (localization references) for more consistent user-facing messages.

Other user-facing fixes
- (83cbd5b1 / b5db9c18 / 7e78c34c) Update update-check URI and several in-app URLs so links and update checks point to the correct locations.
- Various UI layout and initialization tweaks: initial-window values, init button placement, and split/resize behavior improvements across several commits.

## [3.11.2] - 2025-11-06

CI
- (c8cd8a8d) Fix auto-release workflow (`auto-release.yml`).
- (32246d73) Fix auto-release workflow (`auto-release.yml`).
- (d25340f3) Fix auto-release workflow (`auto-release.yml`).
- (18838ffd) CI/Docs: remove version description from auto-release workflow.

Fixes
- (4f237854) Fix profile switching when a Special Action is registered in actions.xml at a position other than the end (could cause profile-switch failure after startup). Also: test fixes and ensure language files are saved to `lang/` at build time.

Docs / Changelog
- (87eb4562) Fix README (typos/formatting).

Chore
- (b0ba181f) Exclude `C:\Program Files\DS4Windows` (packaging/ignore adjustments).

> Note: CI/GitHub Actions will extract the section matching the version in `Directory.Build.props` to populate release notes.

## [3.11.1] - 2025-11-05

CI
- (8f272340) Fix PowerShell syntax in workflow (use proper PS conditional and error handling).
- (846dcc86) Workflow fixes.
- (09671a04) Misc workflow file fixes.
- (72d6d210) Replace deprecated create-release action with `gh` CLI for reliable release creation.
- (f6298837) Workflow fixes.
- (fe09d2ef) Revert to working v3.11.0 workflow structure while applying v3.11.1 content.
- (acc564d0) Fix workflow: create release after build (move create-release step to after successful build/publish).
- (98e07393) Update GitHub Actions to use modern `gh` CLI instead of deprecated actions.

Features / UI
- (0112ced9) Display active profile notifications using a custom top-right window (focus-independent).
- (ddb01d9f) Use custom window to show the active profile name in notifications.

Fixes
- (ef916d53) Ensure notifications display the actual active profile name.

Docs / Changelog
- (1bf98a7f) Update release description for v3.11.1 features.
- (b64bef1a) Update Changelog for v3.11.1 — custom notification system and profile accuracy improvements.

Chore / Misc
- (cd190051) Configure error-suppression settings (attempt to reduce noisy errors).
- (d4cfb1c8) WIP: attempted fixes for an error (investigation/attempted patch).
- (722f7799) Remove obsolete profile-notification code from `Log.cs`.

## [3.11.0]
- Special Action Button Suppression Fix: when a Special Action is triggered, the last pressed button's other assigned function is now prevented from executing alongside the Special Action to avoid duplicate inputs.

