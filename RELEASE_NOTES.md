# Steam Controller Bridge v0.10.0

Public test build focused on experimental DSU/Cemuhook motion output, safer controller shutdown, haptic playback expansion, and release packaging fixes.

## Highlights

- Presents the 2026 Steam Controller as a virtual Xbox 360 controller for XInput games.
- Supports Valve `0x1302`, `0x1303`, and `0x1304` Steam Controller HID product IDs.
- Includes full button remapping, keyboard mapping, turbo, presets, and saveable profiles.
- Includes mouse/gyro tools, Old School FPS mode, trackpad-as-mouse, and trackpad-as-stick mode.
- Adds an experimental DSU/Cemuhook motion server for emulator-native gyro/accelerometer testing.
- Includes an Input Test page with front/back controller views, live physical input indicators, rumble intensity, test rumble, test chime, and local MIDI haptic playback.
- Includes optional haptic power chimes when the bridge turns on or off.
- Includes Start with Windows, Start minimized to tray, and automatic Steam handoff behavior.

## Changes in 0.10.0

- Added a `DSU motion` quick toggle.
- Added a built-in DSU/Cemuhook UDP motion server on `127.0.0.1:26760`.
- Added app log guidance for emulator DSU setup.
- Added a controller shortcut to turn Bridge off by holding View + Menu for 5 seconds, with a warning chime after 3 seconds.
- Added expanded MIDI haptic playback modes for fuller pad/rumble output.
- Fixed portable and installer packaging so all PNG assets publish into the external `Assets` folder.
- Replaced inactive navigation artwork with the latest matched icon set.

## DSU Motion Notes

DSU motion is experimental in this release. The server has been confirmed reachable by DSU test tools, but real emulator/game testing is still needed for axis orientation, sensitivity, and drift tuning.

For Cemu or other DSU-compatible emulators, enable `DSU motion` in Steam Controller Bridge and add a DSU/Cemuhook motion source at:

- IP: `127.0.0.1`
- Port: `26760`

Buttons should still come from the virtual Xbox controller. DSU is intended for motion data only.

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
- Rumble, haptics, and MIDI playback are best-effort and may need more hardware tuning.

## Release Assets

- `SteamControllerBridge-win-x64-self-contained-0.10.0.zip`

Installer package: build `installer\SteamControllerBridge.iss` with Inno Setup 6 when `ISCC.exe` is available.
