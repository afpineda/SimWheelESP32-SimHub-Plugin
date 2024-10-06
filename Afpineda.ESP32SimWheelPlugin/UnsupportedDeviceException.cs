#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-10-05
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

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