using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using Disker.Core.Models;
using Disker.Core.Safety;

namespace Disker.Core.Wmi
{
    public class WmiStorageProvider
    {
        private readonly SafetyGuardService _safetyGuard;

        private static readonly string[] VibrantDiskColors = new[]
        {
            "#8B5CF6", // Mor (Lenore C:)
            "#06B6D4", // Camgöbeği (Carmilla D:)
            "#10B981", // Zümrüt Yeşili (Lyra F:)
            "#F59E0B", // Kehribar (Seth X:)
            "#EC4899", // Gül Pembesi (Kaiba Z:)
            "#3B82F6", // Elektrik Mavisi
            "#84CC16", // Lime / Fıstık Yeşili
            "#F97316", // Ateş Turuncusu
            "#6366F1"  // İndigo
        };

        public WmiStorageProvider(SafetyGuardService safetyGuard)
        {
            _safetyGuard = safetyGuard;
        }

        public List<PhysicalDiskInfo> GetDisks()
        {
            var diskList = new List<PhysicalDiskInfo>();

            // 1. Win32_DiskDrive Sorgusu
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    int index = Convert.ToInt32(obj["Index"]);
                    string model = obj["Model"]?.ToString()?.Trim() ?? $"Disk #{index}";
                    string serial = obj["SerialNumber"]?.ToString()?.Trim() ?? string.Empty;
                    string iface = obj["InterfaceType"]?.ToString()?.Trim() ?? "SCSI";
                    ulong size = Convert.ToUInt64(obj["Size"] ?? 0);

                    var diskInfo = new PhysicalDiskInfo
                    {
                        DiskNumber = index,
                        FriendlyName = model,
                        SerialNumber = serial,
                        BusType = iface,
                        SizeBytes = size,
                        PartitionStyle = PartitionStyleType.GPT,
                        IsOnline = true
                    };

                    diskList.Add(diskInfo);
                }
            }

            // 2. PhysicalDisk üzerinden MediaType ve Sağlık Sorgusu
            PopulatePhysicalDiskAttributes(diskList);

            // 3. Bölümleri ve Birimleri Yükle (Win32_DiskPartition + LogicalDisk)
            PopulatePartitionsAndVolumes(diskList);

            // 4. Disklere Varsayılan Canlı ve Ayırt Edici Renk Teması Ata
            int colorIdx = 0;
            foreach (var disk in diskList)
            {
                string mainLetter = disk.Partitions.FirstOrDefault(p => !string.IsNullOrEmpty(p.DriveLetter))?.DriveLetter?.TrimEnd(':', '\\').ToUpperInvariant() ?? "";
                if (mainLetter == "C") disk.ThemeColor = "#8B5CF6"; // Mor (Lenore C:)
                else if (mainLetter == "D") disk.ThemeColor = "#06B6D4"; // Camgöbeği (Carmilla D:)
                else if (mainLetter == "F") disk.ThemeColor = "#10B981"; // Zümrüt Yeşili (Lyra F:)
                else if (mainLetter == "X") disk.ThemeColor = "#F59E0B"; // Kehribar (Seth X:)
                else if (mainLetter == "Z") disk.ThemeColor = "#EC4899"; // Pembe (Kaiba Z:)
                else
                {
                    disk.ThemeColor = VibrantDiskColors[colorIdx % VibrantDiskColors.Length];
                    colorIdx++;
                }

                // Bölümlerin renklerini de diskin renk temasıyla senkronize et
                foreach (var part in disk.Partitions)
                {
                    part.PartitionColor = disk.ThemeColor;
                }
            }

            // 5. Güvenlik Değerlendirmesini Yap
            foreach (var disk in diskList)
            {
                var (isProtected, reason) = _safetyGuard.EvaluateDiskProtection(disk);
                disk.IsProtected = isProtected;
                disk.ProtectionReason = reason;
            }

            return diskList.OrderBy(d => d.DiskNumber).ToList();
        }

        private void PopulatePhysicalDiskAttributes(List<PhysicalDiskInfo> disks)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\Storage", "SELECT * FROM MSFT_PhysicalDisk");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string deviceId = obj["DeviceId"]?.ToString() ?? "";
                    if (!int.TryParse(deviceId, out int num)) continue;

                    var disk = disks.FirstOrDefault(d => d.DiskNumber == num);
                    if (disk == null) continue;

                    if (obj["MediaType"] != null)
                    {
                        ushort mediaType = Convert.ToUInt16(obj["MediaType"]);
                        disk.MediaType = mediaType switch
                        {
                            3 => DiskMediaType.HDD,
                            4 => DiskMediaType.SSD,
                            5 => DiskMediaType.NVMe,
                            _ => DiskMediaType.Unknown
                        };
                    }

                    if (obj["HealthStatus"] != null)
                    {
                        ushort health = Convert.ToUInt16(obj["HealthStatus"]);
                        disk.HealthStatus = health switch
                        {
                            0 => "Sağlıklı",
                            1 => "Uyarı",
                            2 => "Kritik Hata",
                            _ => "Bilinmiyor"
                        };
                    }
                }
            }
            catch { }
        }

        private void PopulatePartitionsAndVolumes(List<PhysicalDiskInfo> disks)
        {
            var driveInfoMap = new Dictionary<string, DriveInfo>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var d in DriveInfo.GetDrives())
                {
                    string key = d.Name.TrimEnd('\\', ':');
                    driveInfoMap[key] = d;
                }
            }
            catch { }

            try
            {
                // Win32_DiskPartition sorgusu
                using var partSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskPartition");
                foreach (ManagementObject partObj in partSearcher.Get())
                {
                    int diskIndex = Convert.ToInt32(partObj["DiskIndex"]);
                    uint partIndex = Convert.ToUInt32(partObj["Index"]);
                    ulong size = Convert.ToUInt64(partObj["Size"]);
                    string type = partObj["Type"]?.ToString() ?? "Temel Bölüm";
                    bool isBoot = partObj["BootPartition"] != null && Convert.ToBoolean(partObj["BootPartition"]);
                    bool isPrimary = partObj["PrimaryPartition"] != null && Convert.ToBoolean(partObj["PrimaryPartition"]);
                    string deviceId = partObj["DeviceID"]?.ToString() ?? "";

                    var disk = disks.FirstOrDefault(d => d.DiskNumber == diskIndex);
                    if (disk == null) continue;

                    var partInfo = new PartitionInfo
                    {
                        DiskNumber = diskIndex,
                        PartitionNumber = partIndex + 1,
                        SizeBytes = size,
                        IsBoot = isBoot,
                        IsSystem = isBoot,
                        PartitionType = type
                    };

                    // Win32_LogicalDiskToPartition ile Mantıksal Sürücü Harfini Eşleştir
                    string escapedDeviceId = deviceId.Replace("\\", "\\\\");
                    using var logicalSearcher = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{escapedDeviceId}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");

                    foreach (ManagementObject logicalObj in logicalSearcher.Get())
                    {
                        string? logicalLetter = logicalObj["DeviceID"]?.ToString(); // "C:"
                        if (!string.IsNullOrEmpty(logicalLetter))
                        {
                            partInfo.DriveLetter = logicalLetter;
                            string cleanLetter = logicalLetter.TrimEnd('\\', ':');
                            if (driveInfoMap.TryGetValue(cleanLetter, out var drive) && drive.IsReady)
                            {
                                partInfo.VolumeLabel = drive.VolumeLabel;
                                partInfo.FileSystem = drive.DriveFormat;
                                partInfo.FreeSpaceBytes = (ulong)drive.TotalFreeSpace;
                            }
                        }
                    }

                    disk.Partitions.Add(partInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WMI Partition Warning]: {ex.Message}");
            }
        }
    }
}
