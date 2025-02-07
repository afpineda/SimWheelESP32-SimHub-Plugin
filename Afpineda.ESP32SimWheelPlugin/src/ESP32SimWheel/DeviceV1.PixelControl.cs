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
using SimHub.Plugins;
using SimHub.Plugins.DataPlugins.RGBDriver;
using Afpineda.ESP32SimWheelPlugin;
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

            public bool SetPixel(PixelGroups group, byte pixelIndex, Color pixelData)
            {
                _report30[0] = Constants.RID_OUTPUT_PIXEL;
                _report30[1] = (byte)group;
                _report30[2] = pixelIndex;
                _report30[3] = pixelData.B;
                _report30[4] = pixelData.G;
                _report30[5] = pixelData.R;
                _report30[6] = 0;
                IsAlive = hidDevice.Write(_report30);
                return IsAlive;
            }

            public bool ShowPixelsNow()
            {
                _report30[0] = Constants.RID_OUTPUT_PIXEL;
                _report30[1] = 0xFF;
                _report30[2] = 0;
                _report30[3] = 0;
                _report30[4] = 0;
                _report30[5] = 0;
                _report30[6] = 0;
                IsAlive = hidDevice.Write(_report30);
                return IsAlive;
            }

            public bool ResetPixels()
            {
                _report30[0] = Constants.RID_OUTPUT_PIXEL;
                _report30[1] = 0xFE;
                _report30[2] = 0;
                _report30[3] = 0;
                _report30[4] = 0;
                _report30[5] = 0;
                _report30[6] = 0;
                IsAlive = hidDevice.Write(_report30);
                return IsAlive;
            }

            public void ReloadLedsDriver()
            {
                _groupUpdateCount = 0;
                if (Capabilities.GetPixelCount(PixelGroups.TelemetryLeds) > 0)
                {
                    _rgbLedsDriver[0] =
                        new RGBLedsDriver(
                            Afpineda.ESP32SimWheelPlugin.Utils.GetLedsSettingsFile(UniqueID, PixelGroups.TelemetryLeds));
                    _rgbLedsDriver[0].LedsUpdated += new EventHandler<Color[]>(OnLedsUpdatedGroup0);
                    _groupUpdateCount++;
                }
                if (Capabilities.GetPixelCount(PixelGroups.ButtonsLighting) > 0)
                {
                    _rgbLedsDriver[1] =
                        new RGBLedsDriver(
                            Afpineda.ESP32SimWheelPlugin.Utils.GetLedsSettingsFile(UniqueID, PixelGroups.ButtonsLighting));
                    _rgbLedsDriver[1].LedsUpdated += new EventHandler<Color[]>(OnLedsUpdatedGroup1);
                    _groupUpdateCount++;
                }
                if (Capabilities.GetPixelCount(PixelGroups.IndividualLeds) > 0)
                {
                    _rgbLedsDriver[2] =
                        new RGBLedsDriver(
                            Afpineda.ESP32SimWheelPlugin.Utils.GetLedsSettingsFile(UniqueID, PixelGroups.IndividualLeds));
                    _rgbLedsDriver[2].LedsUpdated += new EventHandler<Color[]>(OnLedsUpdatedGroup2);
                    _groupUpdateCount++;
                }
            }

            // --------------------------------------------------------
            // Pseudo-constructor
            // --------------------------------------------------------

            partial void InitializePixelControl()
            {
                ReloadLedsDriver();
            }

            // --------------------------------------------------------
            // Private Methods
            // --------------------------------------------------------

            private void SendPixelData(PixelGroups group, Color[] pixelData)
            {
                byte pixelCount = Capabilities.GetPixelCount(group);
                if (pixelCount > pixelData.Length)
                    pixelCount = (byte)pixelData.Length;
                for (byte index = 0; (index < pixelCount); index++)
                {
                    // SimHub.Logging.Current.InfoFormat("Led group {0}: pixel {1} : color {2}", (byte)group, index, pixelData[index]);
                    SetPixel(group, index, pixelData[index]);
                }
                // if (_groupUpdateCount > 0)
                //     _groupUpdateCount--;
                // if (_groupUpdateCount == 0)
                // {
                ShowPixelsNow();
                //     if (_rgbLedsDriver[0] != null)
                //         _groupUpdateCount++;
                //     if (_rgbLedsDriver[1] != null)
                //         _groupUpdateCount++;
                //     if (_rgbLedsDriver[2] != null)
                //         _groupUpdateCount++;
                // }
            }

            private void OnLedsUpdatedGroup0(object sender, Color[] pixelData)
            {
                if ((pixelData == null) || (sender == null))
                    return;
                SendPixelData(PixelGroups.TelemetryLeds, pixelData);
            }

            private void OnLedsUpdatedGroup1(object sender, Color[] pixelData)
            {
                if ((pixelData == null) || (sender == null))
                    return;
                SendPixelData(PixelGroups.ButtonsLighting, pixelData);
            }

            private void OnLedsUpdatedGroup2(object sender, Color[] pixelData)
            {
                if ((pixelData == null) || (sender == null))
                    return;
                SendPixelData(PixelGroups.IndividualLeds, pixelData);
            }

            // --------------------------------------------------------
            // Private data
            // --------------------------------------------------------

            private readonly RGBLedsDriver[] _rgbLedsDriver = new RGBLedsDriver[3];
            private uint _groupUpdateCount = 0;

        } // class Device
    } // namespace V1
} // namespace ESP32SimWheel
