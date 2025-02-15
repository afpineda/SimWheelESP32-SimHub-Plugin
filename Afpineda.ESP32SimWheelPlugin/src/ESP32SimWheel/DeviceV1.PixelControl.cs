#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-07
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License


using System;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices; // For GetLastError
using SimHub.Plugins;
using SimHub.Plugins.DataPlugins.RGBDriver;
using Afpineda.ESP32SimWheelPlugin;
using ESP32SimWheel;
using ESP32SimWheel.HidAPI;
using GameReaderCommon;

namespace ESP32SimWheel
{
    namespace V1
    {
        public partial class Device : ESP32SimWheel.IPixelControl

        {
            public IPixelControl Pixels => _capabilities.HasPixelControl ? this : null;

            // --------------------------------------------------------
            // IPixelControl implementation
            // --------------------------------------------------------

            public void SetPixels(PixelGroups group, Color[] pixelData)
            {
                if (pixelData != null)
                {
                    byte pixelCount = Capabilities.GetPixelCount(group);
                    if (pixelCount > pixelData.Length)
                        pixelCount = (byte)pixelData.Length;

                    for (byte index = 0; (index < pixelCount); index++)
                    {
                        _report30[0] = Constants.RID_OUTPUT_PIXEL;
                        _report30[1] = (byte)group;
                        _report30[2] = index;
                        _report30[3] = pixelData[index].B;
                        _report30[4] = pixelData[index].G;
                        _report30[5] = pixelData[index].R;
                        _report30[6] = 0;
                        _hidDevice.Write(_report30);
                    }
                }
            }

            public void ShowPixelsNow()
            {
                if (_dataVersion.Minor < 6)
                {
                    byte[] report3 = NewReport3(_dataVersion.Minor);
                    report3[4] = Constants.CMD_SHOW_PIXELS;

                    // NOTE: HUGE PERFORMACE PROBLEM HERE !!!
                    _hidDevice.SetFeature(report3);
                }
                else
                {
                    _report30[0] = Constants.RID_OUTPUT_PIXEL;
                    _report30[1] = 0xFF; // show command
                    _report30[2] = 0xFF;
                    _report30[3] = 0xFF;
                    _report30[4] = 0xFF;
                    _report30[5] = 0xFF;
                    _report30[6] = 0xFF;
                    _hidDevice.Write(_report30);
                }
            }

            public void ResetPixels()
            {
                if (_dataVersion.Minor < 6)
                {
                    byte[] report3 = NewReport3(_dataVersion.Minor);
                    report3[4] = Constants.CMD_RESET_PIXELS;
                    _hidDevice.SetFeature(report3);
                }
                else
                {
                    _report30[0] = Constants.RID_OUTPUT_PIXEL;
                    _report30[1] = 0xFE; // Reset command
                    _report30[2] = 0xFF;
                    _report30[3] = 0xFF;
                    _report30[4] = 0xFF;
                    _report30[5] = 0xFF;
                    _report30[6] = 0xFF;
                    _hidDevice.Write(_report30);
                }
            }

            public void RenderPixels(ref GameData data, PluginManager manager)
            {
                foreach (PixelGroups group in Enum.GetValues(typeof(PixelGroups)))
                    if (_rgbLedsDriver[(int)group] != null)
                    {
                        _rgbLedsDriver[(int)group].UpdateData(ref data, manager);
                        SetPixels(group, _rgbLedsDriver[(int)group].GetResult());
                    }
                ShowPixelsNow();
            }

            public RGBLedsDriver TelemetryLedsDriver
            {
                get
                {
                    if (_rgbLedsDriver[(int)PixelGroups.TelemetryLeds] != null)
                        return _rgbLedsDriver[(int)PixelGroups.TelemetryLeds];
                    return null;
                }
            }

            public RGBLedsDriver BacklightLedsDriver
            {
                get
                {
                    if (_rgbLedsDriver[(int)PixelGroups.ButtonsLighting] != null)
                        return _rgbLedsDriver[(int)PixelGroups.ButtonsLighting];
                    return null;
                }
            }

            public RGBLedsDriver IndividualLedsDriver
            {
                get
                {
                    if (_rgbLedsDriver[(int)PixelGroups.IndividualLeds] != null)
                        return _rgbLedsDriver[(int)PixelGroups.IndividualLeds];
                    return null;
                }
            }

            // --------------------------------------------------------
            // Pseudo-constructor
            // --------------------------------------------------------

            partial void InitializePixelControl()
            {
                ReloadLedsDriver();
            }

            public void ReloadLedsDriver()
            {
                foreach (PixelGroups group in Enum.GetValues(typeof(PixelGroups)))
                {
                    _rgbLedsDriver[(int)group] =
                        (Capabilities.GetPixelCount(group) > 0) ?
                            new RGBLedsDriver(
                                Afpineda.ESP32SimWheelPlugin.Utils.GetLedsSettingsFile(UniqueID, group))
                        :
                            null;
                }
            }

            // --------------------------------------------------------
            // Private data
            // --------------------------------------------------------

            private readonly RGBLedsDriver[] _rgbLedsDriver = new RGBLedsDriver[3];

        } // class Device
    } // namespace V1
} // namespace ESP32SimWheel
