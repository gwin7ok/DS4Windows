# Changelog

All notable changes to this project are recorded in this file.

## [Unreleased]

### Fixed
- Command line: the result of `-command Query…` was not shown when DS4Windows was started from a console without redirecting the output (it only appeared with `| more`). The result is now written to the console that started DS4Windows; redirected output (a pipe or a file) still goes to standard output. An interactive Command Prompt does not wait for DS4Windows to finish, so the result can appear after the next prompt; use `start "" /wait DS4Windows.exe -command …` to wait for it.
- Command line: non-ASCII characters in the result of `-command Query…` (for example Japanese profile names) were shown as `?`. The result is now passed as UTF-8 and written in the console's code page. An empty result is now an empty line.
- Log tab: clearing the log now also clears the one-line message at the bottom of the window.

### Changed
- UI: no visible change. The controller diagram buttons are now generated from one layout table, numeric inputs and reading labels share styles, and theme color names were unified (`…Brush` / `…Color`).
- Internal (no change in behavior): the screens now talk to the service layer through small contracts instead of `ControlService`, view models raise `PropertyChanged` through a common base, and the profile editor settings were split into eight regions (gyro, touchpad, triggers, sticks, light bar, output, general, six-axis), each with its own panel, view model and per-slot settings window. The special actions list header text (sort arrows) is now provided by its view model, and the remaining screens that read per-slot settings directly use the same per-slot settings windows.
- Log messages shown in the Log tab and as tray notifications are now translated: when the display language is Japanese they appear in Japanese (about 85 messages, for example controller connection and disconnection, ViGEmBus, UDP/OSC servers, HidHide and profile saving). The log files are always written in English so that reports stay readable. "WARNING:" / "ERROR:" prefixes at the start of messages were removed because the level is already shown. "Waking Up From Suspend" now has a Japanese translation.
- Internal (no change in behavior): dependencies between the layers were straightened out (shared value types and contracts moved to the core folders, the UI no longer refers to the application class directly), static state was removed (the bridge for sending synthetic keys, the arrays for forced light bar colors, an unused static action registry; startup entries, the change log and the HTTP client are now provided by the service container), service registration was split per layer, and the test suite was cleaned up (architecture tests for the layer dependencies, removal of obsolete and duplicate source checks, faster tests).

## [4.3.1] - 2026-11-04

### Changed
- The log line "This binary was built at …" now reads the build time and commit from the program itself, so `build_timestamp.txt` is no longer needed (it was missing from the release archive).
- Log messages were reorganized into one logging API with consistent levels and prefixes. In the log shown in the window: messages about failures that were shown as warnings are now errors, some informational messages are now warnings, and a few short-lived diagnostic messages (for example some in profile settings) are no longer shown in the window and are written to the log file at Debug level only. The tray notifications are unchanged. Debug-only `[Diag-Toast]` lines were removed, and messages that were written to the console or the debugger output now go to the log file.
- Internal (no change in behavior): the service layer was rewired to receive its dependencies directly (removing the static action manager, the service locator holder, lazy lookups and many forwarding members of the control service), the IPC command handling (`-command`) moved into its own class, input report processing was moved out of the control service, and source files were re-organized into layer folders with namespaces matching the folders.

### Fixed
- Command line: `-command LoadProfile.<n>.<name>` / `LoadTempProfile` did not work with profile names that contain non-ASCII characters (for example Japanese). Command text is now sent as UTF-8.
- Command line: `LoadProfile` did not switch the profile when the name differed from the profile's name only in upper/lower case (for example `VerifyA` for `verifyA`). The name is now matched case-insensitively. A warning is also written to the log when a `LoadProfile` command could not be applied.

### Known issues
- Command line: `-command ChangeLedColor` only changes the profile's custom light bar color; the light bar does not change visibly unless the profile's light bar mode is a custom color.

## [4.3.0] - 2026-11-03

### Added
- **Macro: mouse pointer movement.** A macro step can move the mouse pointer to a screen position ("Move to (x, y)") or to a position inside the foreground window ("Move in window (x, y)"; the origin is the top-left of the window's outer frame). Steps are added with "Add Mouse Move" in the macro recording screen and the coordinates can be edited by double-clicking the step. The new "Record mouse movement" check box (off by default) records real mouse moves and clicks made outside the recording box, either as screen or as window-relative coordinates. Macros that contain pointer moves are stored as ordinary step values; older versions read them as light bar colors, so do not go back to an older version with such macros.
- **Macro recording screen: list editing.** Select several steps with Ctrl+click or Shift+click, delete them with the Delete key, reorder them by drag and drop, and click an empty area of the list to clear the selection. When nothing is selected, Record, Insert Wait and Add Mouse Move add steps at the bottom. The buttons on the right were re-ordered.
- **Special Action / Controls: Focus a window.** A new action type brings a program's window to the front (restoring it when minimized). The target is set by program path and/or window title (match mode: contains, exact, starts with, ends with; or "executable name only"), and can be picked by dragging the target mark onto a window. An option moves the mouse pointer to the center of the window after focusing.

## [4.2.4] - 2026-11-03

### Removed
- **Debouncing** (the per-profile "Debouncing" setting and its input filter). With a large value, a pressed button stayed "held" for a while after it was released. The setting is no longer shown or saved. `<DebouncingMs>` in older profiles is ignored when the profile is loaded, and a non-zero value no longer shifts the timing of Multi-action, delays and two-stage triggers.

### Changed
- "Enable Output data to DS4" is now "Send output data to the physical controller". It works regardless of the Emulated Controller (DS4 or Xbox 360), turning it on no longer forces the Emulated Controller to DS4, and re-applying the same profile reflects the setting (turning it off switches the light bar off once).
- The button names in the macro recording screen follow the profile's Emulated Controller (for example Cross, Circle, L1, PS for DS4; A, B, LB, Guide for Xbox 360), both when recording and when a saved macro is opened.
- The hover highlight on the controller picture in the Controls tab now changes the selected row of the button list only after the pointer stays for 0.5 seconds (the highlight itself is immediate).
- A Load Profile whose target profile was deleted is shown as "Load Profile: (none)" in the Controls and Special Actions lists, like in the assignment editor.
- The duplicate-profile box starts with the name of the source profile.
- When a disconnected controller reconnects, temporary (auto) profile state is no longer kept: the usual profile is applied first, and the Auto Profile rule is checked again shortly after.

### Fixed
- Crash when deleting a profile that was used as a Linked Profile.
- Turning on the DS3 pitch/roll simulation now switches the output to DS4 again when saving or applying (and the service is restarted on save, as in older versions). This had been lost in an earlier refactoring.
- A custom Steam folder that does not exist now shows a warning (the value is still saved) instead of being dropped silently.
- A duplicated profile now gets its own profile ID right away (it kept the ID of the original until the next restart).
- Special Action "Load Profile" with a deleted target no longer disconnects the virtual controller when pressed and re-applies the previous profile on release; it does nothing and logs a message.
- The battery level shown in the log and notifications right after a controller connects was 0%; it is now read from the first report. The Controllers tab battery label is also refreshed every 60 seconds, and the "back to profile" log line after leaving an Auto Profile program shows the real battery level instead of N/A.
- After a controller was disconnected and reconnected, the Controllers tab could keep showing the previous battery level, and the profile-applied notification window could show 0%. Battery reporting is now unified: every controller type (DS4, DualSense, DS3, Switch Pro, Joy-Con) updates the battery through one path and one pair of change events, the notification window reads the level from the controller itself, and a stale list row is replaced on reconnect. Verified on a real DS4 only.
- Saving or applying a profile while the service is stopped no longer applies it to slots without a controller or logs notifications.
- The Settings tab status line is no longer hidden.
- Closing the output picker with the window's close button no longer switches a Default assignment's type to another one.
- After changing the Emulated Controller in the profile editor, the Pad tab of an already opened output picker shows the new type.
- Controller Readings: values such as 255 are no longer clipped.

## [4.2.3] - 2026-10-25

### Fixed
- Some messages in the Log tab (for example "Starting...", "Searching for controllers...", "Found Controller") and the "Output Delay" label in Controller Readings were shown in Japanese even when the language was set to "No (English UI)". They now follow the selected language: Japanese in Japanese mode, English otherwise.

## [4.2.2] - 2026-10-24

### Fixed
- Fixed an issue where the DS4 BT Poll Rate was not being reflected after making a change.

## [4.2.1] - 2026-10-24

### Changed
- Dark theme: the color palette was refreshed for a more refined look (backgrounds, surfaces, borders, text, links, warning and accent colors). The theme resources and the XAML of the dark and default themes, the profile editor, the binding window and the controller readings were reorganized and reformatted; there are no behavior changes.

### Documentation
- README: added a section about AI-assisted development (how changes are tested, verified on real hardware and documented, and the expectations for pull requests), and removed the "project has moved" notice.

## [4.2.0] - 2026-10-23

### Added
- Macros: a new Execute Mode, "Repeat (Toggle)". Press the trigger once to start repeating the macro (it keeps repeating after you release), and press again to stop (the macro in progress finishes its current run). Available for Special Actions and for Controls macros. "Run on trigger release" cannot be combined with it.

### Changed
- The macro Execute Mode is now saved as its own setting (`MacroExecuteMode` in Actions.xml and in the profile) instead of inside the Extras / KeyType text. Profiles and Actions.xml saved by earlier versions are still read correctly. Profiles saved by this version cannot be read correctly by earlier versions (a repeating macro plays once there).

## [4.1.0] - 2026-10-23

### Changed
- The project moved to https://github.com/gwin7ok/DS4Windows (formerly gwin7ok/DS4Windows-Vader4Pro). From this version on, update checks, the release page links and the updater use the new repository. Updating from 4.0.1 or earlier to this version works as usual (the old repository only delivers this release). The README and the introduction guide point to the new location.
- DS4Windows now tells DS4Updater which repository to update from (`--ds4windows-repo`), so an older DS4Updater that is already installed no longer looks at the old repository.

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

