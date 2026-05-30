# Steam Controller Bridge v0.10.3

Focused polish build for profile automation, update checks, and smoother haptics.

## Highlights

- Presents the 2026 Steam Controller as a virtual Xbox 360 controller for XInput games.
- Supports Valve `0x1302`, `0x1303`, and `0x1304` Steam Controller HID product IDs.
- Includes full button remapping, keyboard mapping, turbo, presets, and saveable profiles.
- Includes mouse/gyro tools, Old School FPS mode, trackpad-as-mouse, and trackpad-as-stick mode.
- Includes experimental DSU/Cemuhook motion output for emulator testing.
- Includes an Input Test page with live controller state, rumble intensity, test rumble, test chime, and local MIDI haptic playback.

## Changes in 0.10.3

- Added a GitHub update checker on the Logs page.
- Added profile hooks so saved profiles can automatically apply when selected game/app executables are running.
- Improved Smart MIDI haptic playback with better channel handling and a gentler `25%` pad melody boost.
- Refined natural game rumble translation so effects feel less harsh and more game-like.
- Added the running app version to startup logs for easier support.
- This release does not include the experimental Game Bar control surface.

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
- DSU/Cemuhook motion output is experimental and may need per-emulator tuning.
- HidHide duplicate-device handling is not integrated.
- Remapping is currently global rather than per-game.
- Haptic behavior is still best-effort and may need more game-by-game tuning.

## Release Assets

- `SteamControllerBridgeSetup-0.10.3.exe`
- `SteamControllerBridge-0.10.3-win-x64-portable.zip`
