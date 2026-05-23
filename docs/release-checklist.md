# Release Checklist

Use this checklist for each public release.

## Before Publishing

- Build the solution in Release mode.
- Publish the self-contained `win-x64` build.
- Build the Inno Setup installer.
- Build the portable ZIP.
- Run the installer locally.
- Confirm the app opens and the versioned files exist in `dist`.
- Update `README.md`, `CHANGELOG.md`, and `RELEASE_NOTES.md`.

## GitHub Release

- Tag: `v0.4.0`
- Title: `Steam Controller Bridge v0.4.0`
- Body: use `docs/release-v0.4.0.md`
- Assets:
  - `dist/installer/SteamControllerBridgeSetup-0.4.0.exe`
  - `dist/SteamControllerBridge-win-x64-self-contained-0.4.0.zip`

## Smoke Test

- Steam closed.
- Controller connected.
- Bridge turns on.
- Virtual Xbox controller appears.
- ABXY remap works.
- Turbo works on at least one button.
- Trackpad mouse toggles correctly.
- Gyro mouse toggles correctly.
- Rumble toggle disables rumble.
- App restores lizard mode when turned off.
