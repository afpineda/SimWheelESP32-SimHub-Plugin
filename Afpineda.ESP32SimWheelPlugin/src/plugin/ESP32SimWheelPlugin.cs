#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-04
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using SimHub.Plugins;
using SimHub.Plugins.DataPlugins.RGBDriver;
using SimHub.Plugins.DataPlugins.RGBDriver.Settings;
using GameReaderCommon;

using ESP32SimWheel;

namespace Afpineda.ESP32SimWheelPlugin
{
    [PluginDescription("Telemetry for an ESP32 open-source sim-wheel / button box")]
    [PluginAuthor("Ángel Fernández Pineda. Madrid. Spain. 2024")]
    [PluginName("ESP32 Sim-wheel")]
    public class ESP32SimWheelPlugin : IDataPlugin, IWPFSettingsV2
    {

        public CustomSettings Settings { get; private set; }
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.customicon);
        public string LeftMenuTitle => "ESP32 Sim-wheel";
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new MainControl(this);
            // return _mainControl;
        }

        // --------------------------------------------------------
        // Main LOOP
        // --------------------------------------------------------

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
            // TimeSpan lastFrameTime = DateTime.UtcNow - _lastFrameTimestamp;

            if (data.GamePaused && !_gamePaused)
                _refreshDeviceList = true;
            _gamePaused = data.GamePaused;

            RefreshAvailableDevicesWhenNeeded();
            MonitorGameAndCar(ref data);
            if (_skippedFrames >= Settings.FrameSkip)
            {
                _skippedFrames = 0;
                foreach (var device in Settings.Devices)
                {
                    device.Pixels?.RenderPixels(ref data, pluginManager);
                    device.TelemetryData?.SendTelemetry(ref data);
                }
            }
            else
                _skippedFrames++;
            RefreshSelectedDeviceWhenNeeded();
            MonitorSaveRequest();
            // _lastFrameTimestamp = DateTime.UtcNow;
        }

        // --------------------------------------------------------

        public void RefreshAvailableDevicesWhenNeeded()
        {
            bool forceRefresh = _refreshDeviceList;
            _refreshDeviceList = false;
            if (!forceRefresh)
                foreach (var device in Settings.Devices)
                    if (!device.IsOpen)
                    {
                        SimHub.Logging.Current.InfoFormat(
                           "[ESP32 Sim-wheel] Disconnected: '{0}'",
                           device.HidInfo.DisplayName);
                        forceRefresh = true;
                    }
            if (forceRefresh)
            {
                SimHub.Logging.Current.Info("[ESP32 Sim-wheel] Refreshing device list");
                ObservableCollection<ESP32SimWheel.IDevice> newDeviceList = new ObservableCollection<ESP32SimWheel.IDevice>();
                foreach (var device in ESP32SimWheel.Devices.Enumerate())
                    newDeviceList.Add(device);
                Settings.Devices = newDeviceList;
            }
        }

        private void RefreshSelectedDeviceWhenNeeded()
        {
            TimeSpan elapsed = DateTime.UtcNow - _lastTick;
            if (elapsed.TotalMilliseconds > TICK_RATE_MS)
            {
                _lastTick = DateTime.UtcNow;
                Settings.RefreshSelectedDeviceState();
            }
        }

        private void MonitorGameAndCar(ref GameData data)
        {
            if (data.NewData != null)
            {
                string currentCar = (data.NewData.CarId ?? "");
                string currentGame = (data.GameName ?? "");
                Settings.UpdateGameAndCar(currentGame, currentCar);
            }
        }

        private void MonitorSaveRequest()
        {
            if (_saveRequest)
            {
                _saveRequest = false;
                Settings.SaveSettingsFromAllDevices();
                this.SaveCommonSettings<CustomSettings>("GeneralSettings", Settings);
            }
        }

        // --------------------------------------------------------
        // Plugin initialization and finalization
        // --------------------------------------------------------

        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.InfoFormat(
                "[ESP32 Sim-wheel] Plugin version: {0}",
                PLUGIN_VERSION);
            LoadSettings();
            // _lastFrameTimestamp = DateTime.UtcNow;
            _refreshDeviceList = true;
        }

        public void End(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("[ESP32 Sim-wheel] End");
            this.SaveCommonSettings<CustomSettings>("GeneralSettings", Settings);
            ResetAllPixels();
        }

        // --------------------------------------------------------
        // Public methods
        // (called from the user interface control)
        // --------------------------------------------------------

        public void Refresh()
        {
            _refreshDeviceList = true;
            SimHub.Logging.Current.Info("[ESP32 Sim-wheel] [UI] Refresh device list");
        }

        public void Save()
        {
            _saveRequest = true;
            SimHub.Logging.Current.Info("[ESP32 Sim-wheel] [UI] Save requested");
        }

        // --------------------------------------------------------
        // Auxiliary methods
        // --------------------------------------------------------

        private void LoadSettings()
        {
            // Load settings
            Settings = this.ReadCommonSettings<CustomSettings>(
                "GeneralSettings",
                () => new CustomSettings());

            SimHub.Logging.Current.InfoFormat(
                           "[ESP32 Sim-wheel] Init: {0} device/game/car settings",
                            Settings.DeviceGameAndCarSettings.Length);
            foreach (var gameCarSettings in Settings.DeviceGameAndCarSettings)
                SimHub.Logging.Current.InfoFormat(
                    "[ESP32 Sim-wheel] Settings loaded for {0}/{1}/{2}",
                    gameCarSettings.DeviceID,
                    gameCarSettings.Game,
                    gameCarSettings.Car);
        }

        private void ResetAllPixels()
        {
            foreach (var device in Settings.Devices)
                device.Pixels?.ResetPixels();
        }

        // --------------------------------------------------------
        // Private Fields and properties
        // --------------------------------------------------------



        private bool _refreshDeviceList = true;
        private bool _saveRequest = false;
        private DateTime _lastTick = DateTime.MinValue;
        // private DateTime _lastFrameTimestamp = DateTime.MinValue;
        private bool _gamePaused = false;
        private uint _skippedFrames = 0;
        private const double TICK_RATE_MS = 500; // 2 FPS for UI

        // --------------------------------------------------------
       // --------------------------------------------------------
       private const string PLUGIN_VERSION = "2.5.0";
       // --------------------------------------------------------
       // --------------------------------------------------------
    }
}