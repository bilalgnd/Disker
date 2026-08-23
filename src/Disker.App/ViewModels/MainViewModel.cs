using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Disker.Core.Models;
using Disker.Core.Operations;
using Disker.Core.Safety;
using Disker.Core.Wmi;

namespace Disker.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SafetyGuardService _safetyGuard;
        private readonly WmiStorageProvider _wmiProvider;
        private readonly DiskOperationService _operationService;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = "Hazır";
        [ObservableProperty] private PhysicalDiskInfo? _selectedDisk;
        [ObservableProperty] private int _totalDisksCount;
        [ObservableProperty] private int _protectedDisksCount;

        public ObservableCollection<PhysicalDiskInfo> Disks { get; } = new();
        public ObservableCollection<string> ProtectedLabelsList { get; } = new();

        public MainViewModel()
        {
            _safetyGuard = new SafetyGuardService();
            _wmiProvider = new WmiStorageProvider(_safetyGuard);
            _operationService = new DiskOperationService(_safetyGuard);

            UpdateProtectedLabelsList();
        }

        public void UpdateProtectedLabelsList()
        {
            ProtectedLabelsList.Clear();
            foreach (var label in _safetyGuard.ProtectedLabels)
            {
                ProtectedLabelsList.Add(label);
            }
        }

        [RelayCommand]
        public async Task LoadDisksAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            StatusMessage = "Sistem diskleri ve S.M.A.R.T. verileri okunuyor...";

            try
            {
                var diskList = await Task.Run(() => _wmiProvider.GetDisks());

                Disks.Clear();
                foreach (var disk in diskList)
                {
                    Disks.Add(disk);
                }

                TotalDisksCount = Disks.Count;
                ProtectedDisksCount = Disks.Count(d => d.IsProtected);

                if (SelectedDisk != null)
                {
                    SelectedDisk = Disks.FirstOrDefault(d => d.DiskNumber == SelectedDisk.DiskNumber) ?? Disks.FirstOrDefault();
                }
                else
                {
                    SelectedDisk = Disks.FirstOrDefault();
                }

                StatusMessage = $"Toplam {TotalDisksCount} disk tespit edildi ({ProtectedDisksCount} korumalı).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Diskler okunurken hata oluştu: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<OperationResult> ResetDiskToSingleVolumeAsync(PhysicalDiskInfo disk, string volumeLabel, string fileSystem = "NTFS")
        {
            IsLoading = true;
            StatusMessage = $"Disk #{disk.DiskNumber} sıfırlanıyor...";

            var progress = new Progress<string>(msg =>
            {
                StatusMessage = msg;
            });

            try
            {
                var result = await _operationService.ResetAndCreateSingleVolumeAsync(disk, volumeLabel, fileSystem, progress);
                await LoadDisksAsync();
                return result;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Hata: {ex.Message}";
                return OperationResult.Fail(ex.Message, ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<OperationResult> FormatPartitionAsync(PartitionInfo partition, string newLabel, string fileSystem = "NTFS")
        {
            IsLoading = true;
            StatusMessage = $"Birim ({partition.DisplayName}) biçimlendiriliyor...";

            try
            {
                var result = await _operationService.FormatPartitionAsync(partition, newLabel, fileSystem);
                await LoadDisksAsync();
                return result;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Hata: {ex.Message}";
                return OperationResult.Fail(ex.Message, ex);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
