#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-04
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using System;
using System.Windows.Data;
using System.Globalization;
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


        [ValueConversion(typeof(bool), typeof(bool))]
        public class InvertBooleanConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool original = (bool)value;
                return !original;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool original = (bool)value;
                return !original;
            }
        }

        [ValueConversion(typeof(bool), typeof(string))]
        public class SecurityLockToStringConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool original = (bool)value;
                if (original)
                    return "⚠ Enabled";
                else
                    return "Disabled";
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return false;
            }
        }

        [ValueConversion(typeof(ClutchWorkingModes), typeof(int))]
        public class ClutchWorkingModeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                ClutchWorkingModes original = (ClutchWorkingModes)value;
                return (int)original;
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                int original = (int)value;
                return (ClutchWorkingModes)original;
            }
        }

        [ValueConversion(typeof(DPadWorkingModes), typeof(int))]
        public class DPadWorkingModeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                DPadWorkingModes original = (DPadWorkingModes)value;
                return (int)original;
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                int original = (int)value;
                return (DPadWorkingModes)original;
            }
        }

        [ValueConversion(typeof(AltButtonWorkingModes), typeof(int))]
        public class AltButtonsWorkingModeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                AltButtonWorkingModes original = (AltButtonWorkingModes)value;
                return (int)original;
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                int original = (int)value;
                return (AltButtonWorkingModes)original;
            }
        }

        [ValueConversion(typeof(byte), typeof(System.Windows.Visibility))]
        public class PixelCountToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                byte original = (byte)value;
                if (original > 0)
                    return System.Windows.Visibility.Visible;
                else
                    return System.Windows.Visibility.Collapsed;
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return 0;
            }
        }

        private const string DN_KEY_ROOT =
            "System\\CurrentControlSet\\Control\\MediaProperties\\PrivateProperties\\Joystick\\OEM\\VID_{0,4:X4}&PID_{1,4:X4}";
        private const string OEM_NAME_KEY = "OEMName";

    } // class Utils
} // namespace ESP32SimWheel