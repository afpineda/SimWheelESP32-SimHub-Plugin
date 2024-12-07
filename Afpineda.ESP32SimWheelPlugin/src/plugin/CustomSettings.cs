#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-07
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using System;
using System.Collections.Generic;
// using System.Linq;
using SimHub.Plugins;
using ESP32SimWheel;

namespace Afpineda.ESP32SimWheelPlugin
{
    public class ClutchSettings
    {
        public ulong DeviceID = 0;
        public string Game { get; set; } = "";
        public string Car { get; set; } = "";
        public byte BitePoint { get; set; } = 0;
        public ClutchWorkingModes WorkingMode { get; set; } = ClutchWorkingModes.Clutch;
    }

    public class CustomSettings
    {
        public bool BindToGameAndCar { get; set; } = false;
        public ClutchSettings[] ClutchSettings
        {
            get
            {
                return _clutchSettings.ToArray();
            }
            set
            {
                _clutchSettings.Clear();
                foreach (var setting in value)
                    _clutchSettings.Add(setting);
            }
        }

        public void SaveClutchSettingsWhenNeeded(
            ulong deviceID,
            string game,
            string car,
            byte bitePoint,
            ClutchWorkingModes workingMode)
        {
            if ((game.Length == 0) || (car.Length == 0) || !BindToGameAndCar)
                return;

            foreach (ClutchSettings item in _clutchSettings)
            {
                if ((item.DeviceID == deviceID) && (item.Game == game) && (item.Car == car))
                {
                    SimHub.Logging.Current.InfoFormat("[ESP32Simwheel] [ClutchSettings] Updated {0:X},{1},{2}",
                        deviceID,
                        game,
                        car);
                    item.BitePoint = bitePoint;
                    item.WorkingMode = workingMode;
                    return;
                }
            }
            ClutchSettings newItem = new ClutchSettings();
            newItem.DeviceID = deviceID;
            newItem.Game = game;
            newItem.Car = car;
            newItem.BitePoint = bitePoint;
            newItem.WorkingMode = workingMode;
            _clutchSettings.Add(newItem);
            SimHub.Logging.Current.InfoFormat("[ESP32Simwheel] [ClutchSettings] Added {0:X},{1},{2}",
                deviceID,
                game,
                car);
        }

        public void RemoveClutchSettings(ulong deviceID, string game, string car)
        {
            if ((game.Length == 0) || (car.Length == 0))
                return;

            foreach (ClutchSettings item in _clutchSettings)
            {
                if ((item.DeviceID == deviceID) && (item.Game == game) && (item.Car == car))
                {
                    SimHub.Logging.Current.InfoFormat("[ESP32Simwheel] [ClutchSettings] Removed {0:X},{1},{2}",
                        deviceID,
                        game,
                        car);
                    _clutchSettings.Remove(item);
                    return;
                }
            }
        }

        public void LoadClutchSettings(
            ulong deviceID,
            string game,
            string car,
            out byte? bitePoint,
            out ClutchWorkingModes? workingMode)
        {
            if ((game.Length > 0) && (car.Length > 0))
                foreach (ClutchSettings item in _clutchSettings)
                {
                    if ((item.DeviceID == deviceID) && (item.Game == game) && (item.Car == car))
                    {
                        SimHub.Logging.Current.InfoFormat("[ESP32Simwheel] [ClutchSettings] Loaded {0:X},{1},{2}",
                            deviceID,
                            game,
                            car);
                        bitePoint = item.BitePoint;
                        workingMode = item.WorkingMode;
                        return;
                    }
                }
            bitePoint = null;
            workingMode = null;
        }

        private readonly List<ClutchSettings> _clutchSettings = new List<ClutchSettings>();
    } // class CustomSettings


} // namespace Afpineda.ESP32SimWheelPlugin