# Steam Controller Bridge v0.9.0

Public test build focused on trackpad-as-stick support, haptic tuning, MIDI haptic playback, and layout polish.

## Highlights

- Presents the 2026 Steam Controller as a virtual Xbox 360 controller for XInput games.
- Supports Valve `0x1302`, `0x1303`, and `0x1304` Steam Controller HID product IDs.
- Includes full button remapping, keyboard mapping, turbo, presets, and saveable profiles.
- Includes mouse/gyro tools, Old School FPS mode, trackpad-as-mouse, and trackpad-as-stick mode.
- Includes an Input Test page with front/back controller views, live physical input indicators, rumble intensity, test rumble, test chime, and local MIDI haptic playback.
- Includes optional haptic power chimes when the bridge turns on or off.
- Includes Start with Windows, Start minimized to tray, and automatic Steam handoff behavior.

## Changes in 0.9.0

- Added trackpad-as-stick output with selectable source pad and virtual left/right stick output.
- Added trackpad-as-stick sensitivity, deadzone, and vertical invert controls.
- Added rumble intensity control and haptic test actions to the Input Test page.
- Added local MIDI haptic chime playback for user-provided MIDI files.
- Improved MIDI haptic playback timing and loudness.
- Improved wide-window behavior with a centered max-width content area.
- Increased the default app window size so normal controls are visible without manual stretching.

## Requirements

- Windows 10 or newer.
- ViGEmBus installed.
- Steam closed while using this MVP.

## First Run

1. Install ViGEmBus if needed.
2. Install Steam Controller Bridge or extract the portable ZIP.
3. Close Steam.
4. Connect the controller.
5. Open Steam Controller Bridge and click the controller icon in the upper-left corner.

## Known Limitations

- ViGEmBus is retired and is used here as a practical MVP backend.
- Native Switch/DSU motion output is not implemented yet; gyro currently maps to mouse or virtual right-stick movement.
- HidHide duplicate-device handling is not integrated yet.
- Remapping is currently global rather than per-game.
- Rumble, haptics, and MIDI playback are best-effort and may need more hardware tuning.

## Release Assets

- `SteamControllerBridgeSetup-0.9.0.exe`
- `SteamControllerBridge-win-x64-self-contained-0.9.0.zip`
