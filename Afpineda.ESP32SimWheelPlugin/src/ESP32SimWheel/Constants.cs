#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-04
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

namespace ESP32SimWheel
{
    // Magic number
    static class MagicNumber
    {
        internal const byte MAGIC_NUMBER_LOW = 0x51;
        internal const byte MAGIC_NUMBER_HIGH = 0xBF;
    }

    namespace V1
    {
        static class Constants
        {
            // Report ID
            internal const byte RID_FEATURE_CAPABILITIES = 2;
            internal const byte RID_FEATURE_CONFIG = 3;
            internal const byte RID_OUTPUT_POWERTRAIN = 20;
            internal const byte RID_OUTPUT_ECU = 21;
            internal const byte RID_OUTPUT_RACE_CONTROL = 22;
            internal const byte RID_OUTPUT_GAUGES = 23;
            internal const byte RID_OUTPUT_PIXEL = 30;

            // Report sizes
            // Note: must increase data size in 1 to make room for the report-ID field
            internal const short REPORT2_SIZE_V1_0 = 8 + 1;
            internal const short REPORT2_SIZE_V1_1 = REPORT2_SIZE_V1_0 + 8;
            internal const short REPORT2_SIZE_V1_3 = REPORT2_SIZE_V1_1 + 1;
            internal const short REPORT2_SIZE_V1_4 = REPORT2_SIZE_V1_3 + 3;
            internal const short REPORT3_SIZE_V1_0 = 4 + 1;
            internal const short REPORT3_SIZE_V1_1 = REPORT3_SIZE_V1_0 + 1;
            internal const short REPORT3_SIZE_V1_2 = REPORT3_SIZE_V1_1 + 1;
            internal const short REPORT20_SIZE_V1_3 = 10 + 1;
            internal const short REPORT21_SIZE_V1_3 = 9 + 1;
            internal const short REPORT22_SIZE_V1_3 = 11 + 1;
            internal const short REPORT23_SIZE_V1_3 = 12 + 1;
            internal const short REPORT30_SIZE_V1_4 = 6 + 1;

            // Simple commands
            internal const byte CMD_SHOW_PIXELS = 7;
            internal const byte CMD_RESET_PIXELS = 8;

            // version
            internal const ushort SUPPORTED_MINOR_VERSION = 4;
        } // class Constants
    } // namespace V1
} // namespace ESP32SimWheel