#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-07
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using System;
using System.Diagnostics;
using HidLibrary;
using SimHub;
using GameReaderCommon;
using Microsoft.VisualBasic;

namespace ESP32SimWheel
{
    namespace V1
    {
        public partial class Device : ESP32SimWheel.ITelemetryData
        {
            public ITelemetryData TelemetryData => (_millisecondsPerFrame > 0) ? this : null;

            // --------------------------------------------------------
            // ITelemetryData implementation
            // --------------------------------------------------------

            public long MillisecondsPerTelemetryFrame => _millisecondsPerFrame;

            public void SendTelemetry(ref GameData data)
            {
                if ((_millisecondsPerFrame == 0) || (data == null) || (data.NewData == null))
                    return;

                if (!_telemetryTimer.IsRunning || (_telemetryTimer.ElapsedMilliseconds >= _millisecondsPerFrame))
                {
                    _telemetryTimer.Stop();
                    if (_capabilities.UsesPowertrainTelemetry)
                    {
                        BuildPowertrainReport(ref data.NewData);
                        _hidDevice.Write(_powertrainReport);
                    }
                    if (_capabilities.UsesEcuTelemetry)
                    {
                        BuildEcuReport(ref data.NewData);
                        _hidDevice.Write(_ecuReport);
                    }
                    if (_capabilities.UsesRaceControlTelemetry)
                    {
                        BuildRaceControlReport(ref data.NewData);
                        _hidDevice.Write(_raceControlReport);
                    }
                    if (_capabilities.UsesGaugesTelemetry)
                    {
                        BuildGaugesReport(ref data.NewData);
                        _hidDevice.Write(_gaugesReport);
                    }
                    _telemetryTimer.Restart();
                }
                if (_capabilities.UsesWheelsTelemetry)
                {
                    if (!_wheelsTelemetryTimer.IsRunning || (_wheelsTelemetryTimer.ElapsedMilliseconds >= 5000))
                    {
                        _wheelsTelemetryTimer.Stop();
                        BuildWheelsReport(ref data.NewData);
                        _hidDevice.Write(_wheelsReport);
                        _wheelsTelemetryTimer.Restart();
                    }
                }
            }

            // --------------------------------------------------------
            // Pseudo-constructor
            // --------------------------------------------------------

            partial void InitializeTelemetryData()
            {
                // Initialize ITelemetryData implementation
                _telemetryTimer.Stop();
                _wheelsTelemetryTimer.Stop();
                if (_capabilities.UsesTelemetryData)
                {
                    _millisecondsPerFrame = 1000 / _capabilities.FramesPerSecond;
                    // Create buffers for output reports
                    int report20Size;
                    int report21Size;
                    int report22Size;
                    int report23Size;
                    int report24Size;
                    GetTelemetryDataReportSizes(
                        _dataVersion.Minor,
                        out report20Size,
                        out report21Size,
                        out report22Size,
                        out report23Size,
                        out report24Size);
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

                    if ((_capabilities.UsesWheelsTelemetry) && (report24Size == 0))
                        throw new UnsupportedDeviceException();
                    _wheelsReport = new byte[report24Size];
                    if (report24Size > 0)
                        _gaugesReport[0] = Constants.RID_OUTPUT_WHEELS;
                }
                else
                    _millisecondsPerFrame = 0;
            }

            private static void GetTelemetryDataReportSizes(
                ushort dataMinorVersion,
                out int report20Size,
                out int report21Size,
                out int report22Size,
                out int report23Size,
                out int report24Size)
            {
                if (dataMinorVersion >= 3)
                {
                    report20Size = Constants.REPORT20_SIZE_V1_3;
                    report21Size = Constants.REPORT21_SIZE_V1_3;
                    report22Size = Constants.REPORT22_SIZE_V1_3;
                    report23Size = Constants.REPORT23_SIZE_V1_3;
                    if (dataMinorVersion >= 7)
                        report24Size = Constants.REPORT24_SIZE_V1_7;
                    else
                        report24Size = 0;
                }
                else
                {
                    report20Size = 0;
                    report21Size = 0;
                    report22Size = 0;
                    report23Size = 0;
                    report24Size = 0;
                }
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

            private void BuildWheelsReport(ref StatusDataBase data)
            {
                // Tire temperature
                ToBytes(
                    data.TyreTemperatureFrontLeft,
                    out _wheelsReport[1],
                    out _wheelsReport[2]);
                ToBytes(
                    data.TyreTemperatureFrontRight,
                    out _wheelsReport[3],
                    out _wheelsReport[4]);
                ToBytes(
                    data.TyreTemperatureRearLeft,
                    out _wheelsReport[5],
                    out _wheelsReport[6]);
                ToBytes(
                    data.TyreTemperatureRearRight,
                    out _wheelsReport[7],
                    out _wheelsReport[8]);

                // Tire pressure
                ToBytes(
                    data.TyrePressureFrontLeft * 100,
                    out _wheelsReport[9],
                    out _wheelsReport[10]);
                ToBytes(
                    data.TyrePressureFrontRight * 100,
                    out _wheelsReport[11],
                    out _wheelsReport[12]);
                ToBytes(
                    data.TyrePressureRearLeft * 100,
                    out _wheelsReport[13],
                    out _wheelsReport[14]);
                ToBytes(
                    data.TyrePressureRearRight * 100,
                    out _wheelsReport[15],
                    out _wheelsReport[16]);

                // Brake temperature
                ToBytes(
                   data.BrakeTemperatureFrontLeft,
                   out _wheelsReport[17],
                   out _wheelsReport[18]);
                ToBytes(
                   data.BrakeTemperatureFrontRight,
                   out _wheelsReport[19],
                   out _wheelsReport[20]);
                ToBytes(
                   data.BrakeTemperatureRearLeft,
                   out _wheelsReport[21],
                   out _wheelsReport[22]);
                ToBytes(
                   data.BrakeTemperatureRearRight,
                   out _wheelsReport[23],
                   out _wheelsReport[24]);

                // Tire wear
                _wheelsReport[25] = ToByte(data.TyreWearFrontLeft);
                _wheelsReport[26] = ToByte(data.TyreWearFrontRight);
                _wheelsReport[27] = ToByte(data.TyreWearRearLeft);
                _wheelsReport[28] = ToByte(data.TyreWearRearRight);
            }

            // --------------------------------------------------------
            // Private fields (ITelemetryData)
            // --------------------------------------------------------

            private Stopwatch _telemetryTimer = new Stopwatch();
            private Stopwatch _wheelsTelemetryTimer = new Stopwatch();
            private int _millisecondsPerFrame = 0;
            private byte[] _powertrainReport;
            private byte[] _ecuReport;
            private byte[] _raceControlReport;
            private byte[] _gaugesReport;
            private byte[] _wheelsReport;


        } // class Device
    } // namespace V1
} // namespace ESP32SimWheel
