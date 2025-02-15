// Decompiled with JetBrains decompiler
// Type: HidLibrary.HidDevice
// Assembly: SimHub.Plugins, Version=1.0.9151.26705, Culture=neutral, PublicKeyToken=null
// MVID: 9821E53B-9D0D-46DC-913E-6B209E69703E
// Assembly location: C:\Program Files (x86)\SimHub\SimHub.Plugins.dll

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SimHub.Plugins;

namespace ESP32SimWheel.HidAPI
{
    public class HidDevice : IDisposable
    {
        public IntPtr Handle { get; private set; }

        public bool IsOpen
        {
            get
            {
                return Handle.ToInt32() != NativeMethods.INVALID_HANDLE_VALUE;
            }
        }

        public string Path { get; private set; }

        public int LastError { get; private set; }

        public ushort VendorID { get; private set; }
        public ushort ProductID { get; private set; }

        public short Usage { get; private set; }
        public short UsagePage { get; private set; }
        public short InputReportByteLength { get; private set; }
        public short OutputReportByteLength { get; private set; }
        public short FeatureReportByteLength { get; private set; }


        public HidDevice(string path)
        {
            this.Path = path;
            this.Handle = (System.IntPtr)NativeMethods.INVALID_HANDLE_VALUE;
            Open();
        }

        public bool Open()
        {
            if (!IsOpen)
            {
                NativeMethods.SECURITY_ATTRIBUTES lpSecurityAttributes = new NativeMethods.SECURITY_ATTRIBUTES();
                lpSecurityAttributes.lpSecurityDescriptor = IntPtr.Zero;
                lpSecurityAttributes.bInheritHandle = true;
                lpSecurityAttributes.nLength = Marshal.SizeOf<NativeMethods.SECURITY_ATTRIBUTES>(lpSecurityAttributes);
                Handle = NativeMethods.CreateFile(
                    Path,
                    DEVICE_ACCESS,
                    SHARE_MODE,
                    ref lpSecurityAttributes,
                    NativeMethods.OPEN_EXISTING,
                    FLAGS_AND_ATTRIBUTES,
                    0);
                if (IsOpen)
                {
                    LastError = 0;
                    // Get hardware ID
                    NativeMethods.HIDD_ATTRIBUTES attributes = new NativeMethods.HIDD_ATTRIBUTES();
                    attributes.Size = Marshal.SizeOf<NativeMethods.HIDD_ATTRIBUTES>(attributes);
                    if (NativeMethods.HidD_GetAttributes(Handle, ref attributes))
                    {
                        VendorID = attributes.VendorID;
                        ProductID = attributes.ProductID;
                    }
                    else
                    {
                        LastError = Marshal.GetLastWin32Error();
                        SimHub.Logging.Current.InfoFormat(
                            "[ESP32 Sim-wheel] [HidAPI] HidD_GetAttributes() failed with code {0}",
                            LastError);
                        Close();
                        return false;
                    }

                    // Get HID capabilities
                    NativeMethods.HIDP_CAPS capabilities = new NativeMethods.HIDP_CAPS();
                    IntPtr preparsedData = new IntPtr();
                    if (NativeMethods.HidD_GetPreparsedData(Handle, ref preparsedData))
                    {
                        NativeMethods.HidP_GetCaps(preparsedData, ref capabilities);
                        NativeMethods.HidD_FreePreparsedData(preparsedData);
                    }
                    else
                    {
                        LastError = Marshal.GetLastWin32Error();
                        SimHub.Logging.Current.InfoFormat(
                            "[ESP32 Sim-wheel] [HidAPI] HidD_GetPreparsedData() failed with code {0}",
                            LastError);
                        Close();
                        return false;
                    }
                    Usage = capabilities.Usage;
                    UsagePage = capabilities.UsagePage;
                    InputReportByteLength = capabilities.InputReportByteLength;
                    FeatureReportByteLength = capabilities.FeatureReportByteLength;
                    OutputReportByteLength = capabilities.OutputReportByteLength;

                    // Try to increase read buffers
                    if (!NativeMethods.HidD_SetNumInputBuffers(Handle, 256))
                    {
                        LastError = Marshal.GetLastWin32Error();
                        SimHub.Logging.Current.InfoFormat(
                            "[ESP32 Sim-wheel] [HidAPI] HidD_SetNumInputBuffers() failed with code {0}",
                            LastError);
                    }

                    return true;
                }
                else
                {
                    LastError = Marshal.GetLastWin32Error();
                    VendorID = 0;
                    ProductID = 0;
                    Usage = 0;
                    UsagePage = 0;
                    InputReportByteLength = 0;
                    FeatureReportByteLength = 0;
                    OutputReportByteLength = 0;
                    return false;
                }
            }
            return true;
        }

        public bool GetFeature(byte id, out byte[] data)
        {
            data = new byte[(int)FeatureReportByteLength];
            if (FeatureReportByteLength > 1)
            {
                data[0] = id;
                return GetFeature(ref data);
            }
            return false;
        }

        public bool GetFeature(ref byte[] data)
        {
            if (Open())
            {
                if (!NativeMethods.HidD_GetFeature(Handle, data, data.Length))
                {
                    LastError = Marshal.GetLastWin32Error();
                    SimHub.Logging.Current.InfoFormat(
                        "[ESP32 Sim-wheel] [HidAPI] HidD_GetFeature() failed with code {0}",
                        LastError);
                    Close();
                    return false;
                }
                return true;
            }
            return false;
        }

        public bool SetFeature(byte[] data)
        {
            if (Open())
            {
                if (!NativeMethods.HidD_SetFeature(Handle, data, data.Length))
                {
                    LastError = Marshal.GetLastWin32Error();
                    Close();
                    return false;
                }
                return true;
            }
            return false;
        }

        public bool Write(byte[] data)
        {
            if ((data.Length > 1) && Open())
            {
                uint lpNumberOfBytesWritten = 0;
                if (!NativeMethods.WriteFile(
                    Handle,
                    data,
                    (uint)data.Length,
                    out lpNumberOfBytesWritten,
                    IntPtr.Zero))
                {
                    LastError = Marshal.GetLastWin32Error();
                    Close();
                    return false;
                }
                return (lpNumberOfBytesWritten == data.Length);
            }
            return false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Close();
        }

        public void Close()
        {
            if (IsOpen)
            {
                NativeMethods.CloseHandle(Handle);
                Handle = (IntPtr)NativeMethods.INVALID_HANDLE_VALUE;
            }
        }

        private const uint DEVICE_ACCESS =
            NativeMethods.GENERIC_READ |
            NativeMethods.GENERIC_WRITE;
        private const uint SHARE_MODE =
            NativeMethods.FILE_SHARE_READ |
            NativeMethods.FILE_SHARE_WRITE;
        private const uint FLAGS_AND_ATTRIBUTES =
            NativeMethods.FILE_ATTRIBUTE_NORMAL |
            NativeMethods.FILE_FLAG_NO_BUFFERING |
            NativeMethods.FILE_FLAG_WRITE_THROUGH;
    }
}
