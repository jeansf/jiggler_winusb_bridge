using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace UsbMouseBridge
{
    class Program
    {
        // GUID_DEVINTERFACE_USB_DEVICE = {A5DCBF10-6530-11D2-901F-00C04FB951ED}
        // Or WinUSB custom interface GUID from registry, or enumerate all WinUSB devices matching VID_062A&PID_4101&MI_01
        
        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public uint cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public uint cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct USB_INTERFACE_DESCRIPTOR
        {
            public byte bLength;
            public byte bDescriptorType;
            public byte bInterfaceNumber;
            public byte bAlternateSetting;
            public byte bNumEndpoints;
            public byte bInterfaceClass;
            public byte bInterfaceSubClass;
            public byte bInterfaceProtocol;
            public byte iInterface;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINUSB_PIPE_INFORMATION
        {
            public int PipeType;
            public byte PipeId;
            public ushort MaximumPacketSize;
            public byte Interval;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        const uint INPUT_MOUSE = 0;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        const uint MOUSEEVENTF_WHEEL = 0x0800;

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SetupDiGetClassDevs(IntPtr ClassGuid, string Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, out uint requiredSize, ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiDestroyDeviceInfoList(IntPtr hDevInfo);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_Initialize(IntPtr DeviceHandle, out IntPtr InterfaceHandle);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_Free(IntPtr InterfaceHandle);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_QueryInterfaceSettings(IntPtr InterfaceHandle, byte AlternateInterfaceNumber, out USB_INTERFACE_DESCRIPTOR UsbAltInterfaceDescriptor);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_QueryPipe(IntPtr InterfaceHandle, byte AlternateInterfaceNumber, byte PipeIndex, out WINUSB_PIPE_INFORMATION PipeInformation);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_ReadPipe(IntPtr InterfaceHandle, byte PipeID, byte[] Buffer, uint BufferLength, out uint LengthTransferred, IntPtr Overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_SetPipePolicy(IntPtr InterfaceHandle, byte PipeID, uint PolicyType, uint ValueLength, ref uint Value);

        const uint DIGCF_PRESENT = 0x00000002;
        const uint DIGCF_ALLCLASSES = 0x00000004;
        const uint DIGCF_DEVICEINTERFACE = 0x00000010;
        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint FILE_SHARE_READ = 0x00000001;
        const uint FILE_SHARE_WRITE = 0x00000002;
        const uint OPEN_EXISTING = 3;
        const uint FILE_FLAG_OVERLAPPED = 0x40000000;

        static string FindDevicePath()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB"))
                {
                    if (key != null)
                    {
                        foreach (var sub in key.GetSubKeyNames())
                        {
                            if (sub.StartsWith("VID_062A&PID_4101&MI_01", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var devKey = key.OpenSubKey(sub))
                                {
                                    foreach (var inst in devKey.GetSubKeyNames())
                                    {
                                        using (var paramKey = devKey.OpenSubKey(inst + @"\Device Parameters"))
                                        {
                                            if (paramKey != null)
                                            {
                                                var guids = paramKey.GetValue("DeviceInterfaceGUIDs") as string[];
                                                var guidStr = paramKey.GetValue("DeviceInterfaceGUID") as string;
                                                string g = (guids != null && guids.Length > 0) ? guids[0] : guidStr;
                                                if (!string.IsNullOrEmpty(g))
                                                {
                                                    Guid ifaceGuid = new Guid(g);
                                                    string path = GetPathByGuid(ifaceGuid, "vid_062a&pid_4101&mi_01");
                                                    if (path != null) return path;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            Guid usbGuid = new Guid("{A5DCBF10-6530-11D2-901F-00C04FB951ED}");
            return GetPathByGuid(usbGuid, "vid_062a&pid_4101&mi_01");
        }

        static string GetPathByGuid(Guid guid, string match)
        {
            IntPtr hDevInfo = SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES | DIGCF_DEVICEINTERFACE);
            if (hDevInfo == IntPtr.Zero || hDevInfo == new IntPtr(-1)) return null;
            try
            {
                SP_DEVICE_INTERFACE_DATA ifaceData = new SP_DEVICE_INTERFACE_DATA();
                ifaceData.cbSize = (uint)Marshal.SizeOf(ifaceData);
                uint idx = 0;
                while (SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref guid, idx, ref ifaceData))
                {
                    idx++;
                    SP_DEVICE_INTERFACE_DETAIL_DATA detail = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                    detail.cbSize = (uint)(IntPtr.Size == 8 ? 8 : (4 + Marshal.SystemDefaultCharSize));
                    SP_DEVINFO_DATA devInfo = new SP_DEVINFO_DATA();
                    devInfo.cbSize = (uint)Marshal.SizeOf(devInfo);
                    uint reqSize;
                    if (SetupDiGetDeviceInterfaceDetail(hDevInfo, ref ifaceData, ref detail, 512, out reqSize, ref devInfo))
                    {
                        if (detail.DevicePath != null && detail.DevicePath.ToLowerInvariant().Contains(match))
                        {
                            return detail.DevicePath;
                        }
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hDevInfo);
            }
            return null;
        }

        static void Main(string[] args)
        {
            byte prevButtons = 0;

            while (true)
            {
                string path = FindDevicePath();
                if (path == null)
                {
                    Thread.Sleep(1500);
                    continue;
                }

                IntPtr hFile = CreateFile(path, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);
                if (hFile == new IntPtr(-1))
                {
                    Thread.Sleep(1500);
                    continue;
                }

                IntPtr hWinUsb;
                if (!WinUsb_Initialize(hFile, out hWinUsb))
                {
                    CloseHandle(hFile);
                    Thread.Sleep(1500);
                    continue;
                }

                try
                {
                    USB_INTERFACE_DESCRIPTOR ifaceDesc;
                    if (!WinUsb_QueryInterfaceSettings(hWinUsb, 0, out ifaceDesc))
                    {
                        Thread.Sleep(1500);
                        continue;
                    }

                    byte inPipeId = 0;
                    ushort maxPacket = 64;
                    for (byte i = 0; i < ifaceDesc.bNumEndpoints; i++)
                    {
                        WINUSB_PIPE_INFORMATION pipeInfo;
                        if (WinUsb_QueryPipe(hWinUsb, 0, i, out pipeInfo))
                        {
                            if ((pipeInfo.PipeId & 0x80) != 0)
                            {
                                inPipeId = pipeInfo.PipeId;
                                maxPacket = pipeInfo.MaximumPacketSize;
                            }
                        }
                    }

                    if (inPipeId == 0)
                    {
                        Thread.Sleep(1500);
                        continue;
                    }

                    uint timeout = 1000;
                    WinUsb_SetPipePolicy(hWinUsb, inPipeId, 3, 4, ref timeout);

                    byte[] buffer = new byte[maxPacket];

                    while (true)
                    {
                        uint read = 0;
                        if (WinUsb_ReadPipe(hWinUsb, inPipeId, buffer, (uint)buffer.Length, out read, IntPtr.Zero))
                        {
                            if (read == 7 && (buffer[0] == 1 || buffer[0] == 2 || buffer[0] == 3))
                            {
                                byte buttons = buffer[1];
                                int rawX = buffer[2] | ((buffer[3] & 0x0F) << 8);
                                if ((rawX & 0x800) != 0) rawX -= 0x1000;

                                int rawY = ((buffer[3] >> 4) & 0x0F) | (buffer[4] << 4);
                                if ((rawY & 0x800) != 0) rawY -= 0x1000;

                                sbyte wheel = (sbyte)buffer[5];
                                ProcessMouseReport(buttons, ref prevButtons, rawX, rawY, wheel);
                            }
                            else if (read >= 3)
                            {
                                int offset = (read >= 4 && (buffer[0] == 1 || buffer[0] == 2 || buffer[0] == 3)) ? 1 : 0;
                                byte buttons = buffer[offset];
                                sbyte dx = (sbyte)buffer[offset + 1];
                                sbyte dy = (sbyte)buffer[offset + 2];
                                sbyte wheel = (read > offset + 3) ? (sbyte)buffer[offset + 3] : (sbyte)0;
                                ProcessMouseReport(buttons, ref prevButtons, dx, dy, wheel);
                            }
                        }
                        else
                        {
                            int err = Marshal.GetLastWin32Error();
                            if (err == 121) // ERROR_SEM_TIMEOUT
                            {
                                continue;
                            }
                            break;
                        }
                    }
                }
                finally
                {
                    WinUsb_Free(hWinUsb);
                    CloseHandle(hFile);
                }
            }
        }

        static void ProcessMouseReport(byte buttons, ref byte prevButtons, int dx, int dy, int wheel)
        {
            uint flags = 0;
            uint mouseData = 0;

            if (dx != 0 || dy != 0)
            {
                flags |= MOUSEEVENTF_MOVE;
            }

            byte changed = (byte)(buttons ^ prevButtons);
            if ((changed & 0x01) != 0) flags |= ((buttons & 0x01) != 0) ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;
            if ((changed & 0x02) != 0) flags |= ((buttons & 0x02) != 0) ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;
            if ((changed & 0x04) != 0) flags |= ((buttons & 0x04) != 0) ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP;
            prevButtons = buttons;

            if (wheel != 0)
            {
                flags |= MOUSEEVENTF_WHEEL;
                mouseData = (uint)(wheel * 120);
            }

            if (flags != 0)
            {
                mouse_event(flags, dx, dy, mouseData, UIntPtr.Zero);
            }
        }
    }
}
