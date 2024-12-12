using ESP32SimWheel;

namespace Afpineda.ESP32SimWheelPlugin
{
    public static class Utils
    {
        public static string GetLedsSettingsFile(ulong deviceID, PixelGroups group)
        {
            return string.Format(
                "PluginsData\\Common\\ESP32SimWheelPlugin\\{0:X16}.{1}.json",
                deviceID,
                group);
        }
    } // class Utils
} // namespace ESP32SimWheelPlugin