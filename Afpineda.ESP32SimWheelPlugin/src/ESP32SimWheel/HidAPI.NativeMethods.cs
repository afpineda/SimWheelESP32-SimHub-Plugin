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

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool WriteFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool HidD_GetFeature(
            IntPtr hidDeviceObject,
            byte[] lpReportBuffer,
            int reportBufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool HidD_GetInputReport(
            IntPtr hidDeviceObject,
            byte[] lpReportBuffer,
            int reportBufferLength);

        [DllImport("hid.dll")]
        internal static extern void HidD_GetHidGuid(ref Guid hidGuid);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool HidD_SetFeature(
            IntPtr hidDeviceObject,
            byte[] lpReportBuffer,
            int reportBufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
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

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool HidD_GetPreparsedData(
              IntPtr hidDeviceObject,
              ref IntPtr preparsedData);

        [DllImport("hid.dll")]
        internal static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool HidD_SetNumInputBuffers(
            IntPtr hidDeviceObject,
            ulong NumberBuffers);

        [DllImport("hid.dll")]
        internal static extern bool HidD_FlushQueue(
            IntPtr HidDeviceObject);

        [DllImport("cfgmgr32.dll", SetLastError = false, ExactSpelling = true)]
        internal static extern int CM_Register_Notification(
            ref CM_NOTIFY_FILTER pFilter,
            IntPtr pContext,
            CM_NOTIFY_CALLBACK pCallback,
            out IntPtr pNotifyContext
        );

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        internal static extern int CM_Unregister_Notification(
            IntPtr pNotifyContext
        );

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

        [StructLayout(LayoutKind.Sequential)]
        internal struct CM_NOTIFY_FILTER
        {
            internal int cbSize;
            internal int Flags;
            internal int FilterType;
            internal int Reserved;
            // Actually this is a a union
            // but we are using the DeviceInterface substructure only
            internal Guid DeviceInterface;
            // The following filler is required.
            // The size of this struct must match 416 bytes.
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 384)]
            string filler;
        }

        // NOT NEEDED FOR NOW
        // [StructLayout(LayoutKind.Sequential)]
        // internal struct CM_NOTIFY_EVENT_DATA
        // {
        //     public int FilterType;
        //     public int Reserved;
        //     // Actually this is a a union
        //     // but we are using the DeviceInterface substructure only
        //     public Guid ClassGuid;
        //     public IntPtr SymbolicLink;
        // }

#pragma warning restore 0649

        // internal delegate int CM_NOTIFY_CALLBACK(
        //     IntPtr hNotify,
        //     IntPtr context,
        //     int action,
        //     ref CM_NOTIFY_EVENT_DATA eventData,
        //     int eventDataSize);

        internal delegate int CM_NOTIFY_CALLBACK(
            IntPtr hNotify,
            IntPtr context,
            int action,
            IntPtr eventData,
            int eventDataSize);


        internal const int CM_NOTIFY_FILTER_TYPE_DEVICE_INTERFACE = 0x00000000;
        // internal const int CM_NOTIFY_ACTION_DEVICE_INTERFACE_ARRIVAL = 0x00000000;
        // internal const int CM_NOTIFY_ACTION_DEVICE_INTERFACE_REMOVAL = 0x00000001;

        internal const uint ERROR_CANCELLED = 0x4C7;
        internal const uint ERROR_INVALID_FUNCTION = 0x01;
        internal const uint ERROR_DEVICE_IN_USE = 0x964;
        internal const uint ERROR_DEV_NOT_EXIST = 0x37;
        internal const uint ERROR_DEVICE_NOT_CONNECTED = 0x48F;
        internal const uint ERROR_DEVICE_UNREACHABLE = 0x141;

    }
}
