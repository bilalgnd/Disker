using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Disker.Core.Native
{
    public static class NativeDisk
    {
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint OPEN_EXISTING = 3;
        public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        // IOCTL ve FSCTL kodları
        public const uint IOCTL_DISK_GET_DRIVE_LAYOUT_EX = 0x00070050;
        public const uint IOCTL_DISK_GET_DRIVE_GEOMETRY_EX = 0x000700A0;
        public const uint IOCTL_DISK_GET_DRIVE_GEOMETRY = 0x00070000;
        public const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x002D1400;
        public const uint SMART_GET_VERSION = 0x00074080;
        public const uint SMART_RCV_DRIVE_DATA = 0x0007C088;
        public const uint FSCTL_LOCK_VOLUME = 0x00090018;
        public const uint FSCTL_UNLOCK_VOLUME = 0x0009001C;
        public const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [StructLayout(LayoutKind.Sequential)]
        public struct DISK_GEOMETRY
        {
            public long Cylinders;
            public int MediaType;
            public uint TracksPerCylinder;
            public uint SectorsPerTrack;
            public uint BytesPerSector;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISK_GEOMETRY_EX
        {
            public DISK_GEOMETRY Geometry;
            public long DiskSize;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STORAGE_PROPERTY_QUERY
        {
            public int PropertyId;
            public int QueryType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] AdditionalParameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STORAGE_ACCESS_ALIGNMENT_DESCRIPTOR
        {
            public uint Version;
            public uint Size;
            public uint BytesPerLogicalSector;
            public uint BytesPerPhysicalSector;
            public uint BytesOffsetForSectorAlignment;
            public uint BytesPerCacheLine;
            public uint BytesOffsetForCacheAlignment;
        }

        public static SafeFileHandle OpenDisk(int diskNumber, bool writeAccess = false)
        {
            string path = $@"\\.\PhysicalDrive{diskNumber}";
            uint access = GENERIC_READ | (writeAccess ? GENERIC_WRITE : 0);
            uint share = FILE_SHARE_READ | FILE_SHARE_WRITE;

            SafeFileHandle handle = CreateFile(
                path,
                access,
                share,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            return handle;
        }

        public static (uint logicalSectorSize, uint physicalSectorSize, long totalSectors, uint bytesPerSector) GetDiskSectorGeometry(int diskNumber)
        {
            using SafeFileHandle handle = OpenDisk(diskNumber);
            if (handle.IsInvalid)
            {
                return (512, 4096, 0, 512);
            }

            uint logicalSector = 512;
            uint physicalSector = 4096;
            long diskSize = 0;
            uint bytesPerSector = 512;

            // 1. IOCTL_DISK_GET_DRIVE_GEOMETRY_EX
            int geomExSize = 1024;
            IntPtr geomExBuffer = Marshal.AllocHGlobal(geomExSize);
            try
            {
                if (DeviceIoControl(handle, IOCTL_DISK_GET_DRIVE_GEOMETRY_EX, IntPtr.Zero, 0, geomExBuffer, (uint)geomExSize, out uint bytesReturned, IntPtr.Zero))
                {
                    var geomEx = Marshal.PtrToStructure<DISK_GEOMETRY_EX>(geomExBuffer);
                    diskSize = geomEx.DiskSize;
                    bytesPerSector = geomEx.Geometry.BytesPerSector;
                    logicalSector = bytesPerSector;
                }
            }
            catch { }
            finally
            {
                Marshal.FreeHGlobal(geomExBuffer);
            }

            // 2. IOCTL_STORAGE_QUERY_PROPERTY -> StorageAccessAlignmentProperty
            var query = new STORAGE_PROPERTY_QUERY
            {
                PropertyId = 6, // StorageAccessAlignmentProperty
                QueryType = 0   // PropertyStandardQuery
            };

            int querySize = Marshal.SizeOf(query);
            int alignSize = Marshal.SizeOf<STORAGE_ACCESS_ALIGNMENT_DESCRIPTOR>();
            IntPtr queryPtr = Marshal.AllocHGlobal(querySize);
            IntPtr alignPtr = Marshal.AllocHGlobal(alignSize);

            try
            {
                Marshal.StructureToPtr(query, queryPtr, false);
                if (DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, queryPtr, (uint)querySize, alignPtr, (uint)alignSize, out uint alignReturned, IntPtr.Zero))
                {
                    var align = Marshal.PtrToStructure<STORAGE_ACCESS_ALIGNMENT_DESCRIPTOR>(alignPtr);
                    if (align.BytesPerLogicalSector > 0) logicalSector = align.BytesPerLogicalSector;
                    if (align.BytesPerPhysicalSector > 0) physicalSector = align.BytesPerPhysicalSector;
                }
            }
            catch { }
            finally
            {
                Marshal.FreeHGlobal(queryPtr);
                Marshal.FreeHGlobal(alignPtr);
            }

            long totalSectors = logicalSector > 0 && diskSize > 0 ? diskSize / logicalSector : 0;
            return (logicalSector, physicalSector, totalSectors, bytesPerSector);
        }

        // Doğrudan Donanım Sensöründen Canlı Disk Sıcaklığı Okuma (IOCTL_STORAGE_QUERY_PROPERTY)
        public static int? GetLiveDiskTemperature(int diskNumber)
        {
            using SafeFileHandle handle = OpenDisk(diskNumber);
            if (handle.IsInvalid) return null;

            // StorageDeviceTemperatureInfo = 12
            var query = new STORAGE_PROPERTY_QUERY
            {
                PropertyId = 12, // StorageDeviceTemperatureInfo
                QueryType = 0    // PropertyStandardQuery
            };

            int querySize = Marshal.SizeOf(query);
            int bufferSize = 512;
            IntPtr queryPtr = Marshal.AllocHGlobal(querySize);
            IntPtr bufferPtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                Marshal.StructureToPtr(query, queryPtr, false);
                if (DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, queryPtr, (uint)querySize, bufferPtr, (uint)bufferSize, out uint returned, IntPtr.Zero) && returned >= 8)
                {
                    // STORAGE_TEMPERATURE_DATA_DESCRIPTOR
                    // +0: Version (uint)
                    // +4: Size (uint)
                    // +8: CriticalTemperature (short)
                    // +10: WarningTemperature (short)
                    // +12: InfoCount (ushort)
                    // +14: Reserved0 (ushort)
                    // +16: STORAGE_TEMPERATURE_INFO[0].Temperature (short in Celsius)
                    if (returned >= 20)
                    {
                        short tempCelsius = Marshal.ReadInt16(bufferPtr, 16);
                        if (tempCelsius > 0 && tempCelsius < 120)
                        {
                            return (int)tempCelsius;
                        }
                    }
                }
            }
            catch { }
            finally
            {
                Marshal.FreeHGlobal(queryPtr);
                Marshal.FreeHGlobal(bufferPtr);
            }

            return null;
        }

        public static bool LockAndDismountVolume(string driveLetter)
        {
            string path = $@"\\.\{driveLetter.TrimEnd('\\')}";
            using SafeFileHandle hVolume = CreateFile(
                path,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (hVolume.IsInvalid) return false;

            DeviceIoControl(hVolume, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
            DeviceIoControl(hVolume, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
            DeviceIoControl(hVolume, FSCTL_UNLOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);

            return true;
        }
    }
}
