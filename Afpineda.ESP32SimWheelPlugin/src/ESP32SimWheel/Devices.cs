#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-04
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

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

#if DEBUG
            foreach (var fakeDevice in FakeDevices)
                yield return new FakeDeviceWrapper(fakeDevice);
#endif

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

#if DEBUG
        private static List<FakeDevice> FakeDevices { get; } = new List<FakeDevice>();

        static Devices()
        {
            // Create fake devices for testing
            FakeDevice fake;

            fake = new FakeDevice(1, "Fake telemetry device");
            fake.Capabilities = new Capabilities(0x0040, 1);
            FakeDevices.Add(fake);

            fake = new FakeDevice(2, "Fake animated clutch device");
            fake.Capabilities = new Capabilities(0x0002, 0);
            fake.AnimateBitePoint = true;
            FakeDevices.Add(fake);

            fake = new FakeDevice(3, "Fake locked device");
            fake.Capabilities = new Capabilities(0x0002, 0);
            fake.AnimateClutchWorkingMode = true;
            fake.IsLocked = true;
            FakeDevices.Add(fake);

            fake = new FakeDevice(4, "Fake battery device");
            fake.Capabilities = new Capabilities((1<<4), 0);
            fake.AnimateBatteryLevel = true;
            fake.IsLocked = true;
            FakeDevices.Add(fake);

            fake = new FakeDevice(5, "Fake static clutch device + DPad + alt");
            fake.Capabilities = new Capabilities(0x0002 | (1<<2) | (1<<3), 0);
            FakeDevices.Add(fake);
        }
#endif

    } // class Devices
} // namespace ESP32SimWheel