# Steam Controller Bridge v0.2.0

Early public test build for using the 2026 Steam Controller outside Steam.

Steam Controller Bridge makes the controller appear as a virtual Xbox 360 controller for XInput games, including Game Pass, Epic, emulators, and other games that do not reliably work through Steam shortcuts.

## Highlights

- One-switch bridge: turn it on and the controller appears as an Xbox controller.
- Full button remapping for ABXY, bumpers, stick clicks, View/Menu, Steam/Guide, D-pad, and L4/L5/R4/R5.
- Per-button Turbo toggles for rapid-fire style input.
- Optional trackpad mouse mode.
- Optional gyro mouse mode.
- Rumble toggle with best-effort haptic passthrough.
- Dark mode.
- Start with Windows.
- Automatic Steam handoff and reconnect attempts.
- Installer and portable ZIP builds.

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

## Known Issues

- ViGEmBus is retired and is used as a practical MVP backend.
- Native Switch/DSU motion output is not implemented yet.
- Gyro currently maps to mouse movement.
- Trackpad-as-stick is not implemented yet.
- HidHide duplicate-device handling is not integrated yet.
- Rumble is best-effort and may need more tuning.
- Remapping is currently global rather than per-game.

## Assets

Upload both files to this GitHub Release:

- `SteamControllerBridgeSetup-0.2.0.exe`
- `SteamControllerBridge-win-x64-self-contained-0.2.0.zip`
