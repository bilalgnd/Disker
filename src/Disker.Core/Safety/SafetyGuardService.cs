using System;
using System.Collections.ObjectModel;
using Disker.Core.Models;

namespace Disker.Core.Safety
{
    public class SafetyGuardService
    {
        public ObservableCollection<string> ProtectedLabels { get; } = new();

        public (bool isProtected, string reason) EvaluateDiskProtection(PhysicalDiskInfo disk)
        {
            // Kullanıcı dilediği gibi korumayı açıp kapatabilir
            return (disk.IsProtected, disk.ProtectionReason);
        }

        public void EnsureDiskCanBeModified(PhysicalDiskInfo disk, string operationName = "İşlem")
        {
            if (disk.IsProtected)
            {
                throw new InvalidOperationException(
                    $"Disk #{disk.DiskNumber} ({disk.FriendlyName}) şu anda koruma altındadır. '{operationName}' işlemine devam etmek için lütfen diskin korumasını kaldırın.");
            }
        }

        /// <summary>
        /// ISO yazma işlemi için hedef diskin güvenli olup olmadığını doğrular.
        /// Sadece USB medya tipi kabul edilir; dahili HDD/SSD/NVMe korunur.
        /// </summary>
        public (bool isSafe, string reason) IsWriteSafeForIso(PhysicalDiskInfo disk)
        {
            if (!disk.IsUsbDisk)
            {
                return (false, $"'{disk.FriendlyName}' bir dahili disk ({disk.MediaTypeTag}). ISO yazma işlemi yalnızca USB belleklere uygulanabilir.");
            }

            if (disk.IsProtected)
            {
                return (false, $"'{disk.FriendlyName}' koruma altında. ISO yazmak için önce korumayı kaldırın.");
            }

            return (true, string.Empty);
        }
    }
}
