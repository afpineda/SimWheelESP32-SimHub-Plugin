using System;

namespace Afpineda.ESP32SimWheelPlugin
{
    public class UnsupportedDeviceException : Exception
    {
        public UnsupportedDeviceException() : base("")
        {
        }

        public UnsupportedDeviceException(string message)
            : base(message)
        {
        }

        public UnsupportedDeviceException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}