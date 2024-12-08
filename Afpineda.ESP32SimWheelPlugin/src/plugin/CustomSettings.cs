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
using SimHub.Plugins;
using ESP32SimWheel;

namespace Afpineda.ESP32SimWheelPlugin
{
    public class DeviceGameAndCarSettings
    {
        public ulong DeviceID { get; set; } = 0;
        public string Game { get; set; } = "";
        public string Car { get; set; } = "";
        public byte BitePoint { get; set; } = 0;
        public ClutchWorkingModes ClutchWorkingMode { get; set; } = ClutchWorkingModes.Clutch;
        public DPadWorkingModes DPadWorkingMode { get; set; } = DPadWorkingModes.Navigation;
        public AltButtonWorkingModes AltButtonsWorkingMode { get; set; } = AltButtonWorkingModes.ALT;
    }

    public class CustomSettings
    {
        public bool BindToGameAndCar
        {
            get { return _bindToGameAndCar; }
            set
            {
                _bindToGameAndCar = value;
                OnBindToGameAndCar(value);
            }
        }
        public DeviceGameAndCarSettings[] DeviceGameAndCarSettings
        {
            get
            {
                return _deviceGameAndCarSettings.ToArray();
            }
            set
            {
                _deviceGameAndCarSettings.Clear();
                foreach (var setting in value)
                    _deviceGameAndCarSettings.Add(setting);
            }
        }

        public bool UpdateGameAndCar(string game, string car)
        {
            if ((game != _lastGame) || (car != _lastCar))
            {
                _lastGame = game;
                _lastCar = car;
                return true;
            }
            return false;
        }

        public void ApplyTo(ESP32SimWheel.IDevice device)
        {
            if ((_lastGame.Length == 0) || (_lastCar.Length == 0) || !BindToGameAndCar)
                return;
            DeviceGameAndCarSettings settings = Find(device.UniqueID);
            if (settings != null)
            {
                if (device.Clutch != null)
                {
                    device.Clutch.ClutchWorkingMode = settings.ClutchWorkingMode;
                    device.Clutch.BitePoint = settings.BitePoint;
                }
                if (device.AltButtons != null)
                    device.AltButtons.AltButtonsWorkingMode = settings.AltButtonsWorkingMode;
                if (device.DPad != null)
                    device.DPad.DPadWorkingMode = settings.DPadWorkingMode;
                SimHub.Logging.Current.InfoFormat("[ESP32 Sim-wheel][Settings] Restored {0}/{1}/{2}",
                    device.HidInfo.DisplayName,
                    _lastGame,
                    _lastCar);
            }
            else
                SimHub.Logging.Current.InfoFormat("[ESP32 Sim-wheel] [Settings] No settings found for {0}/{1}/{2}",
                    device.HidInfo.DisplayName,
                    _lastGame,
                    _lastCar);
        }

        public void SaveFrom(ESP32SimWheel.IDevice device)
        {
            if ((_lastGame.Length == 0) || (_lastCar.Length == 0) || !BindToGameAndCar)
                return;
            if ((device.Clutch == null) && (device.AltButtons == null) && (device.DPad == null))
                return;
            DeviceGameAndCarSettings item = Find(device.UniqueID);
            if (item == null)
            {
                item = new DeviceGameAndCarSettings();
                item.DeviceID = device.UniqueID;
                item.Game = _lastGame;
                item.Car = _lastCar;
                _deviceGameAndCarSettings.Add(item);
            }
            if (device.Clutch != null)
            {
                item.ClutchWorkingMode = device.Clutch.ClutchWorkingMode;
                item.BitePoint = device.Clutch.BitePoint;
            }
            if (device.AltButtons != null)
            {
                item.AltButtonsWorkingMode = device.AltButtons.AltButtonsWorkingMode;
            }
            if (device.DPad != null)
            {
                item.DPadWorkingMode = device.DPad.DPadWorkingMode;
            }
            SimHub.Logging.Current.InfoFormat("[ESP32 Sim-wheel] [Settings] Saved {0}/{1}/{2}",
                device.HidInfo.DisplayName,
                _lastGame,
                _lastCar);
        }

        private DeviceGameAndCarSettings Find(ulong deviceID)
        {
            foreach (DeviceGameAndCarSettings item in _deviceGameAndCarSettings)
            {
                if ((item.DeviceID == deviceID) && (item.Game == _lastGame) && (item.Car == _lastCar))
                    return item;
            }
            return null;
        }

        public void RemoveClutchSettings(ulong deviceID, string game, string car)
        {
            if ((game.Length == 0) || (car.Length == 0))
                return;

            foreach (DeviceGameAndCarSettings item in _deviceGameAndCarSettings)
            {
                if ((item.DeviceID == deviceID) && (item.Game == game) && (item.Car == car))
                {
                    SimHub.Logging.Current.InfoFormat("[ESP32 Sim-wheel] [Settings] Removed {0:X},{1},{2}",
                        deviceID,
                        game,
                        car);
                    _deviceGameAndCarSettings.Remove(item);
                    return;
                }
            }
        }

        public delegate void BindToGameAndCarNotify(bool state);
        public event BindToGameAndCarNotify OnBindToGameAndCar;

        private readonly List<DeviceGameAndCarSettings> _deviceGameAndCarSettings = new List<DeviceGameAndCarSettings>();
        private string _lastGame = "";
        private string _lastCar = "";
        private bool _bindToGameAndCar = false;
    } // class CustomSettings


} // namespace Afpineda.ESP32SimWheelPlugin