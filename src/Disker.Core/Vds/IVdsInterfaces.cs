using System;
using System.Runtime.InteropServices;

namespace Disker.Core.Vds
{
    [ComImport]
    [Guid("9c38ed61-d565-4728-aeee-c80952f0ecde")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsServiceLoader
    {
        void LoadService([MarshalAs(UnmanagedType.LPWStr)] string machineName, out IVdsService service);
    }

    [ComImport]
    [Guid("0818a8ef-9ba9-40d8-a6f9-e22833cc771e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsService
    {
        void IsServiceReady();
        void WaitForServiceReady();
        void GetProperties(out VDS_SERVICE_PROP serviceProp);
        void QueryProviders(uint flags, out IEnumVdsObject enumProviders);
        void QueryMaskedDisks(out IEnumVdsObject enumDisks);
        void QueryUnallocatedDisks(out IEnumVdsObject enumDisks);
        void GetObject(Guid objectId, VDS_OBJECT_TYPE type, [MarshalAs(UnmanagedType.IUnknown)] out object vdsObject);
        void QueryDriveLetters(char firstLetter, uint count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] VDS_DRIVE_LETTER_PROP[] driveLetterProps);
        void QueryFileSystemTypes(out IntPtr fileSystemTypeProps, out uint numberOfFileSystemTypes);
        void Reenumerate();
        void Refresh();
        void CleanupObsoleteMountPoints();
    }

    [ComImport]
    [Guid("118610b7-8d94-4030-b5b8-500889788e4e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumVdsObject
    {
        [PreserveSig]
        int Next(uint celt, [MarshalAs(UnmanagedType.IUnknown)] out object ppObjectArray, out uint pcFetched);
        void Skip(uint celt);
        void Reset();
        void Clone(out IEnumVdsObject ppEnum);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VDS_SERVICE_PROP
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string pwszVersion;
        public uint ulFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VDS_DRIVE_LETTER_PROP
    {
        public char wcLetter;
        public Guid volumeId;
        public uint ulFlags;
        public bool bUsed;
    }

    public enum VDS_OBJECT_TYPE
    {
        VDS_OT_UNKNOWN = 0,
        VDS_OT_PROVIDER = 1,
        VDS_OT_PACK = 2,
        VDS_OT_VOLUME = 3,
        VDS_OT_DISK = 4,
        VDS_OT_SUB_SYSTEM = 5,
        VDS_OT_CONTROLLER = 6,
        VDS_OT_DRIVE = 7,
        VDS_OT_LUN = 8,
        VDS_OT_PORT = 9,
        VDS_OT_PORTAL = 10,
        VDS_OT_TARGET = 11,
        VDS_OT_PORTAL_GROUP = 12,
        VDS_OT_STORAGE_POOL = 13,
        VDS_OT_STORAGE_TIER = 14
    }

    public static class VdsServiceManager
    {
        private static readonly Guid CLSID_VdsLoader = new("9c38ed61-d565-4728-aeee-c80952f0ecde");

        public static IVdsService? InitializeVds()
        {
            try
            {
                Type? loaderType = Type.GetTypeFromCLSID(CLSID_VdsLoader);
                if (loaderType == null) return null;

                var loader = Activator.CreateInstance(loaderType) as IVdsServiceLoader;
                if (loader == null) return null;

                loader.LoadService(null!, out IVdsService service);
                service.WaitForServiceReady();
                return service;
            }
            catch
            {
                return null;
            }
        }
    }
}
