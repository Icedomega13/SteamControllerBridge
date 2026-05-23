# Contributing

Thanks for taking a look at Steam Controller Bridge.

This project is intentionally small. The best contributions keep the default app simple and reliable.

## Local Setup

1. Install the .NET 9 SDK.
2. Install ViGEmBus.
3. Close Steam while testing direct HID mode.
4. Build the solution:

```powershell
dotnet build .\SteamControllerBridge.sln
```

## Useful Areas

- Better reconnect handling
- Trackpad mouse mode
- Gyro-to-stick or gyro-to-mouse mapping
- HidHide integration
- A maintained virtual controller backend
- Installer packaging
- Test tooling for HID report translation

## Design Principle

The main UI should stay boring and obvious: one switch, clear status, minimal settings. Advanced behavior should be hidden until it is needed.
