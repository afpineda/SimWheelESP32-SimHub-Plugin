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

namespace Afpineda.ESP32SimWheelPlugin
{

    //------------------------------------------------------------
    // DeviceGameAndCarSettings
    //------------------------------------------------------------

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

    //------------------------------------------------------------
    // Main class
    //------------------------------------------------------------

    public class CustomSettings : INotifyPropertyChanged
    {
        //------------------------------------------------------------
        // INotifyPropertyChanged implementation
        //------------------------------------------------------------

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //------------------------------------------------------------
        // Public properties
        //------------------------------------------------------------

        public bool BindToGameAndCar
        {
            get { return _bindToGameAndCar; }
            set
            {
                if (value != _bindToGameAndCar)
                {
                    _bindToGameAndCar = value;
                    OnBindToGameAndCar?.Invoke(value);
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(this.IsBindingAvailable));
                }
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
                NotifyPropertyChanged();
            }
        }

        [JsonIgnore]
        public string CurrentGameAndCar
        {
            get
            {
                if ((_lastGame.Length == 0) || (_lastCar.Length == 0))
                    return "(none)";
                else
                    return string.Format(
                        "{0} / {1}",
                        _lastGame,
                        _lastCar);
            }
        }

        [JsonIgnore]
        public bool GameAndCarAvailable
        {
            get
            {
                return ((_lastGame.Length > 0) && (_lastCar.Length > 0));
            }
        }

        [JsonIgnore]
        public bool IsBindingAvailable
        {
            get
            {
                return BindToGameAndCar && GameAndCarAvailable;
            }
        }

        //------------------------------------------------------------
        // Public methods
        //------------------------------------------------------------

        public bool UpdateGameAndCar(string game, string car)
        {
            if ((game != _lastGame) || (car != _lastCar))
            {
                _lastGame = game;
                _lastCar = car;
                NotifyPropertyChanged(nameof(this.CurrentGameAndCar));
                NotifyPropertyChanged(nameof(this.IsBindingAvailable));
                return true;
            }
            return false;
        }

        public void ApplyTo(ESP32SimWheel.IDevice device)
        {
            if ((_lastGame.Length == 0) || (_lastCar.Length == 0) || !BindToGameAndCar)
                return;
            if ((device.Clutch == null) && (device.AltButtons == null) && (device.DPad == null))
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
                device.Refresh();
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
            device.Refresh();
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

        //------------------------------------------------------------
        // CLR bindings
        //------------------------------------------------------------

        public delegate void BindToGameAndCarNotify(bool state);
        public event BindToGameAndCarNotify OnBindToGameAndCar;

        //------------------------------------------------------------
        // Private fields and properties
        //------------------------------------------------------------

        private readonly List<DeviceGameAndCarSettings> _deviceGameAndCarSettings = new List<DeviceGameAndCarSettings>();
        private string _lastGame = "";
        private string _lastCar = "";
        private bool _bindToGameAndCar = false;
    } // class CustomSettings

} // namespace Afpineda.ESP32SimWheelPlugin