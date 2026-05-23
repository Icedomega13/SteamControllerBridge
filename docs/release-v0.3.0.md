# Steam Controller Bridge v0.3.0

Early public test build for using the 2026 Steam Controller outside Steam.

This release adds preset remap profiles and a first pass at gyro-to-right-stick aiming. The app still keeps the main experience simple: turn it on and the controller appears as a virtual Xbox 360 controller.

## Highlights

- One-click remap presets:
  - Default Xbox
  - Nintendo Swap
  - FPS Gyro Mouse
  - FPS Gyro Stick
  - Desktop Mouse
- Gyro can now output to either desktop mouse movement or the virtual Xbox right stick.
- Full button remapping remains available in Advanced.
- Per-button Turbo toggles remain available in Advanced.
- Trackpad mouse, rumble toggle, dark mode, startup, and Steam handoff are still included.

## Requirements

- Windows 10 or newer.
- ViGEmBus installed.
- 2026 Steam Controller by USB or Steam Controller Puck.
- Steam should be closed while using the bridge.

## First Run

1. Install ViGEmBus if needed.
2. Install Steam Controller Bridge or extract the portable ZIP.
3. Close Steam.
4. Connect the controller.
5. Open Steam Controller Bridge and click `On`.

## Notes

- For Nintendo-style layouts, apply the Nintendo Swap preset.
- For games that support right-stick aiming, try the FPS Gyro Stick preset.
- For desktop-like cursor aiming, try FPS Gyro Mouse.
- Gyro-to-stick is intentionally simple in this build and may need sensitivity tuning later.

## Known Issues

- ViGEmBus is retired and is used as a practical MVP backend.
- Native Switch/DSU motion output is not implemented yet.
- Trackpad-as-stick is not implemented yet.
- HidHide duplicate-device handling is not integrated yet.
- Rumble is best-effort and may need more tuning.
- Remapping is currently global rather than per-game.

## Assets

Upload both files to this GitHub Release:

- `SteamControllerBridgeSetup-0.3.0.exe`
- `SteamControllerBridge-win-x64-self-contained-0.3.0.zip`
