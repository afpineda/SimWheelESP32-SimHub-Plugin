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
    public class FakeDevice : ESP32SimWheel.IDevice, ESP32SimWheel.ITelemetryData
    {
        // --------------------------------------------------------
        // IDevice implementation
        // --------------------------------------------------------

        public Capabilities Capabilities { get { return _capabilities; } set { _capabilities = value; } }
        public HidInfo HidInfo { get { return _hidInfo; } set { _hidInfo = value; } }
        public DataVersion DataVersion { get { return _dataVersion; } set { _dataVersion = value; } }
        public IClutch Clutch { get { return null; } }
        public IAnalogClutch AnalogClutch { get { return null; } }
        public ISecurityLock SecurityLock { get { return null; } }
        public IBattery Battery { get { return null; } }
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

        // --------------------------------------------------------
        // Constructor
        // --------------------------------------------------------

        public FakeDevice(ulong UniqueID = 0, string name = "Fake device")
        {
            this._hidInfo.VendorID = 0;
            this._hidInfo.ProductID = 0;
            this._hidInfo.Path = "";
            this._hidInfo.DisplayName = name;
            this._hidInfo.Manufacturer = "";
            this._dataVersion.Major = 1;
            this._dataVersion.Minor = ESP32SimWheel.V1.Constants.SUPPORTED_MINOR_VERSION;
        }

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
        // Private fields
        // --------------------------------------------------------

        private HidInfo _hidInfo = new HidInfo();
        private DataVersion _dataVersion = new DataVersion();
        private Capabilities _capabilities = new Capabilities(0,0);
    }
} // namespace ESP32SimWheel
