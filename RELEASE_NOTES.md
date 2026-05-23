# Steam Controller Bridge v0.2.0

Initial public MVP for testing the 2026 Steam Controller outside Steam.

## Highlights

- Presents the Steam Controller as a virtual Xbox 360 controller for XInput games.
- Supports wired and Steam Controller Puck HID interfaces.
- Handles known state report IDs `0x42` and `0x45`.
- Disables lizard mode while enabled and restores it on shutdown.
- Includes full button remapping for standard gamepad buttons and back paddles.
- Includes per-button turbo toggles for rapid-fire style presses.
- Includes optional trackpad-as-mouse mode.
- Includes simple gyro-to-mouse mode with selectable activation.
- Includes optional rumble passthrough and a rumble toggle.
- Includes Start with Windows and automatic Steam handoff/reconnect behavior.
- Includes dark mode and diagnostic logging.

## Changes in 0.2.0

- Added advanced remapping for ABXY, bumpers, stick clicks, View/Menu, Steam/Guide, D-pad, and L4/L5/R4/R5.
- Added a Turbo checkbox next to each remappable button.
- Added Start with Windows.
- Added automatic backoff when Steam opens, with reconnect attempts after Steam closes or a controller reconnects.
- Kept default behavior simple: standard buttons map normally, while L4=Y, L5=X, R4=B, and R5=A.

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
- Native Switch/DSU motion output is not implemented yet; gyro currently maps to mouse.
- Trackpad-as-stick is not implemented yet.
- HidHide duplicate-device handling is not integrated yet.
- Remapping is currently global rather than per-game.
- Rumble is best-effort and may need more hardware tuning.

## Release Assets

- `SteamControllerBridgeSetup-0.2.0.exe`
- `SteamControllerBridge-win-x64-self-contained-0.2.0.zip`
