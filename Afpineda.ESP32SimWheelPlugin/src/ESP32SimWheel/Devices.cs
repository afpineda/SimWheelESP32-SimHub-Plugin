using System;
using System.Collections.Generic;
using System.Linq;
using HidLibrary;

namespace ESP32SimWheel
{
    static class Devices
    {
        public static IEnumerable<ESP32SimWheel.IDevice> Enumerate()
        {
            foreach (var hid in HidLibrary.HidDevices.Enumerate())
            {
                ESP32SimWheel.IDevice AsSimWheel = null;
                try
                {
                    AsSimWheel = new ESP32SimWheel.V1.Device(hid);
                }
                catch (Exception)
                {
                    // ignore
                }
                if (AsSimWheel != null)
                    yield return AsSimWheel;
            }
        }

        public static IEnumerable<ESP32SimWheel.IDevice> EnumerateTelemetryDataCapable()
        {
            return
                from device in Enumerate()
                where device.Capabilities.UsesTelemetryData
                select device;
        }
    } // class Devices
} // namespace ESP32SimWheel