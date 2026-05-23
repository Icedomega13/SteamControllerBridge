# Steam Controller Bridge

Steam Controller Bridge is a tiny Windows tray app that makes the 2026 Steam Controller appear as a virtual Xbox 360 controller.

The goal is intentionally simple: connect the controller, turn the bridge on, and launch an XInput game. No Steam shortcut setup, no profile editor, no giant dashboard.

## Current Status

This is an early MVP. It works by reading the Steam Controller's raw HID reports and forwarding standard gamepad inputs to a virtual Xbox 360 controller through ViGEmBus.

## Features

- One-switch tray app
- Direct HID input from the 2026 Steam Controller / Steam Controller Puck
- Virtual Xbox 360 output for XInput games
- Best-effort Xbox rumble passthrough to Steam Controller haptics
- Rumble enable/disable toggle
- Back paddle mapping for L4, L5, R4, and R5
- Optional trackpad-as-mouse mode
- Optional dark mode
- Lizard mode disable while enabled
- Lizard mode restore on normal shutdown
- Custom app/tray icon
- Small diagnostic probe project for controller/interface debugging

## Requirements

- Windows 10 or newer
- .NET 9 Desktop Runtime for framework-dependent builds
- ViGEmBus installed
- 2026 Steam Controller, wired or through the Steam Controller Puck

Steam should be closed for this MVP. Steam can claim the controller and interfere with direct HID access.

ViGEmBus is retired/end-of-life, so it is treated as a practical MVP backend rather than the ideal long-term foundation.

## Build

```powershell
dotnet build .\SteamControllerBridge.sln -c Release
```

## Run From Source

```powershell
dotnet run --project .\SteamControllerBridge\SteamControllerBridge.csproj
```

## Publish

```powershell
dotnet publish .\SteamControllerBridge\SteamControllerBridge.csproj -c Release -r win-x64 --self-contained true -o .\dist\SteamControllerBridge-win-x64-self-contained
```

Zip the contents of `dist\SteamControllerBridge-win-x64-self-contained` for a GitHub Release, or build the Inno Setup installer from `installer\SteamControllerBridge.iss`.

## Diagnostic Probe

The probe lists Valve HID interfaces and reports whether they emit controller reports:

```powershell
dotnet run --project .\SteamControllerBridge.Probe\SteamControllerBridge.Probe.csproj
```

This is useful when Windows sees the controller but the bridge cannot find the live input interface.

## Limitations

- No gyro mapping yet
- No trackpad-as-stick mapping yet
- No HidHide integration yet
- No startup option yet
- No installer yet
- Virtual output currently depends on ViGEmBus
- Steam Controller haptics are implemented as best-effort rumble and may need tuning on real hardware

## Credits

This project is informed by public community work around Steam Controller HID reports and virtual gamepad output. It does not include code from SISR or SteamlessController.
