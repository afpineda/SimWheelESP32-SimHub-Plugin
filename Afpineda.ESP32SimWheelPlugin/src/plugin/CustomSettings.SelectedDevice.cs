#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-07
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SimHub.Plugins;
using ESP32SimWheel;
using SimHub.Plugins.DataPlugins.RGBDriver;
using SimHub.Plugins.DataPlugins.RGBDriver.Settings;

namespace Afpineda.ESP32SimWheelPlugin
{
    public partial class CustomSettings : INotifyPropertyChanged
    {
        [JsonIgnore]
        public ESP32SimWheel.IDevice SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                _selectedDevice = value;
                if (_selectedDevice != null)
                    SimHub.Logging.Current.InfoFormat(
                        "[ESP32 Sim-wheel] [UI] Device selected: '{0}'",
                        _selectedDevice.HidInfo.DisplayName);
                else
                    SimHub.Logging.Current.Info("[ESP32 Sim-wheel] [UI] No device selected");
                NotifyPropertyChanged();
                RefreshSelectedDeviceState(true);
            }
        }

        [JsonIgnore]
        public LedsSettings TelemetryLedsSettings
        {
            get
            {
                return SelectedDevice?.Pixels?.TelemetryLedsDriver.Settings ?? null;
            }
        }

        [JsonIgnore]
        public LedsSettings BackLightLedsSettings
        {
            get
            {
                return SelectedDevice?.Pixels?.BacklightLedsDriver.Settings ?? null;
            }
        }

        [JsonIgnore]
        public LedsSettings IndividualLedsSettings
        {
            get
            {
                return SelectedDevice?.Pixels?.IndividualLedsDriver.Settings ?? null;
            }
        }

        [JsonIgnore]
        public bool IsUIVisible { get; internal set; } = false;

        public void RefreshSelectedDeviceState(bool forced = false)
        {
            forced = forced || (IsUIVisible && (_selectedDevice?.Refresh() ?? false));
            if (forced)
            {
                NotifyPropertyChanged(nameof(SelectedDevice));
                NotifyPropertyChanged(nameof(TelemetryLedsSettings));
                NotifyPropertyChanged(nameof(BackLightLedsSettings));
                NotifyPropertyChanged(nameof(IndividualLedsSettings));
                // SimHub.Logging.Current.InfoFormat("[ESP32 Sim-wheel] [UI] [DEBUG] Tick '{0}'", _selectedDevice.HidInfo.DisplayName);
            }
        }


        private ESP32SimWheel.IDevice _selectedDevice = null;

    }// class CustomSettings
}// namespace Afpineda.ESP32SimWheelPlugin