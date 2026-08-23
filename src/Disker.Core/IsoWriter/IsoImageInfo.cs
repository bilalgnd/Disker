using System;
using System.IO;

namespace Disker.Core.IsoWriter
{
    /// <summary>
    /// ISO dosyasını temsil eden bilgi modeli.
    /// Rufus'un GetDrivePartitionData / iso.c analizinden esinlenilerek C# ile yazıldı.
    /// </summary>
    public class IsoImageInfo
    {
        public string FilePath { get; }
        public string FileName { get; }
        public long FileSizeBytes { get; }
        public string DetectedType { get; private set; } = "Bilinmeyen ISO";

        public IsoImageInfo(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("ISO dosyası bulunamadı.", filePath);

            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            FileSizeBytes = new FileInfo(filePath).Length;
            DetectedType = DetectIsoType(filePath);
        }

        /// <summary>
        /// ISO içeriğini hızlıca analiz ederek türünü belirler.
        /// Rufus'un iso.c scan mantığından esinlenilmiştir.
        /// </summary>
        private static string DetectIsoType(string filePath)
        {
            try
            {
                string nameLower = Path.GetFileName(filePath).ToLowerInvariant();

                // Dosya adından hızlı tespit
                if (nameLower.Contains("windows") || nameLower.StartsWith("win"))
                    return "Windows ISO";
                if (nameLower.Contains("ubuntu") || nameLower.Contains("debian") ||
                    nameLower.Contains("fedora") || nameLower.Contains("mint") ||
                    nameLower.Contains("arch") || nameLower.Contains("kali") ||
                    nameLower.Contains("linux"))
                    return "Linux ISO";
                if (nameLower.Contains("macos") || nameLower.Contains("osx"))
                    return "macOS ISO";

                // MBR imzası kontrolü (offset 510: 0x55, 511: 0xAA)
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (fs.Length < 512) return "Küçük ISO";

                byte[] mbr = new byte[512];
                fs.Read(mbr, 0, 512);

                bool hasMbrSignature = mbr[510] == 0x55 && mbr[511] == 0xAA;

                // ISO 9660 Primary Volume Descriptor (sektör 16, offset 0x8000)
                if (fs.Length > 0x8010)
                {
                    fs.Seek(0x8001, SeekOrigin.Begin);
                    byte[] pvd = new byte[5];
                    fs.Read(pvd, 0, 5);
                    bool isIso9660 = pvd[0] == 0x43 && pvd[1] == 0x44 && pvd[2] == 0x30 &&
                                     pvd[3] == 0x30 && pvd[4] == 0x31; // "CD001"

                    if (isIso9660)
                    {
                        return hasMbrSignature ? "Bootable ISO (MBR+ISO9660)" : "ISO 9660 Optik Görüntü";
                    }
                }

                return hasMbrSignature ? "Bootable Disk Image (MBR)" : "Ham Disk Görüntüsü";
            }
            catch
            {
                return "ISO / Disk Görüntüsü";
            }
        }

        public string FileSizeFormatted
        {
            get
            {
                const long GB = 1073741824L;
                const long MB = 1048576L;

                return FileSizeBytes >= GB
                    ? $"{FileSizeBytes / (double)GB:F2} GB"
                    : $"{FileSizeBytes / (double)MB:F0} MB";
            }
        }
    }
}
