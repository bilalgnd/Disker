using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Disker.Core.Models
{
    public enum DiskMediaType
    {
        Unknown,
        HDD,
        SSD,
        NVMe,
        SCSI,
        USB
    }

    public enum PartitionStyleType
    {
        RAW,
        MBR,
        GPT
    }

    public partial class PhysicalDiskInfo : ObservableObject
    {
        [ObservableProperty] private int _diskNumber;
        [ObservableProperty] private string _friendlyName = string.Empty;
        [ObservableProperty] private string _serialNumber = string.Empty;
        [ObservableProperty] private string _busType = string.Empty;
        [ObservableProperty] private DiskMediaType _mediaType = DiskMediaType.Unknown;
        [ObservableProperty] private ulong _sizeBytes;
        [ObservableProperty] private PartitionStyleType _partitionStyle = PartitionStyleType.GPT;
        [ObservableProperty] private string _healthStatus = "Healthy";
        [ObservableProperty] private int? _temperatureCelsius;

        // Canlı Kaydırma ve Sürükleme Animasyonu Özellikleri
        [ObservableProperty] private double _dragOffsetY = 0;
        [ObservableProperty] private bool _isDragging = false;
        [ObservableProperty] private int _zIndex = 0;
        [ObservableProperty] private double _cardScale = 1.0;
        [ObservableProperty] private double _cardOpacity = 1.0;

        // Diskin Panel ve Tema Rengi (Hex String)
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CardBgColor))]
        [NotifyPropertyChangedFor(nameof(CardBorderColor))]
        [NotifyPropertyChangedFor(nameof(CardSubRowBgColor))]
        [NotifyPropertyChangedFor(nameof(CardSubRowBorderColor))]
        private string _themeColor = "#10B981";

        public string CardBgColor => ThemeColor.ToUpperInvariant() switch
        {
            "#10B981" => "#73072016",
            "#8B5CF6" => "#731A0F33",
            "#06B6D4" => "#73052028",
            "#F59E0B" => "#732B1A05",
            "#EC4899" => "#732C0B1E",
            "#3B82F6" => "#730B1A38",
            "#EF4444" => "#732E0E12",
            "#84CC16" => "#73182808",
            "#F97316" => "#732E1406",
            "#6366F1" => "#73141438",
            _ => "#73151D2A"
        };

        public string CardBorderColor => ThemeColor.ToUpperInvariant() switch
        {
            "#10B981" => "#6610B981",
            "#8B5CF6" => "#668B5CF6",
            "#06B6D4" => "#6606B6D4",
            "#F59E0B" => "#66F59E0B",
            "#EC4899" => "#66EC4899",
            "#3B82F6" => "#663B82F6",
            "#EF4444" => "#66EF4444",
            "#84CC16" => "#6684CC16",
            "#F97316" => "#66F97316",
            "#6366F1" => "#666366F1",
            _ => "#6638BDF8"
        };

        public string CardSubRowBgColor => ThemeColor.ToUpperInvariant() switch
        {
            "#10B981" => "#4D03120C",
            "#8B5CF6" => "#4D0E071F",
            "#06B6D4" => "#4D031218",
            "#F59E0B" => "#4D180E02",
            "#EC4899" => "#4D1A0511",
            "#3B82F6" => "#4D050E21",
            "#EF4444" => "#4D1A0609",
            "#84CC16" => "#4D0D1704",
            "#F97316" => "#4D1A0A02",
            "#6366F1" => "#4D0A0A21",
            _ => "#4D0F1723"
        };

        public string CardSubRowBorderColor => ThemeColor.ToUpperInvariant() switch
        {
            "#10B981" => "#4010B981",
            "#8B5CF6" => "#408B5CF6",
            "#06B6D4" => "#4006B6D4",
            "#F59E0B" => "#40F59E0B",
            "#EC4899" => "#40EC4899",
            "#3B82F6" => "#403B82F6",
            "#EF4444" => "#40EF4444",
            "#84CC16" => "#4084CC16",
            "#F97316" => "#40F97316",
            "#6366F1" => "#406366F1",
            _ => "#4038BDF8"
        };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProtectionBadgeText))]
        [NotifyPropertyChangedFor(nameof(ProtectionBgColor))]
        [NotifyPropertyChangedFor(nameof(ProtectionBorderColor))]
        [NotifyPropertyChangedFor(nameof(ProtectionTextColor))]
        [NotifyPropertyChangedFor(nameof(IsResettable))]
        private bool _isProtected = false;

        [ObservableProperty] private string _protectionReason = string.Empty;
        [ObservableProperty] private bool _isOnline = true;
        [ObservableProperty] private bool _isReadOnly = false;

        public ObservableCollection<PartitionInfo> Partitions { get; } = new();

        public string CapacityFormatted => FormatBytes(SizeBytes);
        public string Subtitle => $"Disk #{DiskNumber} • {CapacityFormatted}";
        
        public bool IsResettable => !IsProtected;
        public string ProtectionBadgeText => IsProtected ? "🛡️ Koruma Aktif" : "🔓 Koruma Kapalı";
        public string ProtectionBgColor => IsProtected ? "#78350F" : "#1E293B";
        public string ProtectionBorderColor => IsProtected ? "#FACC15" : "#334155";
        public string ProtectionTextColor => IsProtected ? "#FEF08A" : "#94A3B8";

        // Akıllı Donanım Tespiti
        public string MediaTypeTag
        {
            get
            {
                string name = (FriendlyName ?? string.Empty).ToUpperInvariant();
                string bus = (BusType ?? string.Empty).ToUpperInvariant();

                if (MediaType == DiskMediaType.NVMe || bus.Contains("NVME") || 
                    name.Contains("NVME") || name.Contains("SNV") || name.Contains("PCIE") || name.Contains("M.2"))
                {
                    return "M.2 NVMe";
                }

                if (MediaType == DiskMediaType.SSD || name.Contains("SSD") || name.Contains("WDS") || 
                    name.Contains("FURY") || name.Contains("EVO") || name.Contains("PRO") || name.Contains("SANDISK") || name.Contains("CRUCIAL"))
                {
                    return "SATA SSD";
                }

                if (MediaType == DiskMediaType.HDD || name.StartsWith("ST") || name.Contains("BARRACUDA") || 
                    name.Contains("TOSHIBA") || name.Contains("HITACHI") || name.Contains("HARDDRIVE") || (name.Contains("WD") && !name.Contains("WDS")))
                {
                    return "HDD";
                }

                if (bus.Contains("USB")) return "USB";
                return "SATA";
            }
        }

        public string MediaTypeColor => MediaTypeTag switch
        {
            "M.2 NVMe" => "#A855F7",
            "SATA SSD" => "#3B82F6",
            "HDD" => "#F59E0B",
            "USB" => "#06B6D4",
            _ => "#64748B"
        };

        /// <summary>USB bellek ise true — sadece USB disklerde ISO yazma butonu göstermek için kullanılır.</summary>
        public bool IsUsbDisk => MediaTypeTag == "USB";

        public string PrimaryVolumeTitle
        {
            get
            {
                // Önce harfi olan ana veri bölümünü bul (C:, D:, E: vb.)
                var dataPart = Partitions.FirstOrDefault(p => !p.IsSystemPartition && (!string.IsNullOrWhiteSpace(p.VolumeLabel) || !string.IsNullOrWhiteSpace(p.DriveLetter)));
                if (dataPart != null)
                {
                    if (!string.IsNullOrWhiteSpace(dataPart.VolumeLabel) && !string.IsNullOrWhiteSpace(dataPart.DriveLetter))
                        return $"{dataPart.VolumeLabel} ({dataPart.DriveLetter})";
                    if (!string.IsNullOrWhiteSpace(dataPart.VolumeLabel))
                        return dataPart.VolumeLabel;
                    if (!string.IsNullOrWhiteSpace(dataPart.DriveLetter))
                        return $"Yerel Disk ({dataPart.DriveLetter})";
                }

                var anyNamed = Partitions.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.VolumeLabel) || !string.IsNullOrWhiteSpace(p.DriveLetter));
                if (anyNamed != null)
                {
                    if (!string.IsNullOrWhiteSpace(anyNamed.VolumeLabel) && !string.IsNullOrWhiteSpace(anyNamed.DriveLetter))
                        return $"{anyNamed.VolumeLabel} ({anyNamed.DriveLetter})";
                    if (!string.IsNullOrWhiteSpace(anyNamed.VolumeLabel))
                        return anyNamed.VolumeLabel;
                    if (!string.IsNullOrWhiteSpace(anyNamed.DriveLetter))
                        return $"Yerel Disk ({anyNamed.DriveLetter})";
                }
                return $"Disk #{DiskNumber}";
            }
        }

        public ulong TotalUsedBytes => (ulong)Partitions.Sum(p => (long)p.UsedSpaceBytes);
        public ulong TotalFreeBytes => SizeBytes > TotalUsedBytes ? SizeBytes - TotalUsedBytes : 0;
        public double TotalUsedPercentage => SizeBytes > 0 ? ((double)TotalUsedBytes / SizeBytes) * 100.0 : 0.0;
        public double TotalFreePercentage => SizeBytes > 0 ? ((double)TotalFreeBytes / SizeBytes) * 100.0 : 0.0;

        public string DiskUsageSummary => Partitions.Count > 0 
            ? $"Dolu: {FormatBytes(TotalUsedBytes)} (%{TotalUsedPercentage:F1})  •  Boş: {FormatBytes(TotalFreeBytes)} (%{TotalFreePercentage:F1})"
            : "Bölüm Yok / Ham Alan";

        public static string FormatBytes(ulong bytes)
        {
            if (bytes >= 1024UL * 1024 * 1024 * 1024)
                return $"{(double)bytes / (1024UL * 1024 * 1024 * 1024):F2} TB";
            if (bytes >= 1024UL * 1024 * 1024)
                return $"{(double)bytes / (1024UL * 1024 * 1024):F2} GB";
            if (bytes >= 1024UL * 1024)
                return $"{(double)bytes / (1024UL * 1024):F2} MB";
            if (bytes >= 1024UL)
                return $"{(double)bytes / 1024UL:F2} KB";
            return $"{bytes} B";
        }
    }

    public partial class PartitionInfo : ObservableObject
    {
        [ObservableProperty] private int _diskNumber;
        [ObservableProperty] private uint _partitionNumber;
        [ObservableProperty] private string _driveLetter = string.Empty;
        [ObservableProperty] private string _volumeLabel = string.Empty;
        [ObservableProperty] private string _fileSystem = "NTFS";
        [ObservableProperty] private ulong _sizeBytes;
        [ObservableProperty] private ulong _freeSpaceBytes;
        [ObservableProperty] private string _partitionType = "Basic";
        [ObservableProperty] private bool _isSystem;
        [ObservableProperty] private bool _isBoot;
        [ObservableProperty] private bool _isProtected;
        [ObservableProperty] private string _protectionReason = string.Empty;
        [ObservableProperty] private bool _isVisible = true;

        [ObservableProperty] private string _partitionColor = "#3B82F6";

        public bool IsSystemPartition
        {
            get
            {
                // 1. Sürücü harfi olmayan tüm bölümler (MSR, EFI, gizli kurtarma vb.) daima sistem bölümüdür
                if (string.IsNullOrWhiteSpace(DriveLetter)) return true;

                string label = (VolumeLabel ?? string.Empty).ToUpperInvariant();
                if (label.Contains("SYSTEM RESERVED") || label.Contains("SİSTEM AYRILDI") || 
                    label.Contains("RECOVERY") || label.Contains("KURTARMA") ||
                    label.Contains("EFI") || label.Contains("UEFI_NTFS"))
                    return true;

                string type = (PartitionType ?? string.Empty).ToUpperInvariant();
                if (type.Contains("MSR") || type.Contains("RECOVERY") || type.Contains("RESERVED"))
                    return true;

                // 2. USB bellek üzerindeki küçük EFI/UEFI bootloader bölümleri (<= 512MB FAT16/FAT32)
                // (Ana Windows C: sürücüsü hariç)
                if (SizeBytes <= 512UL * 1024 * 1024 && (FileSystem.Equals("FAT32", StringComparison.OrdinalIgnoreCase) || 
                                                         FileSystem.Equals("FAT", StringComparison.OrdinalIgnoreCase) || 
                                                         FileSystem.Equals("FAT16", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!DriveLetter.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
        }

        public ulong UsedSpaceBytes => SizeBytes > FreeSpaceBytes ? SizeBytes - FreeSpaceBytes : 0;
        public double UsedPercentage => SizeBytes > 0 ? ((double)UsedSpaceBytes / SizeBytes) * 100.0 : 0.0;
        public double FreePercentage => SizeBytes > 0 ? ((double)FreeSpaceBytes / SizeBytes) * 100.0 : 0.0;

        public string SizeFormatted => PhysicalDiskInfo.FormatBytes(SizeBytes);
        public string UsedSpaceFormatted => PhysicalDiskInfo.FormatBytes(UsedSpaceBytes);
        public string FreeSpaceFormatted => PhysicalDiskInfo.FormatBytes(FreeSpaceBytes);

        public string UsageFullSummary => $"Dolu: {UsedSpaceFormatted} (%{UsedPercentage:F1})  •  Boş: {FreeSpaceFormatted} (%{FreePercentage:F1})";
        public string UsageBadge => $"%{UsedPercentage:F0} Dolu";

        public string DisplayName => !string.IsNullOrWhiteSpace(VolumeLabel) 
            ? (!string.IsNullOrWhiteSpace(DriveLetter) ? $"{VolumeLabel} ({DriveLetter})" : VolumeLabel)
            : (!string.IsNullOrWhiteSpace(DriveLetter) ? $"Yerel Disk ({DriveLetter})" : $"{PartitionType} #{PartitionNumber}");
    }

    public class SmartAttribute
    {
        public byte Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte CurrentValue { get; set; }
        public byte WorstValue { get; set; }
        public byte ThresholdValue { get; set; }
        public byte Threshold { get => ThresholdValue; set => ThresholdValue = value; }
        public ulong RawValue { get; set; }
        public bool IsPassing { get; set; } = true;
        public string StatusDescription => IsPassing ? "İyi" : "Riskli";
    }

    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }

        public static OperationResult Ok(string message = "İşlem başarılı.") => new() { Success = true, Message = message };
        public static OperationResult Fail(string message, Exception? ex = null) => new() { Success = false, Message = message, Exception = ex };
    }
}
