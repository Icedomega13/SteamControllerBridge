# Steam Controller Bridge v0.10.4

Game Bar widget build with controller-friendly quick controls and the latest haptic refinements.

## Highlights

- Presents the 2026 Steam Controller as a virtual Xbox 360 controller for XInput games.
- Supports Valve `0x1302`, `0x1303`, and `0x1304` Steam Controller HID product IDs.
- Includes full button remapping, keyboard mapping, turbo, presets, and saveable profiles.
- Includes mouse/gyro tools, Old School FPS mode, trackpad-as-mouse, and trackpad-as-stick mode.
- Includes experimental DSU/Cemuhook motion output for emulator testing.
- Includes an Input Test page with live controller state, rumble intensity, test rumble, test chime, and local MIDI haptic playback.
- Includes an Xbox Game Bar widget for quick in-game access to bridge power, connection state, rumble intensity, and back paddle remaps.

## Changes in 0.10.4

- Added the first real Xbox Game Bar widget.
- Added bridge on/off, subtle connection status, rumble intensity, and L4/L5/R4/R5 remapping to the widget.
- Added local control hooks so the widget can talk to the running bridge.
- Added controller-friendly rumble step controls in the widget.
- Added Start + Back hold-for-5-seconds controller shutdown to the public feature list.
- Added startup bridge support for users who want the controller to enter Xbox mode automatically.
- Kept the smoother natural rumble translation and Smart MIDI haptic playback improvements.

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

To use the optional Game Bar widget, install the widget package from the release assets, run Steam Controller Bridge, then open Xbox Game Bar and add the Steam Controller Bridge widget.

## Known Limitations

- ViGEmBus is retired and is used here as a practical MVP backend.
- DSU/Cemuhook motion output is experimental and may need per-emulator tuning.
- HidHide duplicate-device handling is not integrated.
- Remapping is currently global rather than per-game.
- Haptic behavior is still best-effort and may need more game-by-game tuning.
- The Xbox Game Bar widget is new and may need additional polishing on some display scales.

## Release Assets

- `SteamControllerBridgeSetup-0.10.4.exe`
- `SteamControllerBridge-0.10.4-win-x64-portable.zip`
- `SteamControllerBridge.GameBarWidget-0.10.4.2-msix.zip`
