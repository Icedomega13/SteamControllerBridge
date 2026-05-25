# Steam Controller Bridge v0.8.0

Public test build focused on profile quality-of-life, input testing, startup behavior, haptic feedback, and a cleaner Motion page.

## Highlights

- Presents the 2026 Steam Controller as a virtual Xbox 360 controller for XInput games.
- Supports Valve `0x1302`, `0x1303`, and `0x1304` Steam Controller HID product IDs.
- Includes full button remapping, keyboard mapping, turbo, presets, and saveable profiles.
- Includes mouse/gyro tools, Old School FPS mode, and trackpad-as-mouse mode.
- Includes an Input Test page with front/back controller views and live physical input indicators.
- Includes optional haptic power chimes when the bridge turns on or off.
- Includes Start with Windows, Start minimized to tray, and automatic Steam handoff behavior.

## Changes in 0.8.0

- Added a Start minimized to tray option for quieter background launches.
- Added an Input Test page with front/back controller views and live physical input values.
- Added startup profile selection.
- Added profile duplicate and reset-default actions.
- Added overwrite confirmation and unsaved-change status for profiles.
- Added optional haptic power chimes for bridge activation and deactivation.
- Moved gyro aim enable/activation controls into the Motion page.
- Restored larger sidebar navigation icon sizing.

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
- Trackpad-as-stick is not implemented yet.
- HidHide duplicate-device handling is not integrated yet.
- Remapping is currently global rather than per-game.
- Rumble and haptic chimes are best-effort and may need more hardware tuning.

## Release Assets

- `SteamControllerBridgeSetup-0.8.0.exe`
- `SteamControllerBridge-win-x64-self-contained-0.8.0.zip`
