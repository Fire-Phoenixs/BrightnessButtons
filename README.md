# BrightnessButtons

A minimal software brightness controller for external monitors on Windows. It targets the setups where the system brightness control is missing or useless: mini PCs, desktops driving separate monitors, and multi-monitor arrangements.

## Why it was made

Windows exposes a brightness slider only when the display advertises a backlight control channel, which is normally the case for a laptop panel. On a desktop or mini PC connected to an external monitor, that channel is often absent, so the slider either does not appear or has no effect. The monitor's own brightness is then reachable only through its on-screen menu, which may be awkward to reach, shared with other settings, or unreliable.

BrightnessButtons supplies the missing control in software. It dims the image the graphics card sends out, so the perceived brightness of the screen can be adjusted from the desktop in one click, without touching the monitor.

## Features

- Two buttons: dimmer and brighter.
- 5% change per click.
- Adjustable range from 50% to 100%. The floor keeps the screen from becoming unreadable.
- Restores every active display to 100% on launch.
- Applies to all displays that Windows currently reports as active, in a single action.

## How it works

The program rewrites the gamma ramp of each display through GDI's `SetDeviceGammaRamp`.

1. `Screen.AllScreens` enumerates the active displays.
2. For each one, a device context is opened with `CreateDC("DISPLAY", deviceName, ...)`.
3. A 256-entry lookup table is built per channel: entry *i* becomes `round(i x 257 x factor)`, clamped to the 16-bit range. The red, green and blue channels receive identical values, so hue is preserved while the overall output level drops.
4. The table is uploaded with `SetDeviceGammaRamp`, and a rejected call is reported with the device name that refused it.

The ramp is a lookup applied to output code values after rendering. Because all three channels are scaled by the same factor, the effect is a linear gain on the image rather than a change to the display's gamma exponent.

A gamma ramp outlives the process that sets it: a dim level stays in effect after the window is closed, until the session ends, the display mode changes, or another program rewrites the ramp. That is why the program resets to 100% on every start. Any leftover dimming can always be undone by launching it again.

## What it does not do

- **It is not backlight control.** Monitor OSD brightness, backlight behaviour and power consumption are unchanged. The panel still emits the same amount of light; the image is darkened electronically.
- **It costs tonal range.** Lowering code values compresses the available range, so dark regions lose detail first and banding can appear at low levels. The result is lower brightness *and* lower contrast.
- **It has no per-monitor control.** One factor is applied to every active display.
- **It is not supported everywhere.** Some GPU drivers, remote desktop sessions, virtual displays, and capture or switching chains reject `SetDeviceGammaRamp`. When a display refuses the change, the program stops at that display and names it: displays handled before it keep the new level, the rest stay as they were, so desktops can end up at different brightness.

## Requirements and usage

Windows 10 or 11, x64. Run `dist/BrightnessButtons.exe` directly; there is nothing to install. The build is framework-dependent and expects the .NET 8 Desktop Runtime, so install [.NET 8 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0) first if it is not already present. The window labels are in Simplified Chinese; the interface itself is two buttons and a percentage readout.

## Build from source

Requires Windows and the .NET 8 SDK.

```powershell
dotnet publish BrightnessButtons.csproj -c Release -r win-x64 `
  --self-contained false -p:PublishSingleFile=true -p:DebugType=None -o dist
```

The output is a single executable at `dist/BrightnessButtons.exe`.

## License

MIT. See [LICENSE](LICENSE).
