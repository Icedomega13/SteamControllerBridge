# Steam Controller Bridge v0.6.0

Initial public MVP for testing the 2026 Steam Controller outside Steam.

## Highlights

- Presents the Steam Controller as a virtual Xbox 360 controller for XInput games.
- Supports wired and Steam Controller Puck HID interfaces.
- Handles known state report IDs `0x42` and `0x45`.
- Disables lizard mode while enabled and restores it on shutdown.
- Includes full button remapping for standard gamepad buttons and back paddles.
- Includes keyboard key mapping for controller buttons, triggers, back paddles, and pad clicks.
- Includes per-button turbo toggles for rapid-fire style presses.
- Includes remap presets for Default Xbox, Nintendo swap, FPS gyro mouse, FPS gyro right stick, and Desktop Mouse.
- Includes a redesigned tabbed Advanced UI for presets, button remaps, motion, and logs.
- Includes L4, L5, R4, and R5 as gyro activation options.
- Includes a mappable gyro aim toggle button for temporarily disabling gyro aim.
- Includes gyro stick speed and deadzone tuning controls.
- Includes a universal turbo speed slider and trigger turbo toggles.
- Includes optional trackpad-as-mouse mode.
- Includes simple gyro mode with mouse or virtual right-stick output.
- Includes optional rumble passthrough and a rumble toggle.
- Includes Start with Windows and automatic Steam handoff/reconnect behavior.
- Includes dark mode and diagnostic logging.

## Changes in 0.6.0

- Redesigned the main window with a professional dark sidebar layout.
- Added a larger status header and prominent On/Off control.
- Reworked quick controls and advanced navigation around Presets, Buttons, Keyboard, Motion, and Logs.
- Added custom rounded panel styling and refreshed dark theme colors.

## Requirements

- Windows 10 or newer.
- ViGEmBus installed.
- Steam closed while using this MVP.

## First Run

1. Install ViGEmBus if needed.
2. Install Steam Controller Bridge or extract the portable ZIP.
3. Close Steam.
4. Connect the controller.
5. Open Steam Controller Bridge and click `On`.

## Known Limitations

- ViGEmBus is retired and is used here as a practical MVP backend.
- Native Switch/DSU motion output is not implemented yet; gyro currently maps to mouse or virtual right-stick movement.
- Trackpad-as-stick is not implemented yet.
- HidHide duplicate-device handling is not integrated yet.
- Remapping is currently global rather than per-game.
- Rumble is best-effort and may need more hardware tuning.

## Release Assets

- `SteamControllerBridgeSetup-0.6.0.exe`
- `SteamControllerBridge-win-x64-self-contained-0.6.0.zip`
