#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-04
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using System;
using System.Text;
using System.Linq;
using System.IO;
using HidLibrary;
using SimHub;

namespace ESP32SimWheel
{
    namespace V1
    {
        public partial class Device :
            ESP32SimWheel.IDevice,
            ESP32SimWheel.IClutch,
            ESP32SimWheel.ISecurityLock,
            ESP32SimWheel.IBattery,
            ESP32SimWheel.IDpad,
            ESP32SimWheel.IAltButtons
        {
            // --------------------------------------------------------
            // String representation
            // --------------------------------------------------------

            public override string ToString()
            {
                return HidInfo.DisplayName;
            }

            // --------------------------------------------------------
            // IDevice implementation
            // --------------------------------------------------------

            public Capabilities Capabilities { get { return _capabilities; } }
            public HidInfo HidInfo { get { return _hidInfo; } }
            public DataVersion DataVersion { get { return _dataVersion; } }
            public IClutch Clutch
            {
                get
                {
                    if (_capabilities.HasClutch)
                        return this;
                    else
                        return null;
                }
            }
            public IAnalogClutch AnalogClutch { get { return null; } }
            public ISecurityLock SecurityLock { get { return this; } }

            public IBattery Battery
            {
                get
                {
                    if (_capabilities.HasBattery)
                        return this;
                    else
                        return null;
                }
            }

            public IDpad DPad
            {
                get
                {
                    if (_capabilities.HasDPad)
                        return this;
                    else return null;
                }
            }

            public IAltButtons AltButtons
            {
                get
                {
                    if (_capabilities.HasAltButtons)
                        return this;
                    else return null;
                }
            }

            public ulong UniqueID { get; private set; }

            public bool Refresh()
            {
                bool changed = false;
                byte[] newReport3;

                if (hidDevice.ReadFeatureData(out newReport3, Constants.RID_FEATURE_CONFIG))
                {
                    if (!Enumerable.SequenceEqual<byte>(newReport3, _report3))
                        changed = true;
                    _report3 = newReport3;
                    return changed;
                }
                ThrowIOException();
                return false;
            }
            // --------------------------------------------------------
            // IDPAD implementation
            // --------------------------------------------------------

            public DPadWorkingModes DPadWorkingMode
            {
                get
                {
                    if ((_report3.Length < 6) || (_report3[5] != 0))
                        return DPadWorkingModes.Navigation;
                    return DPadWorkingModes.Button;
                }
                set
                {
                    if (_report3.Length > 5)
                    {
                        byte[] newReport3 = NewReport3();
                        newReport3[5] = (byte)value;
                        if (!hidDevice.WriteFeatureData(newReport3))
                            ThrowIOException();
                    }
                }
            }

            // --------------------------------------------------------
            // IAltButtons implementation
            // --------------------------------------------------------

            public AltButtonWorkingModes AltButtonsWorkingMode
            {
                get
                {
                    if (_report3[2] == 0)
                        return AltButtonWorkingModes.Button;
                    else
                        return AltButtonWorkingModes.ALT;
                }
                set
                {
                    byte[] newReport3 = NewReport3();
                    newReport3[2] = (byte)value;
                    if (!hidDevice.WriteFeatureData(newReport3))
                        ThrowIOException();
                }
            }

            // --------------------------------------------------------
            // IBattery implementation
            // --------------------------------------------------------

            public void ForceBatteryCalibration()
            {
                throw new NotImplementedException();
            }

            public byte BatteryLevel
            {
                get
                {
                    if (_capabilities.HasBattery)
                        return _report3[4];
                    else
                        return 0;
                }
            }

            // --------------------------------------------------------
            // ISecurityLock implementation
            // --------------------------------------------------------

            public bool IsLocked
            {
                get
                {
                    if (_report3.Length > 6)
                        return (_report3[6] != 0);
                    return false;
                }
            }

            // --------------------------------------------------------
            // IClutch implementation
            // --------------------------------------------------------

            public ClutchWorkingModes ClutchWorkingMode
            {
                get { return (ClutchWorkingModes)_report3[1]; }
                set
                {
                    byte[] newReport3 = NewReport3();
                    newReport3[1] = (byte)value;
                    if (!hidDevice.WriteFeatureData(newReport3))
                        ThrowIOException();
                }
            }

            public byte BitePoint
            {
                get { return _report3[3]; }
                set
                {
                    if (value < 255)
                    {
                        byte[] newReport3 = NewReport3();
                        newReport3[3] = value;
                        if (!hidDevice.WriteFeatureData(newReport3))
                            ThrowIOException();
                    }
                }
            }

            // --------------------------------------------------------
            // Constructor
            // --------------------------------------------------------

            public Device(HidDevice hidDevice)
            {
                if (hidDevice == null)
                    throw new ArgumentNullException("hidDevice");
                this.hidDevice = hidDevice;
                hidDevice.OpenDevice();

                // Read capabilities (feature) report
                byte[] capabilitiesReport = GetCapabilitiesReport();

                // SimHub.Logging.Current.Info("[ESP32SimWheel] Candidate found");
                // Check magic number and data version
                CheckMagicNumber(capabilitiesReport);
                _dataVersion = CheckDataVersion(capabilitiesReport);
                // SimHub.Logging.Current.Info("[ESP32SimWheel] Magic number and data version ok");

                // Check report sizes
                int maxFeatureReportSize = hidDevice.Capabilities.FeatureReportByteLength;
                int maxOutputReportSize = hidDevice.Capabilities.OutputReportByteLength;
                CheckFeatureReportSizes(_dataVersion.Minor, maxFeatureReportSize);
                CheckOutputReportSizes(_dataVersion.Minor, maxOutputReportSize);
                // SimHub.Logging.Current.Info("[ESP32SimWheel] Report sizes ok");

                // Retrieve unique ID
                UniqueID = (_dataVersion.Minor >= 1) ?
                    BitConverter.ToUInt64(capabilitiesReport, 9)
                    :
                    0;

                // Retrieve max FPS
                byte fps = (capabilitiesReport.Length >= Constants.REPORT2_SIZE_V1_3) && (_dataVersion.Minor >= 3) ?
                    capabilitiesReport[17]
                    :
                    (byte)0;

                // Create capabilities struct
                ushort flags = BitConverter.ToUInt16(capabilitiesReport, 7);
                this._capabilities = new Capabilities(flags, fps);

                // Populate HidInfo
                _hidInfo.Path = hidDevice.DevicePath;
                _hidInfo.VendorID = hidDevice.Attributes.VendorId;
                _hidInfo.ProductID = hidDevice.Attributes.ProductId;
                _hidInfo.Manufacturer = ""; // Note: ReadManufacturer() does not work
                string oemDisplayName = Utils.GetHidDisplayName(
                    _hidInfo.VendorID,
                    _hidInfo.ProductID);
                if (oemDisplayName == null)
                    _hidInfo.DisplayName = string.Format("S/N:{0,16:X16}", UniqueID);
                else
                    _hidInfo.DisplayName = oemDisplayName;

                // Initialize ITelemetryData implementation
                InitializeTelemetryData();

                // Create report #3 (wheel configuration)
                _report3 = NewReport3();

                // Initialize
                Refresh();
                // SimHub.Logging.Current.Info("[ESP32SimWheel] Device found");
            } // constructor

            partial void InitializeTelemetryData();

            // --------------------------------------------------------
            // Private methods in help of the class constructor
            // --------------------------------------------------------

            private byte[] GetCapabilitiesReport()
            {
                byte[] featureReport;
                if (hidDevice.ReadFeatureData(out featureReport, Constants.RID_FEATURE_CAPABILITIES))
                {
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(featureReport, 1, 2);
                        Array.Reverse(featureReport, 3, 2);
                        Array.Reverse(featureReport, 5, 2);
                        Array.Reverse(featureReport, 7, 2);
                        if (featureReport.Length >= Constants.REPORT3_SIZE_V1_1)
                            Array.Reverse(featureReport, 9, 8);
                    }
                    return featureReport;
                }
                ThrowIOException();
                return null;
            }

            private static void CheckFeatureReportSizes(ushort dataMinorVersion, int maxFeatureReportSize)
            {
                if ((dataMinorVersion == 0) && (maxFeatureReportSize >= Constants.REPORT3_SIZE_V1_0))
                    return;
                if ((dataMinorVersion == 1) && (maxFeatureReportSize >= Constants.REPORT3_SIZE_V1_1))
                    return;
                else if (maxFeatureReportSize >= Constants.REPORT3_SIZE_V1_2)
                    return;
                throw new UnsupportedDeviceException();
            }

            private static void CheckOutputReportSizes(
                ushort dataMinorVersion,
                int maxOutputReportSize)
            {
                if ((dataMinorVersion >= 3) &&
                        (maxOutputReportSize >= Constants.REPORT21_SIZE_V1_3) &&
                        (maxOutputReportSize >= Constants.REPORT20_SIZE_V1_3) &&
                        (maxOutputReportSize >= Constants.REPORT22_SIZE_V1_3) &&
                        (maxOutputReportSize >= Constants.REPORT23_SIZE_V1_3))
                    return;
                if (maxOutputReportSize == 0)
                    return;
                throw new UnsupportedDeviceException();
            }

            private static DataVersion CheckDataVersion(byte[] capabilitiesReport)
            {
                DataVersion dataVersion = new DataVersion();
                dataVersion.Major = BitConverter.ToUInt16(capabilitiesReport, 3);
                dataVersion.Minor = BitConverter.ToUInt16(capabilitiesReport, 5);
                if ((dataVersion.Major != 1) ||
                    (dataVersion.Minor > Constants.SUPPORTED_MINOR_VERSION) ||
                    (dataVersion.Minor == 0))
                    throw new UnsupportedDeviceException();
                return dataVersion;
            }

            private static void CheckMagicNumber(byte[] featureReport)
            {
                if ((featureReport[1] != MagicNumber.MAGIC_NUMBER_LOW) ||
                    (featureReport[2] != MagicNumber.MAGIC_NUMBER_HIGH))
                    throw new UnsupportedDeviceException();
            }

            private static short GetReport3Size(ushort dataMinorVersion)
            {
                if (dataMinorVersion >= 2)
                {
                    return Constants.REPORT3_SIZE_V1_2;
                }
                else if (dataMinorVersion == 1)
                    return Constants.REPORT3_SIZE_V1_1;
                else
                    return Constants.REPORT3_SIZE_V1_0;
            }

            // --------------------------------------------------------
            // Private fields and methods
            // --------------------------------------------------------

            private byte[] _report3;

            private byte[] NewReport3()
            {
                byte[] report = Enumerable.Repeat<byte>(0xFF, GetReport3Size(_dataVersion.Minor)).ToArray();
                report[0] = 3;
                return report;
            }

            private static void ThrowIOException()
            {
                throw new IOException("Hid device not available");
            }

            // --------------------------------------------------------
            // Private fields (IDevice)
            // --------------------------------------------------------

            internal readonly HidDevice hidDevice;
            private readonly Capabilities _capabilities;
            private readonly HidInfo _hidInfo = new HidInfo();
            private readonly DataVersion _dataVersion;
        } // class Device
    } // namespace V1
} // namespace ESP32SimWheel
