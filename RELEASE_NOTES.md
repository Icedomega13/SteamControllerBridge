# Steam Controller Bridge v0.1.0

Initial public MVP for testing the 2026 Steam Controller outside Steam.

## Highlights

- Presents the Steam Controller as a virtual Xbox 360 controller for XInput games.
- Supports wired and Steam Controller Puck HID interfaces.
- Handles known state report IDs `0x42` and `0x45`.
- Disables lizard mode while enabled and restores it on shutdown.
- Includes paddle mapping for L4, L5, R4, and R5.
- Includes optional trackpad-as-mouse mode.
- Includes optional rumble passthrough and a rumble toggle.
- Includes dark mode and diagnostic logging.

## Requirements

- Windows 10 or newer.
- ViGEmBus installed.
- Steam closed while using this MVP.

## Known Limitations

- ViGEmBus is retired and is used here as a practical MVP backend.
- Gyro mapping is not implemented yet.
- HidHide duplicate-device handling is not implemented yet.
- Rumble is best-effort and may need more hardware tuning.
