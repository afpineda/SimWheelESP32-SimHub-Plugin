#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2025-02-13
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ESP32SimWheel.HidAPI
{
    internal static class NativeMethods
    {
        internal const uint FILE_SHARE_READ = 1;
        internal const uint FILE_SHARE_WRITE = 2;
        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const int INVALID_HANDLE_VALUE = -1;
        internal const uint OPEN_EXISTING = 3;
        internal const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        internal const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        internal const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool FlushFileBuffers(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            ref NativeMethods.SECURITY_ATTRIBUTES lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            uint hTemplateFile);

        [DllImport("kernel32.dll")]
        internal static extern uint WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);

        [DllImport("kernel32.dll")]
        internal static extern bool WriteFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("hid.dll")]
        internal static extern bool HidD_GetFeature(
            IntPtr hidDeviceObject,
            byte[] lpReportBuffer,
            int reportBufferLength);

        [DllImport("hid.dll")]
        internal static extern bool HidD_GetInputReport(
            IntPtr hidDeviceObject,
            byte[] lpReportBuffer,
            int reportBufferLength);

        [DllImport("hid.dll")]
        internal static extern void HidD_GetHidGuid(ref Guid hidGuid);

        [DllImport("hid.dll")]
        internal static extern bool HidD_SetFeature(
            IntPtr hidDeviceObject,
            byte[] lpReportBuffer,
            int reportBufferLength);

        [DllImport("hid.dll")]
        internal static extern bool HidD_SetOutputReport(
            IntPtr hidDeviceObject,
            byte[] lpReportBuffer,
            int reportBufferLength);

        [DllImport("hid.dll")]
        internal static extern bool HidD_GetAttributes(
            IntPtr hidDeviceObject,
            ref NativeMethods.HIDD_ATTRIBUTES attributes);

        [DllImport("hid.dll")]
        internal static extern int HidP_GetCaps(
            IntPtr preparsedData,
            ref NativeMethods.HIDP_CAPS capabilities);

        [DllImport("hid.dll")]
        internal static extern bool HidD_GetPreparsedData(
              IntPtr hidDeviceObject,
              ref IntPtr preparsedData);


        [DllImport("hid.dll")]
        internal static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

#pragma warning disable 0649

        internal struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        internal struct HIDD_ATTRIBUTES
        {
            internal int Size;
            internal ushort VendorID;
            internal ushort ProductID;
            internal short VersionNumber;
        }

        internal struct HIDP_CAPS
        {
            internal short Usage;
            internal short UsagePage;
            internal short InputReportByteLength;
            internal short OutputReportByteLength;
            internal short FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            internal short[] Reserved;
            internal short NumberLinkCollectionNodes;
            internal short NumberInputButtonCaps;
            internal short NumberInputValueCaps;
            internal short NumberInputDataIndices;
            internal short NumberOutputButtonCaps;
            internal short NumberOutputValueCaps;
            internal short NumberOutputDataIndices;
            internal short NumberFeatureButtonCaps;
            internal short NumberFeatureValueCaps;
            internal short NumberFeatureDataIndices;
        }

#pragma warning restore 0649

    }
}
