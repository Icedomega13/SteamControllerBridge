# Steam Controller Bridge v0.7.0

Initial public MVP for testing the 2026 Steam Controller outside Steam.

## Highlights

- Presents the Steam Controller as a virtual Xbox 360 controller for XInput games.
- Supports wired and Steam Controller Puck HID interfaces.
- Supports Valve `0x1302`, `0x1303`, and `0x1304` Steam Controller HID product IDs.
- Handles known state report IDs `0x42` and `0x45`.
- Disables lizard mode while enabled and restores it on shutdown.
- Includes full button remapping for standard gamepad buttons and back paddles.
- Includes keyboard key mapping for controller buttons, triggers, back paddles, and pad clicks.
- Includes per-button turbo toggles for rapid-fire style presses.
- Includes remap presets for Default Xbox, Nintendo swap, FPS gyro mouse, FPS gyro right stick, Desktop Mouse, and Old School FPS.
- Includes saveable profiles with import/export support for sharing layouts.
- Includes a redesigned tabbed Advanced UI for presets, button remaps, motion, and logs.
- Includes L4, L5, R4, and R5 as gyro activation options.
- Includes a mappable gyro aim toggle button for temporarily disabling gyro aim.
- Includes gyro stick speed and deadzone tuning controls.
- Includes a universal turbo speed slider and trigger turbo toggles.
- Includes optional trackpad-as-mouse mode.
- Includes simple gyro mode with mouse or virtual right-stick output.
- Includes left-stick WASD and right-stick mouse modes for keyboard-and-mouse-only PC games.
- Includes optional rumble passthrough and a rumble toggle.
- Includes Start with Windows and automatic Steam handoff/reconnect behavior.
- Includes diagnostic logging.

## Changes in 0.7.0

- Added saveable preset profiles with load, import, export, and delete controls.
- Added an Old School FPS preset for games without controller support.
- Added left-stick WASD output, right-stick mouse aiming, mouse speed, and vertical invert controls.
- Suppressed virtual stick output when stick-to-keyboard or stick-to-mouse modes are enabled.
- Added Valve `0x1303` HID support for controllers that expose the Bluetooth-style product ID.

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

- `SteamControllerBridgeSetup-0.7.0.exe`
- `SteamControllerBridge-win-x64-self-contained-0.7.0.zip`
