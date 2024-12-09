#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-05
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using SimHub.Plugins;
using GameReaderCommon;

namespace ESP32SimWheel
{
    public class FakeDevice :
        ESP32SimWheel.IDevice,
        ESP32SimWheel.ITelemetryData,
        ESP32SimWheel.IClutch,
        ESP32SimWheel.ISecurityLock,
        ESP32SimWheel.IBattery,
        ESP32SimWheel.IDpad,
        ESP32SimWheel.IAltButtons
    {
        // --------------------------------------------------------
        // Device simulation
        // --------------------------------------------------------

        public bool AnimateBitePoint { get; set; } = false;
        public bool AnimateClutchWorkingMode { get; set; } = false;

        // --------------------------------------------------------
        // IDevice implementation
        // --------------------------------------------------------

        public Capabilities Capabilities { get { return _capabilities; } set { _capabilities = value; } }
        public HidInfo HidInfo { get { return _hidInfo; } set { _hidInfo = value; } }
        public DataVersion DataVersion { get { return _dataVersion; } set { _dataVersion = value; } }
        public IClutch Clutch { get { if (_capabilities.HasClutch) return this; else return null; } }
        public IAnalogClutch AnalogClutch { get { return null; } }
        public ISecurityLock SecurityLock { get { return this; } }
        public IBattery Battery { get { if (_capabilities.HasBattery) return this; else return null; } }
        public ITelemetryData TelemetryData
        {
            get
            {
                if (Capabilities.UsesTelemetryData)
                    return this;
                else
                    return null;
            }
        }
        public IDpad DPad { get { if (_capabilities.HasDPad) return this; else return null; } }
        public IAltButtons AltButtons { get { if (_capabilities.HasAltButtons) return this; else return null; } }

        public ulong UniqueID { get; set; }

        public bool Refresh()
        {
            if (AnimateBitePoint)
            {
                _bitePoint = (byte)(_bitePoint + 1);
                if (_bitePoint == 255)
                    _bitePoint = 0;
                _updated = true;

            }
            if (AnimateClutchWorkingMode)
            {
                if (_clutchWorkingMode == ClutchWorkingModes.Button)
                    _clutchWorkingMode = ClutchWorkingModes.Clutch;
                else
                    _clutchWorkingMode = _clutchWorkingMode + 1;
                _updated = true;
            }
            if (AnimateBatteryLevel)
            {
                if (_batteryLevel <= 100)
                    _batteryLevel++;
                else
                    _batteryLevel = 0;
                _updated = true;
            }
            bool result = _updated;
            _updated = false;
            return result;
        }

        // --------------------------------------------------------
        // Constructor
        // --------------------------------------------------------

        public FakeDevice(ulong UniqueID = 0, string name = "Fake device")
        {
            this.UniqueID = UniqueID;
            this._hidInfo.VendorID = 0;
            this._hidInfo.ProductID = 0;
            this._hidInfo.Path = "";
            this._hidInfo.DisplayName = name;
            this._hidInfo.Manufacturer = "";
            this._dataVersion.Major = 1;
            this._dataVersion.Minor = ESP32SimWheel.V1.Constants.SUPPORTED_MINOR_VERSION;
        }

        // --------------------------------------------------------
        // IDPAD implementation
        // --------------------------------------------------------

        public DPadWorkingModes DPadWorkingMode
        {
            get { return _dPadWorkingMode; }
            set
            {
                _updated = (_dPadWorkingMode != value);
                _dPadWorkingMode = value;
                SimHub.Logging.Current.InfoFormat(
                    "[FakeDeviceESP32] [{0}] DPAD working mode = {1}",
                    _hidInfo.DisplayName,
                    _dPadWorkingMode);
            }
        }
        private DPadWorkingModes _dPadWorkingMode = DPadWorkingModes.Navigation;

        // --------------------------------------------------------
        // IAltButtons implementation
        // --------------------------------------------------------

        public AltButtonWorkingModes AltButtonsWorkingMode
        {
            get { return _altButtonsWorkingMode; }
            set
            {
                _updated = (_altButtonsWorkingMode != value);
                _altButtonsWorkingMode = value;
                SimHub.Logging.Current.InfoFormat(
                    "[FakeDeviceESP32] [{0}] ALT buttons working mode = {1}",
                    _hidInfo.DisplayName,
                    _altButtonsWorkingMode);
            }
        }
        private AltButtonWorkingModes _altButtonsWorkingMode = AltButtonWorkingModes.ALT;

        // --------------------------------------------------------
        // IBattery implementation
        // --------------------------------------------------------

        public void ForceBatteryCalibration()
        {
            SimHub.Logging.Current.InfoFormat(
                "[FakeDeviceESP32] [{0}] ForceBatteryCalibration()",
                _hidInfo.DisplayName);
        }

        public byte BatteryLevel
        {
            get
            {
                return _batteryLevel;
            }
            set
            {
                _updated = (_batteryLevel != value);
                _batteryLevel = value;
            }
        }

        public bool AnimateBatteryLevel { get; set; } = false;
        private byte _batteryLevel = 0;

        // --------------------------------------------------------
        // ISecurityLock implementation
        // --------------------------------------------------------

        public bool IsLocked
        {
            get { return _isLocked; }
            set
            {
                _updated = (_isLocked != value);
                _isLocked = value;
            }
        }

        private bool _isLocked = false;

        // --------------------------------------------------------
        // ITelemetryData implementation
        // --------------------------------------------------------

        public long MillisecondsPerTelemetryFrame
        {
            get
            {
                if (this.Capabilities.FramesPerSecond > 0)
                    return 0;
                else
                    return 1000 / this.Capabilities.FramesPerSecond;
            }
        }

        public bool SendTelemetry(ref GameData data)
        {
            SimHub.Logging.Current.InfoFormat(
                "[FakeDeviceESP32] [{0}] SendTelemetry()",
                _hidInfo.DisplayName);
            return true;
        }

        // --------------------------------------------------------
        // IClutch implementation
        // --------------------------------------------------------

        public ClutchWorkingModes ClutchWorkingMode
        {
            get { return _clutchWorkingMode; }
            set
            {
                _updated = (_clutchWorkingMode != value);
                _clutchWorkingMode = value;
                SimHub.Logging.Current.InfoFormat(
                    "[FakeDeviceESP32] [{0}] Clutch working mode = {1}",
                    _hidInfo.DisplayName,
                    _clutchWorkingMode);
            }
        }
        public byte BitePoint
        {
            get { return _bitePoint; }
            set
            {
                if (value < 255)
                {
                    _updated = (_bitePoint != value);
                    _bitePoint = value;
                    SimHub.Logging.Current.InfoFormat(
                        "[FakeDeviceESP32] [{0}] Bite point = {1}",
                        _hidInfo.DisplayName,
                        _bitePoint);
                }
            }
        }

        private ClutchWorkingModes _clutchWorkingMode = ClutchWorkingModes.Clutch;
        private byte _bitePoint = 0;

        // --------------------------------------------------------
        // Private fields
        // --------------------------------------------------------

        private HidInfo _hidInfo = new HidInfo();
        private DataVersion _dataVersion = new DataVersion();
        private Capabilities _capabilities = new Capabilities(0, 0);
        private bool _updated = true;
    }
} // namespace ESP32SimWheel
