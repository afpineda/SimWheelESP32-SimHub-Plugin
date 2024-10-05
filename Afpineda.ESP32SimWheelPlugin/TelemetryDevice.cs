using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using HidSharp;
using GameReaderCommon;

namespace Afpineda.ESP32SimWheelPlugin
{
    internal sealed class TelemetryDevice
    {
        // --------------------------------------------------------
        // Constructor / destructor
        // --------------------------------------------------------

        public TelemetryDevice(HidDevice hidDevice)
        {
            if (hidDevice == null)
                throw new ArgumentNullException("HID device is null");
            _hidDevice = hidDevice;

            // Check max report size
            int maxFeatureReportSize = hidDevice.GetMaxFeatureReportLength();
            int maxOutputReportSize = hidDevice.GetMaxOutputReportLength();
            HidStream stream;
            if ((maxFeatureReportSize >= REPORT2_SIZE_V1_0) &&
                hidDevice.TryOpen(out stream))
            {
                byte[] capabilitiesReport = GetCapabilitiesReport(stream, maxFeatureReportSize);

                // Check magic number and data version compatibility
                CheckMagicNumber(capabilitiesReport);
                DataMajorVersion = BitConverter.ToUInt16(capabilitiesReport, 3);
                DataMinorVersion = BitConverter.ToUInt16(capabilitiesReport, 5);
                if ((DataMajorVersion != 1) || (DataMinorVersion < 3))
                    throw new UnsupportedDeviceException();

                // Retrieve capabilities
                ushort capabilities = BitConverter.ToUInt16(capabilitiesReport, 7);
                _usesPowertrainTelemetry = (capabilities & CAP_TELEMETRY_POWERTRAIN) != 0;
                _usesEcuTelemetry = (capabilities & CAP_TELEMETRY_ECU) != 0;
                _usesRaceControlTelemetry = (capabilities & CAP_TELEMETRY_RACE_CONTROL) != 0;
                _usesGaugesTelemetry = (capabilities & CAP_TELEMETRY_GAUGES) != 0;

                // Ignore devices not using telemetry data
                if (!_usesPowertrainTelemetry &&
                    !_usesEcuTelemetry &&
                    !_usesRaceControlTelemetry &&
                    !_usesGaugesTelemetry)
                    throw new UnsupportedDeviceException();

                // Check compatibility of report sizes
                int report20Size;
                int report21Size;
                int report22Size;
                int report23Size;
                CheckOutputReportSizes(
                    maxOutputReportSize,
                    DataMinorVersion,
                    out report20Size,
                    out report21Size,
                    out report22Size,
                    out report23Size);

                // Retrieve max FPS
                if (capabilitiesReport[17] > 0)
                    _millisecondsPerFrame = 1000 / capabilitiesReport[17];
                else
                    _millisecondsPerFrame = 0;

                // Create buffers for output reports
                _powertrainReport = new byte[report20Size];
                _powertrainReport[0] = RID_OUTPUT_POWERTRAIN;
                _ecuReport = new byte[report21Size];
                _ecuReport[0] = RID_OUTPUT_ECU;
                _raceControlReport = new byte[report22Size];
                _raceControlReport[0] = RID_OUTPUT_RACE_CONTROL;
                _gaugesReport = new byte[report23Size];
                _gaugesReport[0] = RID_OUTPUT_GAUGES;

                // Initialize
                _telemetryTimer.Stop();
                return;
            }
            throw new UnsupportedDeviceException();
        }

        // --------------------------------------------------------
        // HID attributes
        // --------------------------------------------------------

        public string DevicePath
        {
            get { return _hidDevice.DevicePath; }
        }

        public int ProductID
        {
            get { return _hidDevice.ProductID; }
        }
        public int VendorID
        {
            get { return _hidDevice.VendorID; }
        }

        // --------------------------------------------------------
        // Capabilities
        // --------------------------------------------------------

        public ushort DataMajorVersion { get; private set; }
        public ushort DataMinorVersion { get; private set; }

        // --------------------------------------------------------
        // Telemetry
        // --------------------------------------------------------

        public long MillisecondsPerTelemetryFrame { get { return _millisecondsPerFrame; } }

        public bool SendTelemetry(ref GameData data)
        {
            if ((_millisecondsPerFrame > 0) &&
                (!_telemetryTimer.IsRunning || (_telemetryTimer.ElapsedMilliseconds >= _millisecondsPerFrame)))
                try
                {
                    _telemetryTimer.Stop();
                    HidStream stream = _hidDevice.Open();
                    if (_usesPowertrainTelemetry)
                    {
                        BuildPowertrainReport(ref data.NewData);
                        stream.Write(_powertrainReport);
                    }
                    if (_usesEcuTelemetry)
                    {
                        BuildEcuReport(ref data.NewData);
                        stream.Write(_ecuReport);
                    }
                    if (_usesRaceControlTelemetry)
                    {
                        BuildRaceControlReport(ref data.NewData);
                        stream.Write(_raceControlReport);
                    }
                    if (_usesGaugesTelemetry)
                    {
                        BuildGaugesReport(ref data.NewData);
                        stream.Write(_gaugesReport);
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
        // Device listing
        // --------------------------------------------------------

        public static IEnumerable<TelemetryDevice> GetAll()
        {
            var hidDeviceList = HidSharp.DeviceList.Local.GetHidDevices();
            TelemetryDevice candidate;
            foreach (HidDevice hidDevice in hidDeviceList)
            {
                try
                {
                    candidate = new TelemetryDevice(hidDevice);
                }
                catch (Exception)
                {
                    continue;
                }
                yield return candidate;
            }
        }

        // --------------------------------------------------------
        // Private methods in help of the class constructor
        // --------------------------------------------------------

        private static void CheckOutputReportSizes(
            int maxFeatureReportSize,
            ushort dataMinorVersion,
            out int report20Size,
            out int report21Size,
            out int report22Size,
            out int report23Size)
        {
            if (dataMinorVersion >= 3)
            {
                report20Size = REPORT20_SIZE_V1_3;
                report21Size = REPORT21_SIZE_V1_3;
                report22Size = REPORT22_SIZE_V1_3;
                report23Size = REPORT23_SIZE_V1_3;
            }
            else
            {
                report20Size = 0;
                report21Size = 0;
                report22Size = 0;
                report23Size = 0;

            }
            if ((report20Size > maxFeatureReportSize) ||
                (report21Size > maxFeatureReportSize) ||
                (report22Size > maxFeatureReportSize) ||
                (report23Size > maxFeatureReportSize)
            )
                throw new UnsupportedDeviceException();
        }

        private static byte[] GetCapabilitiesReport(
            HidStream stream,
            int maxFeatureReportSize)
        {
            try
            {
                byte[] featureReport = new byte[maxFeatureReportSize];
                featureReport[0] = RID_FEATURE_CAPABILITIES;
                stream.GetFeature(featureReport);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(featureReport, 1, 2);
                    Array.Reverse(featureReport, 3, 2);
                    Array.Reverse(featureReport, 5, 2);
                    Array.Reverse(featureReport, 7, 2);
                    Array.Reverse(featureReport, 9, 8);
                }
                return featureReport;
            }
            catch (IOException)
            {
                throw new UnsupportedDeviceException();
            }
        }

        private static void CheckMagicNumber(byte[] featureReport)
        {
            if (featureReport[1] != MAGIC_NUMBER_LOW || featureReport[2] != MAGIC_NUMBER_HIGH)
                throw new UnsupportedDeviceException();
        }

        // --------------------------------------------------------
        // Private methods
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
        // Private fields
        // --------------------------------------------------------

        internal readonly HidDevice _hidDevice;
        internal readonly bool _usesPowertrainTelemetry;
        internal readonly bool _usesEcuTelemetry;
        internal readonly bool _usesRaceControlTelemetry;
        internal readonly bool _usesGaugesTelemetry;
        private readonly Stopwatch _telemetryTimer = new Stopwatch();
        private readonly int _millisecondsPerFrame = 0;
        private readonly byte[] _powertrainReport;
        private readonly byte[] _ecuReport;
        private readonly byte[] _raceControlReport;
        private readonly byte[] _gaugesReport;

        // --------------------------------------------------------
        // Private constants
        // --------------------------------------------------------

        // Report ID
        private const byte RID_FEATURE_CAPABILITIES = 2;
        private const byte RID_OUTPUT_POWERTRAIN = 20;
        private const byte RID_OUTPUT_ECU = 21;
        private const byte RID_OUTPUT_RACE_CONTROL = 22;
        private const byte RID_OUTPUT_GAUGES = 23;

        // Magic number
        private const byte MAGIC_NUMBER_LOW = 0x51;
        private const byte MAGIC_NUMBER_HIGH = 0xBF;

        // Report sizes
        // Note: must increase data size in 1 to make room for the report-ID field
        private const int REPORT2_SIZE_V1_0 = 8 + 1;
        private const int REPORT20_SIZE_V1_3 = 10 + 1;
        private const int REPORT21_SIZE_V1_3 = 9 + 1;
        private const int REPORT22_SIZE_V1_3 = 11 + 1;
        private const int REPORT23_SIZE_V1_3 = 12 + 1;

        // Capability flags
        private const ushort CAP_TELEMETRY_POWERTRAIN = 1 << 6;
        private const ushort CAP_TELEMETRY_ECU = 1 << 7;
        private const ushort CAP_TELEMETRY_RACE_CONTROL = 1 << 8;
        private const ushort CAP_TELEMETRY_GAUGES = 1 << 9;
    }
}