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
using System.Diagnostics;
using System.Linq;
using HidLibrary;
using SimHub;
using GameReaderCommon;

namespace ESP32SimWheel
{
    namespace V1
    {
        public class Device :
            ESP32SimWheel.IDevice,
            ESP32SimWheel.ITelemetryData,
            ESP32SimWheel.IClutch
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
            public ISecurityLock SecurityLock { get { return null; } }
            public IBattery Battery { get { return null; } }
            public ITelemetryData TelemetryData
            {
                get
                {
                    if (_millisecondsPerFrame > 0)
                        return this;
                    else
                        return null;
                }
            }
            public IDpad DPad { get { return null; } }
            public ulong UniqueID { get; private set; }

            public bool Tick()
            {
                byte[] newReport3;
                bool changed = false;
                hidDevice.OpenDevice();
                if (hidDevice.ReadFeatureData(out newReport3, 3))
                {
                    if (!Enumerable.SequenceEqual(newReport3, _report3))
                        changed = true;
                    _report3 = newReport3;
                }
                return changed;
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
                    hidDevice.OpenDevice();
                    hidDevice.WriteFeatureData(newReport3);
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
                        hidDevice.OpenDevice();
                        hidDevice.WriteFeatureData(newReport3);
                    }
                }
            }

            // --------------------------------------------------------
            // ITelemetryData implementation
            // --------------------------------------------------------

            public long MillisecondsPerTelemetryFrame { get { return _millisecondsPerFrame; } }

            public bool SendTelemetry(ref GameData data)
            {
                if ((_millisecondsPerFrame > 0) &&
                    (!_telemetryTimer.IsRunning || (_telemetryTimer.ElapsedMilliseconds >= _millisecondsPerFrame)))
                    try
                    {
                        _telemetryTimer.Stop();
                        hidDevice.OpenDevice();
                        if (_capabilities.UsesPowertrainTelemetry)
                        {
                            BuildPowertrainReport(ref data.NewData);
                            if (!hidDevice.Write(_powertrainReport))
                                return false;
                        }
                        if (_capabilities.UsesEcuTelemetry)
                        {
                            BuildEcuReport(ref data.NewData);
                            if (!hidDevice.Write(_ecuReport))
                                return false;
                        }
                        if (_capabilities.UsesRaceControlTelemetry)
                        {
                            BuildRaceControlReport(ref data.NewData);
                            if (!hidDevice.Write(_raceControlReport))
                                return false;
                        }
                        if (_capabilities.UsesGaugesTelemetry)
                        {
                            BuildGaugesReport(ref data.NewData);
                            if (!hidDevice.Write(_gaugesReport))
                                return false;
                        }
                        _telemetryTimer.Restart();
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                return true;
            }

            // --------------------------------------------------------
            // Constructor
            // --------------------------------------------------------

            public Device(HidLibrary.IHidDevice hidDevice)
            {
                if (hidDevice == null)
                    throw new ArgumentNullException("hidDevice");
                this.hidDevice = hidDevice;
                hidDevice.OpenDevice();

                // Read capabilities (feature) report
                byte[] capabilitiesReport;
                if (hidDevice.ReadFeatureData(out capabilitiesReport, 2) &&
                    (capabilitiesReport.Length >= Constants.REPORT2_SIZE_V1_1))
                {
                    // Check magic number and data version
                    CheckMagicNumber(capabilitiesReport);
                    _dataVersion = CheckDataVersion(capabilitiesReport);

                    // Check report sizes
                    short maxFeatureReportSize = hidDevice.Capabilities.FeatureReportByteLength;
                    short maxOutputReportSize = hidDevice.Capabilities.OutputReportByteLength;
                    CheckFeatureReportSizes(_dataVersion.Minor, maxFeatureReportSize);
                    CheckOutputReportSizes(_dataVersion.Minor, maxOutputReportSize);

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
                    _hidInfo.Manufacturer = ""; // hidDevice.ReadManufacturer() DOES NOT WORK
                    string oemDisplayName =
                        Utils.GetHidDisplayName(
                            hidDevice.Attributes.VendorId,
                            hidDevice.Attributes.ProductId);
                    if (oemDisplayName == null)
                        _hidInfo.DisplayName = string.Format("S/N:{0,16:X16}", UniqueID);
                    else
                        _hidInfo.DisplayName = oemDisplayName;

                    // Initialize ITelemetryData implementation
                    _telemetryTimer.Stop();
                    if (_capabilities.UsesTelemetryData)
                    {
                        _millisecondsPerFrame = 1000 / _capabilities.FramesPerSecond;
                        // Create buffers for output reports
                        int report20Size;
                        int report21Size;
                        int report22Size;
                        int report23Size;
                        GetTelemetryDataReportSizes(
                            _dataVersion.Minor,
                            out report20Size,
                            out report21Size,
                            out report22Size,
                            out report23Size);
                        if ((report20Size > 0) && (report21Size > 0) && (report22Size > 0) && (report23Size > 0))
                        {
                            _powertrainReport = new byte[report20Size];
                            _powertrainReport[0] = Constants.RID_OUTPUT_POWERTRAIN;
                            _ecuReport = new byte[report21Size];
                            _ecuReport[0] = Constants.RID_OUTPUT_ECU;
                            _raceControlReport = new byte[report22Size];
                            _raceControlReport[0] = Constants.RID_OUTPUT_RACE_CONTROL;
                            _gaugesReport = new byte[report23Size];
                            _gaugesReport[0] = Constants.RID_OUTPUT_GAUGES;
                        }
                        else
                            throw new UnsupportedDeviceException();
                    }
                    else
                        _millisecondsPerFrame = 0;

                    // Create report #3 (wheel configuration)
                    _report3 = NewReport3();

                    // Initialize
                    Tick();

                    // Done
                    return;
                }
                throw new UnsupportedDeviceException();
            } // constructor

            // --------------------------------------------------------
            // Private methods in help of the class constructor
            // --------------------------------------------------------

            private static void CheckFeatureReportSizes(ushort dataMinorVersion, short maxFeatureReportSize)
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
                short maxOutputReportSize)
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

            private static void GetTelemetryDataReportSizes(
                ushort dataMinorVersion,
                out int report20Size,
                out int report21Size,
                out int report22Size,
                out int report23Size)
            {
                if (dataMinorVersion >= 3)
                {
                    report20Size = Constants.REPORT20_SIZE_V1_3;
                    report21Size = Constants.REPORT21_SIZE_V1_3;
                    report22Size = Constants.REPORT22_SIZE_V1_3;
                    report23Size = Constants.REPORT23_SIZE_V1_3;
                }
                else
                {
                    report20Size = 0;
                    report21Size = 0;
                    report22Size = 0;
                    report23Size = 0;
                }
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
            // Private methods (ITelemetryData)
            // --------------------------------------------------------

            private static void ToBytes(
                ushort value,
                out byte lowByte,
                out byte highByte)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                {
                    lowByte = bytes[0];
                    highByte = bytes[1];
                }
                else
                {
                    lowByte = bytes[1];
                    highByte = bytes[0];
                }
            }

            private static void ToBytes(
                double value,
                out byte lowByte,
                out byte highByte)
            {
                try
                {
                    ushort aux = Convert.ToUInt16(value);
                    ToBytes(aux, out lowByte, out highByte);
                }
                catch (OverflowException)
                {
                    lowByte = 0;
                    highByte = 0;
                }
            }

            private static byte ToByte(bool value)
            {
                try
                {
                    return Convert.ToByte(value);
                }
                catch (OverflowException)
                {
                    return 0;
                }
            }

            private static byte ToByte(int value)
            {
                try
                {
                    return Convert.ToByte(value);
                }
                catch (OverflowException)
                {
                    return 0;
                }
            }

            private static byte ToByte(double value)
            {
                try
                {
                    return Convert.ToByte(value);
                }
                catch (OverflowException)
                {
                    return 0;
                }
            }

            private void BuildPowertrainReport(ref StatusDataBase data)
            {
                // Gear
                try
                {
                    byte auxChar = Convert.ToByte(data.Gear[0]);
                    if ((auxChar >= 32) && (auxChar < 128))
                        _powertrainReport[1] = auxChar; // ASCII space character
                    else
                        _powertrainReport[1] = 32;
                }
                catch (OverflowException)
                {
                    _powertrainReport[1] = 32;
                }

                // RPM
                ToBytes(
                    data.Rpms,
                    out _powertrainReport[2],
                    out _powertrainReport[3]);

                // RPM percentage
                _powertrainReport[4] = ToByte(data.CarSettings_CurrentDisplayedRPMPercent);

                // ShiftLight1
                _powertrainReport[5] = ToByte(data.CarSettings_RPMShiftLight1 * 100);

                // ShiftLight2
                _powertrainReport[6] = ToByte(data.CarSettings_RPMShiftLight2 * 100);

                // Rev limiter
                _powertrainReport[7] = ToByte(data.CarSettings_RPMRedLineReached > 0.0);

                // Engine started
                _powertrainReport[8] = ToByte(data.EngineStarted);

                // Speed
                ToBytes(
                    data.SpeedLocal,
                    out _powertrainReport[9],
                    out _powertrainReport[10]);
            }

            private void BuildEcuReport(ref StatusDataBase data)
            {
                // ABS engaged
                _ecuReport[1] = ToByte(data.ABSActive != 0);

                // TC engaged
                _ecuReport[2] = ToByte(data.TCActive != 0);

                // DRS enabled
                _ecuReport[3] = ToByte(data.DRSEnabled != 0);

                // Pit limiter
                _ecuReport[4] = ToByte(data.PitLimiterOn != 0);

                // Low fuel alert
                _ecuReport[5] = ToByte(data.CarSettings_FuelAlertActive != 0);

                // ECU levels
                _ecuReport[6] = ToByte(data.ABSLevel); // ABS
                _ecuReport[7] = Convert.ToByte(data.TCLevel); // TC1
                _ecuReport[8] = 0; // TC2
                _ecuReport[9] = Convert.ToByte(data.BrakeBias); // Brake bias
            }

            private void BuildRaceControlReport(ref StatusDataBase data)
            {
                _raceControlReport[1] = ToByte(data.Flag_Black);
                _raceControlReport[2] = ToByte(data.Flag_Blue);
                _raceControlReport[3] = ToByte(data.Flag_Checkered);
                _raceControlReport[4] = ToByte(data.Flag_Green);
                _raceControlReport[5] = ToByte(data.Flag_Orange);
                _raceControlReport[6] = ToByte(data.Flag_White);
                _raceControlReport[7] = ToByte(data.Flag_Yellow);
                ToBytes(
                    data.RemainingLaps,
                    out _raceControlReport[8],
                    out _raceControlReport[9]);
                ToBytes(
                    data.SessionTimeLeft.Minutes,
                    out _raceControlReport[10],
                    out _raceControlReport[11]);
            }

            private void BuildGaugesReport(ref StatusDataBase data)
            {
                // Relative turbo pressure
                _gaugesReport[1] = ToByte(data.TurboPercent);

                // Absolute turbo pressure
                ToBytes(
                    data.Turbo * 100,
                    out _gaugesReport[2],
                    out _gaugesReport[3]);

                // Water temperature
                ToBytes(
                    data.WaterTemperature,
                    out _gaugesReport[4],
                    out _gaugesReport[5]);

                /// Oil pressure
                ToBytes(
                    data.OilPressure * 100,
                    out _gaugesReport[6],
                    out _gaugesReport[7]);

                // Oil temperature
                ToBytes(
                    data.OilTemperature,
                    out _gaugesReport[8],
                    out _gaugesReport[9]);

                // Relative fuel
                _gaugesReport[10] = ToByte(data.FuelPercent);

                // Absolute fuel
                ToBytes(
                    data.Fuel,
                    out _gaugesReport[11],
                    out _gaugesReport[12]);
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

            // --------------------------------------------------------
            // Private fields (IDevice)
            // --------------------------------------------------------

            internal readonly HidLibrary.IHidDevice hidDevice;
            private readonly Capabilities _capabilities;
            private readonly HidInfo _hidInfo = new HidInfo();
            private readonly DataVersion _dataVersion;

            // --------------------------------------------------------
            // Private fields (ITelemetryData)
            // --------------------------------------------------------

            private readonly Stopwatch _telemetryTimer = new Stopwatch();
            private readonly int _millisecondsPerFrame = 0;
            private readonly byte[] _powertrainReport;
            private readonly byte[] _ecuReport;
            private readonly byte[] _raceControlReport;
            private readonly byte[] _gaugesReport;


        } // class Device
    } // namespace V1
} // namespace ESP32SimWheel
