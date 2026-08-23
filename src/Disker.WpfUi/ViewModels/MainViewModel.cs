using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Disker.App.Helpers;
using Disker.Core.Diagnostics;
using Disker.Core.IsoWriter;
using Disker.Core.Models;
using Disker.Core.Operations;
using Disker.Core.Safety;
using Disker.Core.Settings;
using Disker.Core.Wmi;

namespace Disker.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SafetyGuardService _safetyGuard;
        private readonly WmiStorageProvider _wmiProvider;
        private readonly DiskOperationService _operationService;
        private readonly DiskHealthAnalysisService _healthService;
        private readonly UserSettingsService _settingsService;
        private readonly IsoWriterService _isoWriterService;

        public Loc L => Loc.Instance;

        public string AppVersion => $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";
        public string AppTitleWithVersion => $"Disker {AppVersion}";

        [ObservableProperty] private bool _isLoading = true;
        [ObservableProperty] private string _statusMessage = "Hazır";
        [ObservableProperty] private PhysicalDiskInfo? _selectedDisk;
        [ObservableProperty] private DiskHealthReport? _selectedHealthReport;
        [ObservableProperty] private int _totalDisksCount;
        [ObservableProperty] private int _protectedDisksCount;
        [ObservableProperty] private int _resettableDisksCount;
        [ObservableProperty] private bool _showSystemPartitions = true;

        // --- SIFIRLAMA VE BİÇİMLENDİRME MODAL EKRANI STATE'İ ---
        [ObservableProperty] private bool _isResetModalOpen = false;
        [ObservableProperty] private PhysicalDiskInfo? _resetTargetDisk;
        [ObservableProperty] private string _resetVolumeLabel = "Depolama";
        [ObservableProperty] private string _resetPartitionScheme = "GPT"; // "GPT" veya "MBR"
        [ObservableProperty] private string _resetFileSystem = "NTFS";     // "NTFS", "exFAT", "FAT32"
        [ObservableProperty] private string _resetDriveLetter = "Otomatik"; // "Otomatik" veya "D:", "E:", "F:", "X:", "Z:"
        [ObservableProperty] private bool _resetIsRunning = false;
        [ObservableProperty] private string _resetProgressText = string.Empty;
        [ObservableProperty] private string _resetErrorMessage = string.Empty;
        [ObservableProperty] private string _resetSuccessMessage = string.Empty;

        // --- TEK BÖLÜM SİLME VE BİRLEŞTİRME MODAL STATE'İ ---
        [ObservableProperty] private bool _isDeletePartitionModalOpen = false;
        [ObservableProperty] private PhysicalDiskInfo? _deletePartitionTargetDisk;
        [ObservableProperty] private PartitionInfo? _deletePartitionTarget;
        [ObservableProperty] private bool _deletePartitionIsRunning = false;
        [ObservableProperty] private string _deletePartitionProgressText = string.Empty;
        [ObservableProperty] private string _deletePartitionErrorMessage = string.Empty;
        [ObservableProperty] private string _deletePartitionSuccessMessage = string.Empty;

        // --- USB FLASH (ISO YAZMA) TAB STATE'İ ---
        [ObservableProperty] private bool _isIsoWriteModalOpen = false;
        [ObservableProperty] private string _selectedIsoPath = string.Empty;
        [ObservableProperty] private string _selectedIsoDisplayName = string.Empty;
        [ObservableProperty] private string _selectedIsoSizeText = string.Empty;
        [ObservableProperty] private string _selectedIsoType = string.Empty;
        [ObservableProperty] private bool _hasSelectedIso = false;
        [ObservableProperty] private PhysicalDiskInfo? _selectedUsbTarget;
        [ObservableProperty] private bool _isIsoWriting = false;
        [ObservableProperty] private double _isoWriteProgressPercent = 0;
        [ObservableProperty] private string _isoWriteStatusText = "HAZIR";
        [ObservableProperty] private string _isoWriteSpeedText = string.Empty;
        [ObservableProperty] private string _isoWriteErrorMessage = string.Empty;
        [ObservableProperty] private string _isoWriteSuccessMessage = string.Empty;
        [ObservableProperty] private bool _hasUsbDrives = false;
        [ObservableProperty] private string _isoWriteDeviceCountText = "0 aygıt bulundu";

        // Rufus Gelişmiş Ayarları & Format Seçenekleri
        [ObservableProperty] private string _selectedBootOption = "Disk veya ISO yansıması (Lütfen seçin)";
        [ObservableProperty] private string _selectedImageOption = "Standart Windows yüklemesi";
        [ObservableProperty] private bool _isImageOptionVisible = true;

        [ObservableProperty] private string _isoPartitionScheme = "GPT"; // "GPT", "MBR"
        [ObservableProperty] private string _isoTargetSystem = "UEFI (CSM yok)"; // "UEFI (CSM yok)", "BIOS (veya UEFI-CSM)"
        [ObservableProperty] private string _isoVolumeLabel = string.Empty;
        [ObservableProperty] private string _isoFileSystem = "NTFS"; // "NTFS", "FAT32 (Varsayılan)", "exFAT"
        [ObservableProperty] private string _isoClusterSize = "4096 bayt (Varsayılan)";
        [ObservableProperty] private bool _isoQuickFormat = true;
        [ObservableProperty] private bool _isoCreateExtendedLabel = true;
        [ObservableProperty] private bool _isoCheckBadBlocks = false;
        [ObservableProperty] private string _isoBadBlocksPasses = "1 geçiş";
        [ObservableProperty] private bool _showAdvancedDriveProperties = false;
        [ObservableProperty] private bool _showAdvancedFormatOptions = false;
        [ObservableProperty] private string _isoChecksumInfo = string.Empty;

        public ObservableCollection<string> BootSelectionOptions { get; } = new()
        {
            "Önyüklenebilir değil",
            "Disk veya ISO yansıması (Lütfen seçin)",
            "FreeDOS",
            "MS-DOS"
        };

        public ObservableCollection<string> ImageOptionList { get; } = new()
        {
            "Standart Windows yüklemesi",
            "Windows To Go"
        };

        public ObservableCollection<string> IsoPartitionSchemes { get; } = new() { "GPT", "MBR" };
        public ObservableCollection<string> IsoTargetSystems { get; } = new() { "UEFI (CSM yok)", "BIOS (veya UEFI-CSM)" };
        public ObservableCollection<string> IsoFileSystems { get; } = new() { "NTFS", "FAT32 (Varsayılan)", "exFAT" };
        public ObservableCollection<string> IsoClusterSizes { get; } = new() { "4096 bayt (Varsayılan)", "8192 bayt", "16 kilobayt", "32 kilobayt", "64 kilobayt" };
        public ObservableCollection<string> IsoBadBlockPassList { get; } = new() { "1 geçiş", "2 geçiş", "3 geçiş", "4 geçiş" };

        private CancellationTokenSource? _isoWriteCts;

        public ObservableCollection<string> AvailableDriveLetters { get; } = new();
        public ObservableCollection<PhysicalDiskInfo> Disks { get; } = new();
        public ObservableCollection<PhysicalDiskInfo> UsbDrives { get; } = new();
        public ObservableCollection<DiskHealthReport> HealthReports { get; } = new();
        public ObservableCollection<string> ProtectedLabelsList { get; } = new();

        public MainViewModel()
        {
            _safetyGuard = new SafetyGuardService();
            _wmiProvider = new WmiStorageProvider(_safetyGuard);
            _operationService = new DiskOperationService(_safetyGuard);
            _healthService = new DiskHealthAnalysisService();
            _settingsService = new UserSettingsService();
            _isoWriterService = new IsoWriterService();

            var settings = _settingsService.LoadSettings();
            Loc.Instance.CurrentLanguage = settings.Language ?? "tr";

            UpdateProtectedLabelsList();
        }

        public void SetLanguage(string lang)
        {
            Loc.Instance.CurrentLanguage = lang;
            SaveUserSettings();
            UpdateProtectedLabelsList();
            StatusMessage = Loc.Instance.IsTr
                ? $"Toplam {TotalDisksCount} disk tespit edildi ({ProtectedDisksCount} korumalı, {ResettableDisksCount} sıfırlanabilir)."
                : $"Total {TotalDisksCount} disks detected ({ProtectedDisksCount} protected, {ResettableDisksCount} resettable).";
        }

        public void PopulateAvailableDriveLetters(PhysicalDiskInfo disk)
        {
            AvailableDriveLetters.Clear();
            AvailableDriveLetters.Add(Loc.Instance.IsTr ? "Otomatik" : "Auto");

            var usedLetters = new HashSet<char>();
            try
            {
                var drives = DriveInfo.GetDrives();
                foreach (var d in drives)
                {
                    string name = d.Name.ToUpperInvariant();
                    if (!string.IsNullOrEmpty(name))
                    {
                        usedLetters.Add(name[0]);
                    }
                }
            }
            catch { }

            var currentDiskLetters = disk.Partitions
                .Where(p => !string.IsNullOrWhiteSpace(p.DriveLetter))
                .Select(p => p.DriveLetter.Trim().ToUpperInvariant()[0])
                .ToList();

            foreach (var l in currentDiskLetters)
            {
                usedLetters.Remove(l);
            }

            for (char c = 'D'; c <= 'Z'; c++)
            {
                if (!usedLetters.Contains(c))
                {
                    AvailableDriveLetters.Add($"{c}:");
                }
            }

            if (currentDiskLetters.Count > 0)
            {
                string curr = $"{currentDiskLetters[0]}:";
                ResetDriveLetter = AvailableDriveLetters.Contains(curr) ? curr : (Loc.Instance.IsTr ? "Otomatik" : "Auto");
            }
            else
            {
                ResetDriveLetter = Loc.Instance.IsTr ? "Otomatik" : "Auto";
            }
        }

        public void OpenResetModal(PhysicalDiskInfo disk)
        {
            if (disk == null) return;
            ResetTargetDisk = disk;
            ResetVolumeLabel = !string.IsNullOrWhiteSpace(disk.PrimaryVolumeTitle) && !disk.PrimaryVolumeTitle.StartsWith("Disk #")
                ? disk.PrimaryVolumeTitle.Split('(')[0].Trim()
                : (disk.SizeBytes > 200UL * 1024 * 1024 * 1024 ? "Depolama500" : "Depolama120");
            ResetPartitionScheme = "GPT";
            ResetFileSystem = "NTFS";
            ResetIsRunning = false;
            ResetProgressText = string.Empty;
            ResetErrorMessage = string.Empty;
            ResetSuccessMessage = string.Empty;

            PopulateAvailableDriveLetters(disk);
            IsResetModalOpen = true;
        }

        public void CloseResetModal()
        {
            if (ResetIsRunning) return;
            IsResetModalOpen = false;
            ResetTargetDisk = null;
        }

        public async Task ExecuteResetModalAsync()
        {
            if (ResetTargetDisk == null || ResetIsRunning) return;

            ResetIsRunning = true;
            ResetErrorMessage = string.Empty;
            ResetSuccessMessage = string.Empty;
            ResetProgressText = Loc.Instance.IsTr ? "Sıfırlama işlemi başlatılıyor..." : "Starting disk reset and format...";

            var progress = new Progress<string>(msg =>
            {
                ResetProgressText = msg;
                StatusMessage = msg;
            });

            try
            {
                var result = await _operationService.ResetAndCreateSingleVolumeAsync(
                    ResetTargetDisk,
                    ResetVolumeLabel,
                    ResetFileSystem,
                    ResetPartitionScheme,
                    ResetDriveLetter,
                    true,
                    progress);

                if (result.Success)
                {
                    ResetSuccessMessage = result.Message;
                    await LoadDisksAsync();
                }
                else
                {
                    ResetErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                ResetErrorMessage = $"{ (Loc.Instance.IsTr ? "Hata" : "Error") }: {ex.Message}";
            }
            finally
            {
                ResetIsRunning = false;
            }
        }

        // --- TEK BÖLÜM SİLME METOTLARI ---
        public void OpenDeletePartitionModal(PhysicalDiskInfo disk, PartitionInfo part)
        {
            if (disk == null || part == null) return;
            DeletePartitionTargetDisk = disk;
            DeletePartitionTarget = part;
            DeletePartitionIsRunning = false;
            DeletePartitionProgressText = string.Empty;
            DeletePartitionErrorMessage = string.Empty;
            DeletePartitionSuccessMessage = string.Empty;
            IsDeletePartitionModalOpen = true;
        }

        public void CloseDeletePartitionModal()
        {
            if (DeletePartitionIsRunning) return;
            IsDeletePartitionModalOpen = false;
            DeletePartitionTargetDisk = null;
            DeletePartitionTarget = null;
        }

        public async Task ExecuteDeletePartitionModalAsync()
        {
            if (DeletePartitionTargetDisk == null || DeletePartitionTarget == null || DeletePartitionIsRunning) return;

            DeletePartitionIsRunning = true;
            DeletePartitionErrorMessage = string.Empty;
            DeletePartitionSuccessMessage = string.Empty;
            DeletePartitionProgressText = Loc.Instance.IsTr ? "Bölüm siliniyor..." : "Deleting partition...";

            var progress = new Progress<string>(msg =>
            {
                DeletePartitionProgressText = msg;
                StatusMessage = msg;
            });

            try
            {
                var result = await _operationService.DeletePartitionAndExtendAdjacentAsync(
                    DeletePartitionTargetDisk.DiskNumber,
                    DeletePartitionTarget.PartitionNumber,
                    DeletePartitionTargetDisk,
                    progress);

                if (result.Success)
                {
                    DeletePartitionSuccessMessage = result.Message;
                    await LoadDisksAsync();
                }
                else
                {
                    DeletePartitionErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                DeletePartitionErrorMessage = $"Hata: {ex.Message}";
            }
            finally
            {
                DeletePartitionIsRunning = false;
            }
        }

        public void ToggleSystemPartitionsVisibility()
        {
            ShowSystemPartitions = !ShowSystemPartitions;
            ApplySystemPartitionsVisibility();
            SaveUserSettings();
        }

        public void ApplySystemPartitionsVisibility()
        {
            foreach (var d in Disks)
            {
                foreach (var p in d.Partitions)
                {
                    p.IsVisible = ShowSystemPartitions || !p.IsSystemPartition;
                }
            }
        }

        public void SaveUserSettings()
        {
            var settings = new UserSettings
            {
                Language = Loc.Instance.CurrentLanguage,
                ShowSystemPartitions = ShowSystemPartitions
            };

            foreach (var d in Disks)
            {
                string id = UserSettingsService.GetDiskId(d);
                settings.DiskOrder.Add(id);
                settings.DiskColors[id] = d.ThemeColor;

                if (d.IsProtected)
                {
                    settings.ProtectedDiskIds.Add(id);
                }
            }
            _settingsService.SaveSettings(settings);
        }

        public void UpdateProtectedLabelsList()
        {
            ProtectedLabelsList.Clear();
            foreach (var d in Disks.Where(x => x.IsProtected))
            {
                ProtectedLabelsList.Add($"{d.PrimaryVolumeTitle} (Disk #{d.DiskNumber})");
            }
        }

        public void ToggleProtection(PhysicalDiskInfo disk)
        {
            if (disk == null) return;
            disk.IsProtected = !disk.IsProtected;
            ProtectedDisksCount = Disks.Count(d => d.IsProtected);
            ResettableDisksCount = Disks.Count(d => !d.IsProtected);
            UpdateProtectedLabelsList();
            SaveUserSettings();
        }

        public void ReorderDisk(PhysicalDiskInfo sourceDisk, PhysicalDiskInfo targetDisk)
        {
            if (sourceDisk == null || targetDisk == null || sourceDisk == targetDisk) return;

            int oldIndex = Disks.IndexOf(sourceDisk);
            int newIndex = Disks.IndexOf(targetDisk);

            if (oldIndex >= 0 && newIndex >= 0)
            {
                Disks.Move(oldIndex, newIndex);
                SaveUserSettings();
            }
        }

        public void MoveDiskUp(PhysicalDiskInfo disk)
        {
            if (disk == null) return;
            int idx = Disks.IndexOf(disk);
            if (idx > 0)
            {
                Disks.Move(idx, idx - 1);
                SaveUserSettings();
            }
        }

        public void MoveDiskDown(PhysicalDiskInfo disk)
        {
            if (disk == null) return;
            int idx = Disks.IndexOf(disk);
            if (idx >= 0 && idx < Disks.Count - 1)
            {
                Disks.Move(idx, idx + 1);
                SaveUserSettings();
            }
        }

        public void SetDiskThemeColor(PhysicalDiskInfo disk, string hexColor)
        {
            if (disk == null) return;
            disk.ThemeColor = hexColor;
            foreach (var p in disk.Partitions)
            {
                p.PartitionColor = hexColor;
            }

            var report = HealthReports.FirstOrDefault(r => r.DiskNumber == disk.DiskNumber);
            if (report != null)
            {
                report.PillColor = hexColor;
            }

            SaveUserSettings();
        }

        [RelayCommand]
        public void SelectHealthReport(DiskHealthReport report)
        {
            if (report == null) return;

            foreach (var r in HealthReports)
            {
                r.IsSelected = (r.DiskNumber == report.DiskNumber);
            }

            SelectedHealthReport = report;
        }

        private bool _isCurrentlyScanning = false;

        [RelayCommand]
        public async Task LoadDisksAsync()
        {
            if (_isCurrentlyScanning) return;

            _isCurrentlyScanning = true;
            IsLoading = true;
            StatusMessage = Loc.Instance.IsTr ? "Sistem diskleri taranıyor..." : "Scanning system disks...";

            try
            {
                var diskList = await Task.Run(() => _wmiProvider.GetDisks());
                var savedSettings = _settingsService.LoadSettings();

                Loc.Instance.CurrentLanguage = savedSettings.Language ?? "tr";

                foreach (var disk in diskList)
                {
                    string id = UserSettingsService.GetDiskId(disk);
                    if (savedSettings.DiskColors.TryGetValue(id, out string? customColor) && !string.IsNullOrWhiteSpace(customColor))
                    {
                        disk.ThemeColor = customColor;
                        foreach (var p in disk.Partitions)
                        {
                            p.PartitionColor = customColor;
                        }
                    }

                    if (savedSettings.ProtectedDiskIds.Contains(id))
                    {
                        disk.IsProtected = true;
                    }
                }

                if (savedSettings.DiskOrder.Count > 0)
                {
                    diskList = diskList
                        .OrderBy(d =>
                        {
                            int idx = savedSettings.DiskOrder.IndexOf(UserSettingsService.GetDiskId(d));
                            return idx >= 0 ? idx : 999;
                        })
                        .ToList();
                }

                Disks.Clear();
                HealthReports.Clear();

                int idx = 0;
                foreach (var disk in diskList)
                {
                    Disks.Add(disk);

                    var report = _healthService.AnalyzeDisk(disk, idx);
                    HealthReports.Add(report);
                    idx++;
                }

                TotalDisksCount = Disks.Count;
                ProtectedDisksCount = Disks.Count(d => d.IsProtected);
                ResettableDisksCount = Disks.Count(d => !d.IsProtected);

                ShowSystemPartitions = savedSettings.ShowSystemPartitions;
                ApplySystemPartitionsVisibility();

                SelectedDisk = Disks.FirstOrDefault();
                
                var firstReport = HealthReports.FirstOrDefault();
                if (firstReport != null)
                {
                    SelectHealthReport(firstReport);
                }

                UpdateProtectedLabelsList();
                RefreshUsbDrives();
                StatusMessage = Loc.Instance.IsTr
                    ? $"Toplam {TotalDisksCount} disk tespit edildi ({ProtectedDisksCount} korumalı, {ResettableDisksCount} sıfırlanabilir)."
                    : $"Total {TotalDisksCount} disks detected ({ProtectedDisksCount} protected, {ResettableDisksCount} resettable).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"{ (Loc.Instance.IsTr ? "Diskler okunurken hata" : "Error reading disks") }: {ex.Message}";
            }
            finally
            {
                _isCurrentlyScanning = false;
                IsLoading = false;
            }
        }
        // --- USB FLASH (ISO YAZMA) METOTLARI ---

        partial void OnIsoPartitionSchemeChanged(string value)
        {
            if (value == "GPT")
            {
                IsoTargetSystem = "UEFI (CSM yok)";
            }
            else if (value == "MBR")
            {
                IsoTargetSystem = "BIOS (veya UEFI-CSM)";
            }
        }

        partial void OnSelectedUsbTargetChanged(PhysicalDiskInfo? value)
        {
            if (value != null && string.IsNullOrWhiteSpace(IsoVolumeLabel))
            {
                IsoVolumeLabel = !string.IsNullOrWhiteSpace(value.PrimaryVolumeTitle)
                    ? value.PrimaryVolumeTitle
                    : "BOOTABLE_USB";
            }
        }

        /// <summary>
        /// ISO dosyası seçer ve bilgilerini ViewModel'e yükler.
        /// Bu metod code-behind tarafından Microsoft.Win32.OpenFileDialog ile çağrılır.
        /// </summary>
        public void SelectIsoFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

            try
            {
                var isoInfo = new IsoImageInfo(filePath);
                SelectedIsoPath = filePath;
                SelectedIsoDisplayName = isoInfo.FileName;
                SelectedIsoSizeText = isoInfo.FileSizeFormatted;
                SelectedIsoType = isoInfo.DetectedType;
                HasSelectedIso = true;
                IsoWriteErrorMessage = string.Empty;
                IsoWriteSuccessMessage = string.Empty;
                IsoWriteStatusText = "HAZIR";
                IsoChecksumInfo = string.Empty;

                // Rufus gibi: Boot Selection listesine ISO adını ekle ve seç
                if (!BootSelectionOptions.Contains(isoInfo.FileName))
                {
                    BootSelectionOptions.Insert(1, isoInfo.FileName);
                }
                SelectedBootOption = isoInfo.FileName;

                // Windows ISO ise Image Option (Standart Windows / Windows To Go) göster
                IsImageOptionVisible = isoInfo.DetectedType.Contains("Windows") || isoInfo.FileName.ToLowerInvariant().Contains("win");

                // Rufus gibi: Otomatik etiket belirle
                string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                IsoVolumeLabel = nameWithoutExt.Length > 32 ? nameWithoutExt[..32] : nameWithoutExt;
            }
            catch (Exception ex)
            {
                IsoWriteErrorMessage = $"ISO okunamadı: {ex.Message}";
                HasSelectedIso = false;
            }
        }

        public async Task CalculateIsoChecksumAsync()
        {
            if (!HasSelectedIso || !File.Exists(SelectedIsoPath)) return;

            try
            {
                IsoWriteStatusText = "Sağlama toplamı (SHA-256) hesaplanıyor...";
                string hash = await Task.Run(() =>
                {
                    using var sha = System.Security.Cryptography.SHA256.Create();
                    using var stream = File.OpenRead(SelectedIsoPath);
                    byte[] hashBytes = sha.ComputeHash(stream);
                    return Convert.ToHexString(hashBytes).ToLowerInvariant();
                });

                IsoChecksumInfo = $"SHA256: {hash}";
                IsoWriteStatusText = "HAZIR";
            }
            catch (Exception ex)
            {
                IsoChecksumInfo = $"Özet hatası: {ex.Message}";
                IsoWriteStatusText = "HAZIR";
            }
        }

        public void OpenIsoWriteModal()
        {
            if (!HasSelectedIso || SelectedUsbTarget == null) return;

            var (isSafe, reason) = _safetyGuard.IsWriteSafeForIso(SelectedUsbTarget);
            if (!isSafe)
            {
                IsoWriteErrorMessage = reason;
                return;
            }

            IsoWriteErrorMessage = string.Empty;
            IsoWriteSuccessMessage = string.Empty;
            IsoWriteProgressPercent = 0;
            IsoWriteStatusText = "HAZIR";
            IsIsoWriteModalOpen = true;
        }

        public void CloseIsoWriteModal()
        {
            if (IsIsoWriting) return;
            IsIsoWriteModalOpen = false;
        }

        public async Task ExecuteIsoWriteAsync()
        {
            if (!HasSelectedIso || SelectedUsbTarget == null || IsIsoWriting) return;

            var (isSafe, reason) = _safetyGuard.IsWriteSafeForIso(SelectedUsbTarget);
            if (!isSafe)
            {
                IsoWriteErrorMessage = reason;
                IsIsoWriteModalOpen = false;
                return;
            }

            IsIsoWriting = true;
            IsIsoWriteModalOpen = false;
            IsoWriteErrorMessage = string.Empty;
            IsoWriteSuccessMessage = string.Empty;
            IsoWriteProgressPercent = 0;
            IsoWriteStatusText = Loc.Instance.IsTr ? "Başlatılıyor..." : "Starting...";

            _isoWriteCts = new CancellationTokenSource();

            var progress = new Progress<IsoWriteProgress>(p =>
            {
                IsoWriteProgressPercent = p.ProgressPercent;
                IsoWriteStatusText = p.StatusText;
                IsoWriteSpeedText = p.SpeedMBps > 0 ? $"{p.SpeedMBps:F1} MB/s" : string.Empty;
                StatusMessage = p.StatusText;
            });

            try
            {
                var isoInfo = new IsoImageInfo(SelectedIsoPath);
                await _isoWriterService.WriteIsoAsync(isoInfo, SelectedUsbTarget, progress, _isoWriteCts.Token);

                IsoWriteSuccessMessage = Loc.Instance.IsTr
                    ? $"✅ {SelectedIsoDisplayName} başarıyla yazıldı!"
                    : $"✅ {SelectedIsoDisplayName} written successfully!";
                IsoWriteProgressPercent = 100;
                IsoWriteSpeedText = string.Empty;
                IsoWriteStatusText = "HAZIR";

                // USB yenile
                await Task.Delay(1500);
                await LoadDisksAsync();
            }
            catch (OperationCanceledException)
            {
                IsoWriteErrorMessage = Loc.Instance.IsTr ? "İşlem iptal edildi." : "Operation cancelled.";
                IsoWriteProgressPercent = 0;
                IsoWriteStatusText = "İPTAL EDİLDİ";
            }
            catch (Exception ex)
            {
                IsoWriteErrorMessage = $"{(Loc.Instance.IsTr ? "Hata" : "Error")}: {ex.Message}";
                IsoWriteStatusText = "HATA";
            }
            finally
            {
                IsIsoWriting = false;
                _isoWriteCts?.Dispose();
                _isoWriteCts = null;
            }
        }

        public void CancelIsoWrite()
        {
            _isoWriteCts?.Cancel();
        }

        /// <summary>
        /// UsbDrives koleksiyonunu sadece USB medya tipiyle günceller.
        /// LoadDisksAsync tarafından çağrılır.
        /// </summary>
        private void RefreshUsbDrives()
        {
            UsbDrives.Clear();
            // IsUsbDisk, BusType string'ini kontrol eder — MediaType enum'u WMI'dan her zaman USB gelmeyebilir
            foreach (var d in Disks.Where(x => x.IsUsbDisk))
            {
                UsbDrives.Add(d);
            }
            HasUsbDrives = UsbDrives.Count > 0;
            IsoWriteDeviceCountText = Loc.Instance.IsTr
                ? $"{UsbDrives.Count} aygıt bulundu"
                : $"{UsbDrives.Count} device(s) found";

            if (SelectedUsbTarget != null && !UsbDrives.Contains(SelectedUsbTarget))
                SelectedUsbTarget = UsbDrives.FirstOrDefault();
            else if (SelectedUsbTarget == null)
                SelectedUsbTarget = UsbDrives.FirstOrDefault();
        }
    }
}
