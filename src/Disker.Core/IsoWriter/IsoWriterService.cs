using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Disker.Core.Models;
using Disker.Core.Native;

namespace Disker.Core.IsoWriter
{
    /// <summary>
    /// ISO → Bootable USB yazma servisi.
    /// Rufus'un format.c / drive.c DD_BUFFER_SIZE = 32MB ham yazma mantığından
    /// esinlenerek tamamen C# / .NET 8 ile yeniden implemente edilmiştir.
    /// GPLv3 lisanslı Rufus kodu (Pete Batard) referans alınmıştır.
    /// </summary>
    public class IsoWriterService
    {
        /// <summary>Buffer boyutu: Rufus'un DD_BUFFER_SIZE = 32MB değeriyle aynı.</summary>
        private const int BufferSize = 32 * 1024 * 1024; // 32 MB

        /// <summary>
        /// ISO dosyasını fiziksel USB sürücüye ham (DD modu) yazar.
        /// </summary>
        /// <param name="iso">Kaynak ISO bilgisi</param>
        /// <param name="targetDisk">Hedef USB PhysicalDiskInfo</param>
        /// <param name="progress">İlerleme callback'i</param>
        /// <param name="ct">İptal token'ı</param>
        public async Task WriteIsoAsync(
            IsoImageInfo iso,
            PhysicalDiskInfo targetDisk,
            IProgress<IsoWriteProgress>? progress = null,
            CancellationToken ct = default)
        {
            // --- Güvenlik Kontrolleri ---
            if (targetDisk.MediaType != DiskMediaType.USB)
                throw new InvalidOperationException("Güvenlik: Hedef sürücü USB medya değil. Sadece USB disklere yazılabilir.");

            if (iso.FileSizeBytes > (long)targetDisk.SizeBytes)
                throw new InvalidOperationException(
                    $"ISO boyutu ({iso.FileSizeFormatted}) USB kapasitesinden ({FormatBytes((long)targetDisk.SizeBytes)}) büyük.");

            progress?.Report(new IsoWriteProgress(0, iso.FileSizeBytes, 0, "[1/4] USB birimler çıkarılıyor..."));

            // --- Adım 1: USB'nin tüm sürücü harflerini çıkar (Dismount) ---
            await Task.Run(() =>
            {
                foreach (var part in targetDisk.Partitions)
                {
                    if (!string.IsNullOrWhiteSpace(part.DriveLetter))
                    {
                        try { NativeDisk.LockAndDismountVolume(part.DriveLetter); }
                        catch { /* Best-effort, continue */ }
                    }
                }
            }, ct);

            ct.ThrowIfCancellationRequested();

            progress?.Report(new IsoWriteProgress(0, iso.FileSizeBytes, 0, "[2/4] Fiziksel disk erişimi açılıyor..."));

            // --- Adım 2: Fiziksel disk handle'ını aç ---
            string physPath = $@"\\.\PhysicalDrive{targetDisk.DiskNumber}";
            using var diskHandle = NativeDisk.CreateFile(
                physPath,
                NativeDisk.GENERIC_READ | NativeDisk.GENERIC_WRITE,
                NativeDisk.FILE_SHARE_READ | NativeDisk.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeDisk.OPEN_EXISTING,
                FILE_FLAG_NO_BUFFERING | FILE_FLAG_WRITE_THROUGH,
                IntPtr.Zero);

            if (diskHandle.IsInvalid)
            {
                int err = Marshal.GetLastWin32Error();
                throw new IOException($"Fiziksel disk açılamadı (Hata kodu: {err}). Uygulamayı Yönetici olarak çalıştırın.");
            }

            // Disk'i kilitle
            DeviceIoControl(diskHandle, NativeDisk.FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);

            ct.ThrowIfCancellationRequested();
            progress?.Report(new IsoWriteProgress(0, iso.FileSizeBytes, 0, "[3/4] ISO yazılıyor..."));

            // --- Adım 3: Ham DD yazma (32MB chunk'larla) ---
            await Task.Run(async () =>
            {
                using var isoStream = new FileStream(iso.FilePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, bufferSize: BufferSize, useAsync: true);

                // SafeFileHandle'den FileStream oluştur
                using var diskStream = new FileStream(diskHandle, FileAccess.Write, bufferSize: BufferSize, isAsync: true);

                var buffer = new byte[BufferSize];
                long totalWritten = 0;
                long totalSize = iso.FileSizeBytes;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                long lastReportedBytes = 0;
                double lastElapsed = 0;

                int bytesRead;
                while ((bytesRead = await isoStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    ct.ThrowIfCancellationRequested();

                    // Yazma boyutunu sektör hizamasına (512 byte) yuvarla
                    int aligned = AlignToSector(bytesRead);
                    await diskStream.WriteAsync(buffer, 0, aligned, ct);

                    totalWritten += bytesRead;

                    // İlerleme raporu (her 256KB'da bir)
                    double elapsed = stopwatch.Elapsed.TotalSeconds;
                    if (elapsed - lastElapsed >= 0.25 || totalWritten == totalSize)
                    {
                        double bytesDelta = totalWritten - lastReportedBytes;
                        double timeDelta = elapsed - lastElapsed;
                        double speedMBps = timeDelta > 0 ? (bytesDelta / 1024.0 / 1024.0) / timeDelta : 0;

                        string statusText = $"[3/4] Yazılıyor: {FormatBytes(totalWritten)} / {FormatBytes(totalSize)}  •  {speedMBps:F1} MB/s";
                        progress?.Report(new IsoWriteProgress(totalWritten, totalSize, speedMBps, statusText));

                        lastReportedBytes = totalWritten;
                        lastElapsed = elapsed;
                    }
                }

                await diskStream.FlushAsync(ct);
                stopwatch.Stop();

            }, ct);

            ct.ThrowIfCancellationRequested();

            // --- Adım 4: Disk layoutunu güncelle ---
            progress?.Report(new IsoWriteProgress(iso.FileSizeBytes, iso.FileSizeBytes, 0, "[4/4] USB disk yapısı güncelleniyor..."));

            await Task.Run(() =>
            {
                DeviceIoControl(diskHandle, IOCTL_DISK_UPDATE_PROPERTIES, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
                DeviceIoControl(diskHandle, NativeDisk.FSCTL_UNLOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
            }, ct);

            progress?.Report(new IsoWriteProgress(iso.FileSizeBytes, iso.FileSizeBytes, 0,
                $"✅ Tamamlandı! {iso.FileName} başarıyla yazıldı."));
        }

        // --- Win32 sabitleri (Rufus'un winio.h / drive.h'den) ---
        private const uint FILE_FLAG_NO_BUFFERING   = 0x20000000; // Rufus: sektör hizamalı yazma için gerekli
        private const uint FILE_FLAG_WRITE_THROUGH  = 0x80000000; // Rufus: cache bypass, doğrudan donanım yazma
        private const uint IOCTL_DISK_UPDATE_PROPERTIES = 0x00070140; // Disk layout refresh

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            Microsoft.Win32.SafeHandles.SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer, uint nInBufferSize,
            IntPtr lpOutBuffer, uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        /// <summary>
        /// Yazma boyutunu 512 byte sektör sınırına yukarı yuvarlar.
        /// Rufus'un GetDrivePartitionData sektör geometri hizalamasından esinlenildi.
        /// </summary>
        private static int AlignToSector(int size, int sectorSize = 512)
        {
            return ((size + sectorSize - 1) / sectorSize) * sectorSize;
        }

        private static string FormatBytes(long bytes)
        {
            const long GB = 1073741824L;
            const long MB = 1048576L;
            return bytes >= GB
                ? $"{bytes / (double)GB:F2} GB"
                : $"{bytes / (double)MB:F0} MB";
        }
    }

    /// <summary>ISO yazma işlemi ilerleme verisi.</summary>
    public record IsoWriteProgress(
        long BytesWritten,
        long TotalBytes,
        double SpeedMBps,
        string StatusText)
    {
        public double ProgressPercent =>
            TotalBytes > 0 ? (double)BytesWritten / TotalBytes * 100.0 : 0;
    }
}
