#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-04
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using System;
using Microsoft.Win32;

namespace ESP32SimWheel
{
    static class Utils
    {
        public static string GetHidDisplayName(int vid, int pid)
        {
            try
            {
                string path = string.Format(DN_KEY_ROOT, vid, pid);
                var subKey = Registry.CurrentUser.OpenSubKey(path);
                if (subKey != null)
                {
                    string value = (string)subKey.GetValue(OEM_NAME_KEY);
                    subKey.Close();
                    return value;
                }
            }
            catch (Exception)
            {
                // Ignore
            }
            return null;
        }

        private const string DN_KEY_ROOT =
            "System\\CurrentControlSet\\Control\\MediaProperties\\PrivateProperties\\Joystick\\OEM\\VID_{0,4:X4}&PID_{1,4:X4}";
        private const string OEM_NAME_KEY = "OEMName";

    } // class Utils
} // namespace ESP32SimWheel