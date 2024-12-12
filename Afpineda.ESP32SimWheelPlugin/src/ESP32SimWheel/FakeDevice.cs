#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-05
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using System.Text;
using System.Drawing;
using SimHub.Plugins;
using GameReaderCommon;

namespace ESP32SimWheel
{
    public delegate void FakeDeviceUpdateNotify();

    public class FakeDevice :
        ESP32SimWheel.ITelemetryData,
        ESP32SimWheel.IClutch,
        ESP32SimWheel.ISecurityLock,
        ESP32SimWheel.IBattery,
        ESP32SimWheel.IDpad,
        ESP32SimWheel.IAltButtons,
        ESP32SimWheel.IPixelControl
    {
        // --------------------------------------------------------
        // Device simulation
        // --------------------------------------------------------

        public bool AnimateBitePoint { get; set; } = false;
        public bool AnimateClutchWorkingMode { get; set; } = false;

        public event FakeDeviceUpdateNotify OnFakeDeviceUpdate;

        public void Refresh()
        {
            bool updated = false;
            if (AnimateBitePoint)
            {
                _bitePoint = (byte)(_bitePoint + 1);
                if (_bitePoint == 255)
                    _bitePoint = 0;
                updated = true;
            }
            if (AnimateClutchWorkingMode)
            {
                if (_clutchWorkingMode == ClutchWorkingModes.Button)
                    _clutchWorkingMode = ClutchWorkingModes.Clutch;
                else
                    _clutchWorkingMode = _clutchWorkingMode + 1;
                updated = true;
            }
            if (AnimateBatteryLevel)
            {
                if (_batteryLevel <= 100)
                    _batteryLevel++;
                else
                    _batteryLevel = 0;
                updated = true;
            }
            if (updated)
                OnFakeDeviceUpdate?.Invoke();
        }

        // --------------------------------------------------------
        // IDevice implementation
        // --------------------------------------------------------

        public Capabilities Capabilities { get { return _capabilities; } set { _capabilities = value; } }
        public HidInfo HidInfo { get { return _hidInfo; } set { _hidInfo = value; } }
        public DataVersion DataVersion { get { return _dataVersion; } set { _dataVersion = value; } }
        public IClutch Clutch => (_capabilities.HasClutch) ? this : null;
        public ISecurityLock SecurityLock => this;
        public IBattery Battery => _capabilities.HasBattery ? this : null;
        public ITelemetryData TelemetryData => _capabilities.UsesTelemetryData ? this : null;
        public IDpad DPad => _capabilities.HasDPad ? this : null;
        public IAltButtons AltButtons => _capabilities.HasAltButtons ? this : null;
        public IPixelControl Pixels => _capabilities.HasPixelControl ? this : null;

        public ulong UniqueID { get; set; }

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
                if (_dPadWorkingMode != value)
                    OnFakeDeviceUpdate?.Invoke();
                _dPadWorkingMode = value;
                SimHub.Logging.Current.InfoFormat(
                    "[FakeDeviceESP32] [{0}] DPAD working mode = {1}",
                    _hidInfo.DisplayName,
                    _dPadWorkingMode);
            }
        }

        // --------------------------------------------------------
        // IAltButtons implementation
        // --------------------------------------------------------

        public AltButtonWorkingModes AltButtonsWorkingMode
        {
            get { return _altButtonsWorkingMode; }
            set
            {
                if (_altButtonsWorkingMode != value)
                    OnFakeDeviceUpdate?.Invoke();
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
                if (_batteryLevel != value)
                    OnFakeDeviceUpdate?.Invoke();
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
                if (_isLocked != value)
                    OnFakeDeviceUpdate?.Invoke();
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
                if (_clutchWorkingMode != value)
                    OnFakeDeviceUpdate?.Invoke();
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
                    if (_bitePoint != value)
                        OnFakeDeviceUpdate?.Invoke();
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
        // IPixelControl implementation
        // --------------------------------------------------------

        public void SetPixels(PixelGroups group, Color[] pixelData)
        {
            if ((pixelData != null) && (pixelData.Length > 0))
            {
                var auxStr = new StringBuilder();
                foreach (var pixel in pixelData)
                {
                    //auxStr.Append(string.Format("{0:X} ", pixel.ToArgb()));
                    auxStr.Append(((pixel.ToArgb() & 0x00FFFFFF) != 0) ? "o" : ".");
                }
                SimHub.Logging.Current.InfoFormat(
                    "[FakeDeviceESP32] [{0}] setPixels({1}): {2}",
                    _hidInfo.DisplayName,
                    group,
                    auxStr.ToString());
            }
        }

        public void ShowPixelsNow()
        {
            // SimHub.Logging.Current.InfoFormat("[FakeDeviceESP32] [{0}] ShowPixelsNow()",
            //     _hidInfo.DisplayName);
        }

        // --------------------------------------------------------
        // Private fields
        // --------------------------------------------------------

        private HidInfo _hidInfo = new HidInfo();
        private DataVersion _dataVersion = new DataVersion();
        private Capabilities _capabilities = new Capabilities(0, 0);
        private DPadWorkingModes _dPadWorkingMode = DPadWorkingModes.Navigation;
    } // class FakeDevice

    public class FakeDeviceWrapper : ESP32SimWheel.IDevice
    {
        // --------------------------------------------------------
        // IDevice implementation
        // --------------------------------------------------------

        public Capabilities Capabilities => _device.Capabilities;
        public HidInfo HidInfo => _device.HidInfo;
        public DataVersion DataVersion => _device.DataVersion;
        public IClutch Clutch => _device.Clutch;
        public IAnalogClutch AnalogClutch => null;
        public ISecurityLock SecurityLock => _device;
        public IBattery Battery => _device.Battery;
        public ITelemetryData TelemetryData => _device.TelemetryData;
        public IDpad DPad => _device.DPad;
        public IAltButtons AltButtons => _device.AltButtons;
        public IPixelControl Pixels => _device.Pixels;
        public ulong UniqueID { get { return _device.UniqueID; } set { _device.UniqueID = value; } }

        public bool Refresh()
        {
            _device.Refresh();
            return _updated;
        }

        // --------------------------------------------------------
        // Constructor
        // --------------------------------------------------------

        public FakeDeviceWrapper(FakeDevice device)
        {
            _device = device;
            _device.OnFakeDeviceUpdate += (() => _updated = true);
        }

        // --------------------------------------------------------
        // Private fields
        // --------------------------------------------------------

        private readonly FakeDevice _device;
        private bool _updated = false;
    } // class FakeDeviceWrapper

} // namespace ESP32SimWheel
