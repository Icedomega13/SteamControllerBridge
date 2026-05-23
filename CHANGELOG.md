# Changelog

## 0.5.3

- Fixed keyboard mapping SendInput structure sizing so Windows can accept mapped key events.
- Added log messages when Windows rejects mapped keyboard events.

## 0.5.2

- Suppressed matching virtual Xbox button or trigger output when that controller input is mapped to a keyboard key.

## 0.5.1

- Switched keyboard mapping output to scan-code based input for better compatibility with apps and games.

## 0.5.0

- Added a Keyboard tab for mapping controller buttons to keyboard keys.
- Added key capture and clear controls for normal buttons, triggers, back paddles, and pad clicks.
- Keyboard mappings release cleanly when the bridge stops or the controller disconnects.

## 0.4.3

- Added a mappable gyro aim toggle button that can temporarily disable gyro while keeping the normal gyro activation trigger.
- Added LT and RT turbo toggles.
- Added a universal turbo speed slider.

## 0.4.2

- Increased default gyro-to-right-stick strength.
- Added gyro stick speed and deadzone controls to the Motion tab.

## 0.4.1

- Added L4, L5, R4, and R5 as gyro activation options.

## 0.4.0

- Redesigned the main window with a cleaner modern layout.
- Replaced the single oversized Advanced page with compact tabs for Presets, Buttons, Motion, and Logs.
- Improved default window size so controls fit without maximizing the app.
- Refreshed light and dark theme styling.

## 0.3.0

- Added one-click remap presets.
- Added Nintendo ABXY swap preset.
- Added FPS gyro mouse and FPS gyro right-stick presets.
- Added Desktop Mouse preset.
- Added gyro output mode so gyro can drive either mouse movement or the virtual right stick.

## 0.2.0

- Added full button remapping for standard Xbox-style buttons and back paddles.
- Added per-button turbo toggles.
- Added gyro mouse, trackpad mouse, rumble toggle, and dark mode.
- Added Start with Windows.
- Added automatic Steam handoff and reconnect attempts.
- Added installer and portable release packaging.
- Improved README, release notes, and first-run guidance.

## 0.1.0

- Initial MVP.
- Added one-switch WinForms tray app.
- Added direct HID detection for the 2026 Steam Controller and Steam Controller Puck.
- Added virtual Xbox 360 output through ViGEmBus.
- Added lizard mode disable/restore.
- Added custom app icon.
- Added diagnostic probe project.
