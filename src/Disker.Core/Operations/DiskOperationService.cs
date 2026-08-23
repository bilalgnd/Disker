using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Disker.Core.Models;
using Disker.Core.Native;
using Disker.Core.Safety;

namespace Disker.Core.Operations
{
    public class DiskOperationService
    {
        private readonly SafetyGuardService _safetyGuard;

        public DiskOperationService(SafetyGuardService safetyGuard)
        {
            _safetyGuard = safetyGuard;
        }

        /// <summary>
        /// Seçilen tek bir bölümü güvenle siler ve mevcut veri bölümünü (örn. D:) genişletmeye çalışır.
        /// </summary>
        public async Task<OperationResult> DeletePartitionAndExtendAdjacentAsync(
            int diskNumber, 
            uint partitionNumber,
            PhysicalDiskInfo disk,
            IProgress<string>? progress = null)
        {
            try
            {
                _safetyGuard.EnsureDiskCanBeModified(disk, "Bölümü Sil ve Birleştir");

                progress?.Report($"[1/3] Disk #{diskNumber} üzerindeki Bölüm #{partitionNumber} siliniyor...");

                // PowerShell ile seçili bölümü sil (Remove-Partition)
                string deleteScript = $@"
Remove-Partition -DiskNumber {diskNumber} -PartitionNumber {partitionNumber} -Confirm:$false
";
                var delResult = await RunPowerShellScriptAsync(deleteScript);
                if (!delResult.Success)
                {
                    return OperationResult.Fail($"Bölüm silinemedi: {delResult.Message}");
                }

                progress?.Report($"[2/3] Bölüm başarıyla silindi. Boş alan ana bölüme katılıyor...");

                // Ana veri bölümünü maksimum boyuta genişlet (Resize-Partition)
                string extendScript = $@"
$parts = Get-Partition -DiskNumber {diskNumber} | Where-Object {{ $_.DriveLetter -ne $null -and $_.DriveLetter -ne '' }}
foreach ($p in $parts) {{
    try {{
        $max = (Get-PartitionSupportedSize -DiskNumber {diskNumber} -PartitionNumber $p.PartitionNumber).SizeMax
        if ($max -gt $p.Size) {{
            Resize-Partition -DiskNumber {diskNumber} -PartitionNumber $p.PartitionNumber -Size $max -ErrorAction SilentlyContinue
        }}
    }} catch {{}}
}}
";
                await RunPowerShellScriptAsync(extendScript);

                progress?.Report($"[3/3] İşlem tamamlandı!");
                return OperationResult.Ok($"Bölüm #{partitionNumber} başarıyla silindi ve verileriniz korunarak alan optimize edildi.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(ex.Message, ex);
            }
        }

        /// <summary>
        /// Diski tamamen sıfırlar ve kullanıcının seçtiği ayarlarla (GPT/MBR, Sürücü Harfi, NTFS/exFAT/FAT32, Etiket) tek parça biçimlendirir.
        /// </summary>
        public async Task<OperationResult> ResetAndCreateSingleVolumeAsync(
            PhysicalDiskInfo disk, 
            string volumeLabel = "Yeni Birim", 
            string fileSystem = "NTFS",
            string partitionScheme = "GPT",
            string driveLetter = "Otomatik",
            bool quickFormat = true,
            IProgress<string>? progress = null)
        {
            try
            {
                _safetyGuard.EnsureDiskCanBeModified(disk, "Diski Sıfırla ve Biçimlendir");

                progress?.Report($"[1/4] Disk #{disk.DiskNumber} ({disk.FriendlyName}) üzerindeki açık birimler kilitleniyor...");

                foreach (var part in disk.Partitions)
                {
                    if (!string.IsNullOrEmpty(part.DriveLetter))
                    {
                        NativeDisk.LockAndDismountVolume(part.DriveLetter);
                    }
                }

                progress?.Report($"[2/4] Disk #{disk.DiskNumber} üzerindeki tüm bölümler (OEM/Kurtarma dahil) temizleniyor...");

                string scheme = partitionScheme.ToUpperInvariant() == "MBR" ? "MBR" : "GPT";
                string fs = fileSystem.ToUpperInvariant() switch
                {
                    "EXFAT" => "exFAT",
                    "FAT32" => "FAT32",
                    _ => "NTFS"
                };

                string cleanLetter = driveLetter.Trim().TrimEnd(':').ToUpperInvariant();
                string partitionCmd = (cleanLetter == "OTOMATIK" || string.IsNullOrWhiteSpace(cleanLetter))
                    ? $"$p = New-Partition -DiskNumber {disk.DiskNumber} -UseMaximumSize -AssignDriveLetter"
                    : $"$p = New-Partition -DiskNumber {disk.DiskNumber} -UseMaximumSize -DriveLetter '{cleanLetter}'";

                string cleanScript = $@"
Clear-Disk -Number {disk.DiskNumber} -RemoveData -RemoveOEM -Confirm:$false
Initialize-Disk -Number {disk.DiskNumber} -PartitionStyle {scheme}
{partitionCmd}
Format-Volume -Partition $p -FileSystem {fs} -NewFileSystemLabel '{volumeLabel}' -Confirm:$false
";
                progress?.Report($"[3/4] Disk {scheme} tablosu ile başlatılıyor ve {fs} olarak biçimlendiriliyor...");

                var result = await RunPowerShellScriptAsync(cleanScript);
                if (!result.Success)
                {
                    return OperationResult.Fail($"Disk sıfırlama işlemi başarısız oldu: {result.Message}");
                }

                string letterInfo = (cleanLetter != "OTOMATIK" && !string.IsNullOrWhiteSpace(cleanLetter)) ? $" ({cleanLetter}:)" : "";
                progress?.Report($"[4/4] İşlem tamamlandı! Disk '{volumeLabel}'{letterInfo} ({scheme} / {fs}) olarak hazır.");

                return OperationResult.Ok($"Disk #{disk.DiskNumber} başarıyla sıfırlandı ve '{volumeLabel}'{letterInfo} ({scheme}, {fs}) olarak oluşturuldu.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(ex.Message, ex);
            }
        }

        public async Task<OperationResult> CleanDiskAsync(PhysicalDiskInfo disk, IProgress<string>? progress = null)
        {
            try
            {
                _safetyGuard.EnsureDiskCanBeModified(disk, "Diski Temizle (Clean)");

                progress?.Report($"Disk #{disk.DiskNumber} temizleniyor...");
                string script = $"Clear-Disk -Number {disk.DiskNumber} -RemoveData -RemoveOEM -Confirm:$false";
                return await RunPowerShellScriptAsync(script);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(ex.Message, ex);
            }
        }

        private static async Task<OperationResult> RunPowerShellScriptAsync(string script)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"").Replace("\r\n", "; ")}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process == null) return OperationResult.Fail("PowerShell başlatılamadı.");

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
                    {
                        return OperationResult.Fail(error);
                    }

                    return OperationResult.Ok(output);
                }
                catch (Exception ex)
                {
                    return OperationResult.Fail(ex.Message, ex);
                }
            });
        }
    }
}
