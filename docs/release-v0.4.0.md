# Steam Controller Bridge v0.4.0

Early public test build for using the 2026 Steam Controller outside Steam.

This release focuses on the app UI. Advanced settings are no longer one giant page; they are split into compact tabs for presets, button remaps, motion settings, and logs.

## Highlights

- Cleaner modern main window.
- Advanced settings split into tabs:
  - Presets
  - Buttons
  - Motion
  - Logs
- Button remapping no longer requires maximizing the app across the whole desktop.
- Light and dark theme styling refreshed.
- Existing 0.3.0 features remain:
  - remap presets
  - full button remapping
  - per-button Turbo
  - gyro mouse or gyro right-stick output
  - trackpad mouse
  - rumble toggle
  - Steam handoff/reconnect

## Requirements

- Windows 10 or newer.
- ViGEmBus installed.
- 2026 Steam Controller by USB or Steam Controller Puck.
- Steam should be closed while using the bridge.

## Known Issues

- ViGEmBus is retired and is used as a practical MVP backend.
- Native Switch/DSU motion output is not implemented yet.
- Trackpad-as-stick is not implemented yet.
- HidHide duplicate-device handling is not integrated yet.
- Rumble is best-effort and may need more tuning.
- Remapping is currently global rather than per-game.

## Assets

Upload both files to this GitHub Release:

- `SteamControllerBridgeSetup-0.4.0.exe`
- `SteamControllerBridge-win-x64-self-contained-0.4.0.zip`
