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
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            _mainControl = new MainControl(this);
            return _mainControl;
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
            try
            {
                if (data.GamePaused && !_gamePaused)
                    _refreshDeviceList = true;
                _gamePaused = data.GamePaused;

                MonitorBindingsEnabling();
                MonitorGameAndCar(ref data);
                UpdateDeviceListWhenNeeded();
                SendTelemetryData(ref data);
                SendPixelData();
                MonitorSaveRequest();
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

                // Obtain a new list of available devices
                ESP32SimWheel.IDevice[] newDevices =
                    ESP32SimWheel.Devices.Enumerate().ToArray();

                int count = 0;
                foreach (var device in newDevices)
                {
                    count++;
                    SimHub.Logging.Current.InfoFormat(
                            "[ESP32 Sim-wheel] Found: '{0}'",
                            device.HidInfo.DisplayName);
                    if (IsNewDevice(device.UniqueID))
                        Settings.ApplyTo(device);
                }
                SimHub.Logging.Current.InfoFormat(
                    "[ESP32 Sim-wheel] Refresh: {0} devices found",
                    count);
                _devices = newDevices;
            }
        }

        private void SendTelemetryData(ref GameData data)
        {
            if ((data.GameRunning) && (data.NewData != null))
                foreach (var device in _devices)
                    if ((device.TelemetryData != null) &&
                        !device.TelemetryData.SendTelemetry(ref data))
                        _refreshDeviceList = true;
        }

        private void SendPixelData()
        {
            System.Drawing.Color[] pixels = null;
            foreach (var device in _devices)
                if (device.Pixels != null)
                    try
                    {
                        pixels = LocateRGBLedsDriver(
                            device.UniqueID,
                            PixelGroups.TelemetryLeds)
                            ?.GetResult();
                        device.Pixels.SetPixels(PixelGroups.TelemetryLeds, pixels);

                        pixels = LocateRGBLedsDriver(
                            device.UniqueID,
                            PixelGroups.ButtonsLighting)
                            ?.GetResult();
                        device.Pixels.SetPixels(PixelGroups.ButtonsLighting, pixels);

                        pixels = LocateRGBLedsDriver(
                            device.UniqueID,
                            PixelGroups.IndividualLeds)
                            ?.GetResult();
                        device.Pixels.SetPixels(PixelGroups.IndividualLeds, pixels);

                        device.Pixels.ShowPixelsNow();
                    }
                    catch (IOException)
                    {
                        _refreshDeviceList = true;
                    }
        }

        private void MonitorBindingsEnabling()
        {
            if (_bindingsEnabledEvent)
            {
                // The user has enabled bindings
                SimHub.Logging.Current.Info("[ESP32 Sim-wheel] Bindings enabled");
                ApplySettingsToAllDevices();
                _bindingsEnabledEvent = false;
            }
        }

        private void MonitorGameAndCar(ref GameData data)
        {
            if (data.NewData != null)
            {
                string currentCar = (data.NewData.CarId ?? "");
                string currentGame = (data.GameName ?? "");

                if (Settings.UpdateGameAndCar(currentGame, currentCar))
                {
                    // Game or car has changed
                    ApplySettingsToAllDevices();
                    // Notify UI
                    // _mainControl.Dispatcher.Invoke(() => _mainControl.OnGameCarChange(currentGame, currentCar));
                }
            }
        }

        private void MonitorSaveRequest()
        {
            if (_saveRequest)
            {
                SaveSettingsFromAllDevices();
                _saveRequest = false;
            }
        }

        // --------------------------------------------------------
        // Plugin initialization and finalization
        // --------------------------------------------------------

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            // Initialize fields
            _ledDrivers[(int)PixelGroups.TelemetryLeds] = new Dictionary<ulong, RGBLedsDriver>();
            _ledDrivers[(int)PixelGroups.ButtonsLighting] = new Dictionary<ulong, RGBLedsDriver>();
            _ledDrivers[(int)PixelGroups.IndividualLeds] = new Dictionary<ulong, RGBLedsDriver>();

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

            // Configure events
            Settings.OnBindToGameAndCar += OnBindToGameAndCar;
            _bindingsEnabledEvent = Settings.BindToGameAndCar;

            // Refresh device list
            _refreshDeviceList = true;
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("[ESP32 Sim-wheel] End");
            ResetAllPixels();
            this.SaveCommonSettings<CustomSettings>("GeneralSettings", Settings);
        }

        // --------------------------------------------------------
        // Public methods
        // (called from the user interface control)
        // --------------------------------------------------------

        public void Refresh()
        {
            _refreshDeviceList = true;
            SimHub.Logging.Current.Info("[ESP32 Sim-wheel] Force device list update");
        }

        public void Save()
        {
            _saveRequest = true;
            SimHub.Logging.Current.Info("[ESP32 Sim-wheel] Save requested");
        }

        public void ReloadRGBLedDrivers(ulong deviceID)
        {
            SimHub.Logging.Current.InfoFormat(
                "[ESP32 Sim-wheel] Request to reload RGB Leds drivers for device {0:X16}",
                deviceID);
            lock (_reloadLedsDriversLock)
            {
                _ledDrivers[(int)PixelGroups.TelemetryLeds].Remove(deviceID);
                _ledDrivers[(int)PixelGroups.ButtonsLighting].Remove(deviceID);
                _ledDrivers[(int)PixelGroups.IndividualLeds].Remove(deviceID);
            }
        }

        // --------------------------------------------------------
        // Auxiliary methods
        // --------------------------------------------------------

        private void OnBindToGameAndCar(bool state)
        {
            if (state)
                _bindingsEnabledEvent = true;
        }

        private bool IsNewDevice(ulong deviceID)
        {
            foreach (var device in _devices)
                if (device.UniqueID == deviceID)
                    return false;
            return true;
        }

        private void ApplySettingsToAllDevices()
        {
            foreach (var device in _devices)
                try
                {
                    Settings.ApplyTo(device);
                }
                catch
                {
                    _refreshDeviceList = true;
                }
        }

        private void SaveSettingsFromAllDevices()
        {
            foreach (var device in _devices)
                try
                {
                    Settings.SaveFrom(device);
                }
                catch
                {
                    _refreshDeviceList = true;
                }
            this.SaveCommonSettings<CustomSettings>("GeneralSettings", Settings);
        }

        private RGBLedsDriver LocateRGBLedsDriver(
            ulong deviceID,
            PixelGroups group)
        {

            Dictionary<ulong, RGBLedsDriver> dict = _ledDrivers[(int)group];
            RGBLedsDriver driver = null;
            lock (_reloadLedsDriversLock)
            {
                if (!dict.TryGetValue(deviceID, out driver) || (driver == null))
                {
                    driver = new RGBLedsDriver(
                        Utils.GetLedsSettingsFile(deviceID, group));
                    dict.Add(deviceID, driver);
                    SimHub.Logging.Current.InfoFormat(
                        "[ESP32 Sim-wheel] RGB Leds driver loaded for device {0:X16} and group {1}",
                            deviceID,
                            group);
                }
            }
            return driver;
        }

        private void ResetAllPixels()
        {
            foreach (var device in _devices)
                try
                {
                    device.Pixels?.ResetPixels();
                }
                catch
                {
                    // Do nothing since we are quitting
                }
        }

        // --------------------------------------------------------
        // Private Fields and properties
        // --------------------------------------------------------

        private bool _refreshDeviceList = true;

        private ESP32SimWheel.IDevice[] _devices = new ESP32SimWheel.IDevice[0];
        private MainControl _mainControl = null;
        private bool _gamePaused = false;
        private bool _bindingsEnabledEvent = false;
        private bool _saveRequest = false;
        private readonly Dictionary<ulong, RGBLedsDriver>[] _ledDrivers = new Dictionary<ulong, RGBLedsDriver>[3];
        private const int DEVICE_MONITOR_INTERVAL_MS = 1000;
        private readonly object _reloadLedsDriversLock = new object();
    }
}