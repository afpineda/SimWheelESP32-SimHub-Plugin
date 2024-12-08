#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-04
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Linq;
using System.Windows.Media;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using ESP32SimWheel;

namespace Afpineda.ESP32SimWheelPlugin
{
    [PluginDescription("Telemetry for an ESP32 open-source sim-wheel / button box")]
    [PluginAuthor("Ángel Fernández Pineda. Madrid. Spain. 2024")]
    [PluginName("ESP32 Sim-wheel")]
    public class ESP32SimWheelPlugin : IDataPlugin, IWPFSettingsV2
    {

        /// <summary>
        /// User settings
        /// </summary>
        public CustomSettings Settings { get; private set; }

        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>
        public PluginManager PluginManager { get; set; }

        /// <summary>
        /// Gets the left menu icon. Icon must be 24x24 and compatible with black and white display.
        /// </summary>
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.customicon);

        /// <summary>
        /// Gets a short plugin title to show in left menu. Return null if you want to use the title as defined in PluginName attribute.
        /// </summary>
        public string LeftMenuTitle => "ESP32 Sim-wheel";

        /// <summary>
        /// Called one time per game data update, contains all normalized game data,
        /// raw data are intentionally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        ///
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        ///
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data">Current game data, including current and previous data frame.</param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            try
            {
                if (data.GamePaused && !_gamePaused)
                    _refreshDeviceList = true;
                _gamePaused = data.GamePaused;

                MonitorBindingsEnabling();
                MonitorGameAndCar(ref data);
                UpdateDeviceListWhenNeeded();
                SendTelemetryData(ref data);
                MonitorDevices();
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.InfoFormat("[ESP32 Sim-wheel] Refreshing due to {0}", ex.ToString());
                _refreshDeviceList = true;
            }
        }

        public void UpdateDeviceListWhenNeeded()
        {
            if (_refreshDeviceList)
            {
                SimHub.Logging.Current.Info("[ESP32 Sim-wheel] Refreshing device list");
                _refreshDeviceList = false;
                // Save all device settings before refreshing,
                // since an existing device may not appear in the
                // new list
                foreach (var device in _devices)
                    Settings.SaveFrom(device);

                // Obtain a new list of available devices
                _devices = ESP32SimWheel.Devices.Enumerate().ToArray();

                int count = 0;
                foreach (var device in _devices)
                {
                    count++;
                    // Reload settings since a new
                    // device may appear
                    Settings.ApplyTo(device);
                    // Log
                    SimHub.Logging.Current.InfoFormat(
                            "[ESP32 Sim-wheel] Found: '{0}'",
                            device.HidInfo.DisplayName);
                }
                SimHub.Logging.Current.InfoFormat(
                    "[ESP32 Sim-wheel] Refresh: {0} devices found",
                    count);

                // Restart timer
                _deviceMonitorTimer.Restart();
            }
        }

        private void SendTelemetryData(ref GameData data)
        {
            if ((data.GameRunning) && (data.NewData != null))
            {
                foreach (var device in _devices)
                {
                    if ((device.TelemetryData != null) &&
                        !device.TelemetryData.SendTelemetry(ref data))
                        _refreshDeviceList = true;
                }
            }
        }

        private void MonitorBindingsEnabling()
        {
            if (_bindingsEnabledEvent)
            {
                // The user has enabled bindings
                SimHub.Logging.Current.Info("[ESP32Simwheel] Bindings enabled");
                foreach (var device in _devices)
                    Settings.ApplyTo(device);
            }
        }

        private void MonitorGameAndCar(ref GameData data)
        {
            if (data.NewData != null)
            {
                string currentCar = (data.NewData.CarId ?? "");
                string currentGame = (data.GameName ?? "");

                if (Settings.UpdateGameAndCar(currentCar, currentGame))
                {
                    // Game or car has changed
                    // Apply stored settings to each device
                    foreach (var device in _devices)
                        Settings.ApplyTo(device);

                    // Notify UI
                    _mainControl.Dispatcher.Invoke(() => _mainControl.OnGameCarChange(currentGame, currentCar));
                }
            }
        }

        private void MonitorDevices()
        {
            if (_deviceMonitorTimer.ElapsedMilliseconds > DEVICE_MONITOR_INTERVAL_MS)
            {
                try
                {
                    foreach (var device in _devices)
                    {
                        if (device.Refresh() || _bindingsEnabledEvent)
                            Settings.SaveFrom(device);
                    }
                    _bindingsEnabledEvent = false;
                }
                finally
                {
                    _deviceMonitorTimer.Restart();
                }
            }
        }


        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            _mainControl = new MainControl(this);
            return _mainControl;
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
#if DEBUG
            Devices.UseFakeDevices();
#endif
            SimHub.Logging.Current.Info("[ESP32 Sim-wheel] Init");
            _refreshDeviceList = true;
            _deviceMonitorTimer.Restart();
            // Load settings
            Settings = this.ReadCommonSettings<CustomSettings>(
                "GeneralSettings",
                () => new CustomSettings());
            Settings.OnBindToGameAndCar += OnBindToGameAndCar;
            _bindingsEnabledEvent = Settings.BindToGameAndCar;
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("[ESP32 Sim-wheel] End");
            this.SaveCommonSettings<CustomSettings>("GeneralSettings", Settings);
        }

        public void Refresh()
        {
            _refreshDeviceList = true;
            SimHub.Logging.Current.Info("[ESP32 Sim-wheel] Force device list update");
        }

        void OnBindToGameAndCar(bool state)
        {
            if (state)
                _bindingsEnabledEvent = true;
        }

        private bool _refreshDeviceList = true;

        private ESP32SimWheel.IDevice[] _devices = new ESP32SimWheel.IDevice[0];
        private MainControl _mainControl = null;
        private bool _gamePaused = false;
        private bool _bindingsEnabledEvent = false;
        private readonly Stopwatch _deviceMonitorTimer = new Stopwatch();
        private const int DEVICE_MONITOR_INTERVAL_MS = 1000;
    }
}