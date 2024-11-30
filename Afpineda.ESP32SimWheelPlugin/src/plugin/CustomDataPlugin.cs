using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;
using System.Collections;
using System.Collections.Generic;

namespace Afpineda.ESP32SimWheelPlugin
{
    [PluginDescription("Telemetry for an ESP32 open-source sim-wheel / button box")]
    [PluginAuthor("Ángel Fernández Pineda. Madrid. Spain. 2024")]
    [PluginName("ESP32 Sim-wheel")]
    public class CustomDataPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
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
            if ((data.GameRunning) && (data.NewData != null))
            {
                if (_refreshDeviceList)
                {
                    _refreshDeviceList = false;
                    _devices = TelemetryDevice.GetAll();
                    int count = 0;
                    foreach (var device in _devices)
                    {
                        count++;
                        SimHub.Logging.Current.Info(
                            string.Format(
                                "[ESP32 Sim-wheel] Found: VID/PID {0}/{1} (v{2}.{3})",
                                device.VendorID,
                                device.ProductID,
                                device.DataMajorVersion,
                                device.DataMinorVersion));
                    }
                    SimHub.Logging.Current.Info(
                        string.Format(
                            "[ESP32 Sim-wheel] Refresh: {0} devices found",
                            count));
                }

                foreach (var device in _devices)
                {
                    if (!device.SendTelemetry(ref data))
                        _refreshDeviceList = true;
                }
            }
            else
                _refreshDeviceList = true;
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {

        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new CustomSettingsControl(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("[ESP32 Sim-wheel] Init");
            _refreshDeviceList = true;
        }

        public void Refresh()
        {
            _refreshDeviceList = true;
            SimHub.Logging.Current.Info("[ESP32 Sim-wheel] Force device list update");
        }

        private bool _refreshDeviceList = true;
        private IEnumerable<TelemetryDevice> _devices;

    }
}