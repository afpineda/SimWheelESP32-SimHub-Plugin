# SimHub plugin for ESP32 open-source sim-wheels or button boxes

This project provides a [SimHub](https://www.simhubdash.com/)
plugin to send telemetry data to
[ESP32 open-source](https://github.com/afpineda/OpenSourceSimWheelESP32)
sim-wheels and button boxes.

## Installing

1. Download the latest package.
2. Unzip to the SimHub installation folder, typically:
    `C:\Program Files (x86)\SimHub\`
3. Run SimHub.
4. It should detect this plugin automatically:

   ![Plugin detection](./doc/SimHubAutodetect.png)

5. Click the right button to activate it, then click "Show in left main menu".
6. A new item will appear in the left main menu, called "ESP32 Sim-wheel".

### Upgrading to a newer version

Just repeat the unzip procedure.
There is no need to re-activate this plugin again.

## Running

- The plugin will automatically detect all connected devices in most cases.
  However, it will ignore devices that do not have telemetry display capabilities.
- If your device is not detected, you can force a refresh in two ways:
  - Pause your game, then resume.
  - Click on "ESP32 Sim-wheel" (left main menu),
    then click on "Find connected devices".
- Telemetry data will be sent to all connected and suitable devices.

### Troubleshooting

Plugin activity is shown in the `logs\simhub.txt` file relative to
your installation folder. Typically:
`C:\Program Files (x86)\SimHub\logs\simhub.txt`.

Open that file and look for the string `[ESP32 Sim-wheel]`.
