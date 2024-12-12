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

            public bool SendTelemetry(ref GameData data)
            {
                if ((_millisecondsPerFrame > 0) &&
                    (!_telemetryTimer.IsRunning || (_telemetryTimer.ElapsedMilliseconds >= _millisecondsPerFrame)))
                    try
                    {
                        _telemetryTimer.Stop();
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
            // Pseudo-constructor
            // --------------------------------------------------------

            partial void InitializeTelemetryData()
            {
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
            // Private fields (ITelemetryData)
            // --------------------------------------------------------

            private Stopwatch _telemetryTimer = new Stopwatch();
            private int _millisecondsPerFrame = 0;
            private byte[] _powertrainReport;
            private byte[] _ecuReport;
            private byte[] _raceControlReport;
            private byte[] _gaugesReport;


        } // class Device
    } // namespace V1
} // namespace ESP32SimWheel
