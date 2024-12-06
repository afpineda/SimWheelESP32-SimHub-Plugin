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
        ESP32SimWheel.IBattery
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
        public IClutch Clutch { get { return this; } }
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
        public IDpad DPad { get { return null; } }
        public ulong UniqueID { get; set; }

        public bool Refresh()
        {
            if (AnimateBitePoint)
            {
                _bitePoint = (byte)(_bitePoint + 1);
                if (_bitePoint == 255)
                    _bitePoint = 0;
            }
            if (AnimateClutchWorkingMode)
            {
                if (_clutchWorkingMode == ClutchWorkingModes.Button)
                    _clutchWorkingMode = ClutchWorkingModes.Clutch;
                else
                    _clutchWorkingMode = _clutchWorkingMode + 1;
            }
            if (AnimateBatteryLevel)
            {
                if (BatteryLevel <= 100)
                    BatteryLevel++;
                else
                    BatteryLevel = 0;
            }
            return AnimateBitePoint ||
                   AnimateClutchWorkingMode ||
                   AnimateBatteryLevel;
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
        // IBattery implementation
        // --------------------------------------------------------

        public void ForceBatteryCalibration()
        {
            SimHub.Logging.Current.Info("[FakeDeviceESP32] ForceBatteryCalibration()");
        }

        public byte BatteryLevel { get; set; } = 0;

        public bool AnimateBatteryLevel { get; set; } = false;

        // --------------------------------------------------------
        // ISecurityLock implementation
        // --------------------------------------------------------

        public bool IsLocked { get; set; } = false;

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
            SimHub.Logging.Current.Info("[FakeDeviceESP32] SendTelemetry()");
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
                _clutchWorkingMode = value;
                SimHub.Logging.Current.InfoFormat(
                    "[FakeDeviceESP32] Clutch working mode = {0}",
                    _clutchWorkingMode);
            }
        }
        public byte BitePoint
        {
            get { return _bitePoint; }
            set
            {
                if (_bitePoint < 255)
                {
                    _bitePoint = value;
                    SimHub.Logging.Current.InfoFormat(
                        "[FakeDeviceESP32] Bite point = {0}",
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
    }
} // namespace ESP32SimWheel
