using System;
using System.Linq;
using Disker.Core.Models;
using Disker.Core.Safety;
using Disker.Core.Wmi;

namespace Disker.Tests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("        DISKER SYSTEM VERIFICATION TEST          ");
            Console.WriteLine("=================================================");

            var safety = new SafetyGuardService();
            var provider = new WmiStorageProvider(safety);

            Console.WriteLine($"\n[1] Güvenlik Kalkanı Sabit Etiketleri:");
            foreach (var label in safety.ProtectedLabels)
            {
                Console.WriteLine($"    - 🛡️ {label}");
            }

            Console.WriteLine($"\n[2] Sistem Diskleri Taranıyor...");
            var disks = provider.GetDisks();

            Console.WriteLine($"    Tespit Edilen Fiziksel Disk Sayısı: {disks.Count}\n");

            bool testPassed = true;

            foreach (var disk in disks)
            {
                Console.WriteLine($"-------------------------------------------------");
                Console.WriteLine($"Disk #{disk.DiskNumber}: {disk.FriendlyName} ({disk.CapacityFormatted})");
                Console.WriteLine($"  Seri No: {disk.SerialNumber}");
                Console.WriteLine($"  Tablo: {disk.PartitionStyle} | Sağlık: {disk.HealthStatus}");
                Console.WriteLine($"  Koruma Durumu: {(disk.IsProtected ? "🛡️ KORUMALI" : "🟢 SIFIRLANABİLİR")}");
                if (disk.IsProtected)
                {
                    Console.WriteLine($"  Koruma Sebebi: {disk.ProtectionReason}");
                }

                Console.WriteLine($"  Bölümler ({disk.Partitions.Count}):");
                foreach (var part in disk.Partitions)
                {
                    Console.WriteLine($"    - [{part.DriveLetter}] {part.DisplayName} - {part.SizeFormatted} ({part.FileSystem}) [Tür: {part.PartitionType}]");
                }

                // Doğrulama Kontrolleri:
                // Lyra (Disk 0), Lenore (Disk 4), Carmilla (Disk 3) KORUMALI olmalı!
                // Disk 1 ve Disk 2 SIFIRLANABİLİR olmalı!
                if (disk.Partitions.Any(p => p.VolumeLabel.Equals("Lyra", StringComparison.OrdinalIgnoreCase) ||
                                            p.VolumeLabel.Equals("Lenore", StringComparison.OrdinalIgnoreCase) ||
                                            p.VolumeLabel.Equals("Carmilla", StringComparison.OrdinalIgnoreCase) ||
                                            p.DriveLetter.Equals("C:", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!disk.IsProtected)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  [HATA] Disk #{disk.DiskNumber} ({disk.FriendlyName}) KORUNMALIYDI fakat korunmadı!");
                        Console.ResetColor();
                        testPassed = false;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  [BAŞARILI] Disk #{disk.DiskNumber} güvenlik kalkanı tarafından başarıyla korundu.");
                        Console.ResetColor();
                    }
                }
            }

            Console.WriteLine("\n=================================================");
            if (testPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   TÜM GÜVENLİK VE DİSK TESTLERİ BAŞARIYLA GEÇTİ!   ");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("   GÜVENLİK TESTLERİNDE HATA BULUNDU!   ");
                Console.ResetColor();
            }
            Console.WriteLine("=================================================");
        }
    }
}
