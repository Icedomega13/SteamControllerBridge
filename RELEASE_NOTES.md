# Steam Controller Bridge v0.10.1

Focused polish build for calmer, more usable haptics.

## Highlights

- Presents the 2026 Steam Controller as a virtual Xbox 360 controller for XInput games.
- Supports Valve `0x1302`, `0x1303`, and `0x1304` Steam Controller HID product IDs.
- Includes full button remapping, keyboard mapping, turbo, presets, and saveable profiles.
- Includes mouse/gyro tools, Old School FPS mode, trackpad-as-mouse, and trackpad-as-stick mode.
- Includes experimental DSU/Cemuhook motion output for emulator testing.
- Includes an Input Test page with live controller state, rumble intensity, test rumble, test chime, and local MIDI haptic playback.

## Changes in 0.10.1

- Reworked game rumble passthrough so XInput rumble is less harsh on the Steam Controller haptics.
- Added a softer non-linear rumble curve to preserve subtle effects and tame full-strength spikes.
- Changed the default rumble intensity from `100%` to `50%`.
- Changed the rumble slider range to `0-100%`.
- Added `5%` snapping to the rumble intensity slider.
- Changed double-click reset on the rumble slider to return to `50%`.
- Added red/green tray icon status dots for inactive/active bridge state.

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

- `SteamControllerBridgeSetup-0.10.1.exe`
- `SteamControllerBridge-win-x64-self-contained-0.10.1.zip`
