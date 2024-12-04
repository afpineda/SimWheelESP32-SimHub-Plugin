#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-04
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using GameReaderCommon;

namespace ESP32SimWheel
{
    public enum ClutchWorkingModes : byte
    {
        Clutch = 0,
        Axis,
        ALT,
        Button
    }

    public enum AltButtonWorkingModes : byte
    {
        Button = 0,
        ALT
    }

    public enum DPadWorkingModes : byte
    {
        Button = 0,
        Navigation
    }

    public struct Capabilities
    {
        public byte FramesPerSecond { get; }
        public bool HasAltButtons { get; }
        public bool HasAnalogClutch { get; }
        public bool HasBattery { get; }
        public bool HasBatteryCalibrationData { get; }
        public bool HasClutch { get; }
        public bool HasDPad { get; }
        public bool UsesPowertrainTelemetry { get; }
        public bool UsesEcuTelemetry { get; }
        public bool UsesRaceControlTelemetry { get; }
        public bool UsesGaugesTelemetry { get; }
        public bool UsesTelemetryData
        {
            get
            {
                return UsesPowertrainTelemetry || UsesEcuTelemetry ||
                       UsesRaceControlTelemetry || UsesGaugesTelemetry;
            }
        }

        public Capabilities(ushort flags, byte fps)
        {
            HasAnalogClutch = (flags & 0x0002) != 0;
            HasClutch = HasAnalogClutch || ((flags & 0x0001) != 0);
            HasAltButtons = (flags & 0x0004) != 0;
            HasDPad = (flags & 0x0008) != 0;
            HasBattery = (flags & 0x0010) != 0;
            HasBatteryCalibrationData = (flags & 0x0020) != 0;
            UsesPowertrainTelemetry = (fps > 0) && (flags & 0x0040) != 0;
            UsesEcuTelemetry = (fps > 0) && (flags & 0x0080) != 0;
            UsesRaceControlTelemetry = (fps > 0) && (flags & 0x0100) != 0;
            UsesGaugesTelemetry = (fps > 0) && (flags & 0x0200) != 0;
            FramesPerSecond = fps;
        }
    }

    public struct HidInfo
    {
        public int VendorID { get; internal set; }
        public int ProductID { get; internal set; }
        public string Path { get; internal set; }
        public string DisplayName { get; internal set; }
        public string Manufacturer { get; internal set; }
    }

    public struct DataVersion
    {
        public ushort Major { get; internal set; }
        public ushort Minor { get; internal set; }
        public bool IsCompatible(ushort withMajor, ushort withMinor)
        {
            return (Major == withMajor) && (withMinor <= Minor);
        }
    }

    public interface IClutch
    {
        ClutchWorkingModes ClutchWorkingMode { get; set; }
        byte BitePoint { get; set; }
    }

    public interface IAnalogClutch : IClutch
    {
        void ForceAxisCalibration();
        void ReverseLeftAxis();
        void ReverseRightAxis();
    }

    public interface ISecurityLock
    {
        bool isLocked { get; }
    }

    public interface IAltButtons
    {
        AltButtonWorkingModes AltButtonsWorkingMode { get; set; }
    }

    public interface IDpad
    {
        DPadWorkingModes DPadWorkingMode { get; set; }
    }

    public interface ITelemetryData
    {
        long MillisecondsPerTelemetryFrame { get; }
        bool SendTelemetry(ref GameData data);
    }

    public interface IBattery
    {
        void ForceBatteryCalibration();
        byte BatteryLevel { get; }
        bool IsFactoryCalibrated { get; }
    }

    public interface IDevice
    {
        Capabilities Capabilities { get; }
        HidInfo HidInfo { get; }
        DataVersion DataVersion { get; }
        IClutch Clutch { get; }
        IAnalogClutch AnalogClutch { get; }
        ISecurityLock SecurityLock { get; }
        IBattery Battery { get; }
        ITelemetryData TelemetryData { get; }
        IDpad DPad { get; }
        ulong UniqueID { get; }
    }
}