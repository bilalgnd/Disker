# 💽 Disker — Modern Windows 11 Disk Manager & ISO USB Flasher

<p align="center">
  <img src="src/Disker.WpfUi/Assets/app.ico" width="80" height="80" alt="Disker Logo" />
</p>

<p align="center">
  <b>Windows için Modern, Akıcı, Güvenli Disk Yönetimi ve Rufus Motorlu ISO Bootable USB Yazıcı</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/WPF--UI-Fluent%20v3-0078D4?style=flat&logo=windows11" alt="WPF-UI" />
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D7?style=flat&logo=windows" alt="Windows" />
  <img src="https://img.shields.io/badge/License-MIT-green.style=flat" alt="License" />
</p>

---

## 📸 Ekran Görüntüleri (Screenshots)

### 1. Ana Disk ve Bölüm Yönetimi (Fiziksel Diskler)
Canlı renk kodlu disk kartları, sürükle-bırak sıralama, akıllı donanım tespiti (NVMe, SSD, HDD, USB) ve tek tıkla koruma kilidi:
![Ana Disk Yönetimi](ss/01_physical_disks.png)

### 2. Modern Dahili Sıfırlama ve Biçimlendirme Modalı
Bölümleme tablosu (GPT/MBR), dosya sistemi (NTFS/exFAT/FAT32) ve sürücü harfi yapılandırması:
![Disk Sıfırlama Modalı](ss/02_disk_format.png)

### 3. ISO → Bootable USB Yazıcı (Create Bootable USB)
Rufus Win32 DD mimarisinden esinlenen tam teşekküllü, sektör hizalamalı ham imaj yazıcı:
![Create Bootable USB](ss/03_create_bootable_usb.png)

### 4. S.M.A.R.T. Sağlık ve Sektör Teşhisi
Gerçek zamanlı sıcaklık, tahmini sağlık skoru, RAW S.M.A.R.T. parametreleri ve sektör geometrisi:
![SMART Sağlık Teşhisi](ss/04_smart_health.png)

### 5. Özelleştirilebilir Renk Temaları ve Dil Ayarları
10 farklı canlı disk renk paleti ve anında çift dilli (TR / EN) arayüz:
![Ayarlar ve Temalar](ss/05_settings.png)

---

## ✨ Temel Özellikler (Features)

- **🎨 Windows 11 Fluent Dark Tasarım:** Modern kart yapısı, 10 farklı canlı disk renk paleti ve akıcı animasyonlar.
- **🛡️ Akıllı Güvenlik Koruması (Safety Guard):** `C:` Windows sistem diski ve kullanıcının kilitlediği diskler silme/formatlama işlemlerine karşı donanımsal düzeyde kilitlenir. Koruma durumu oturumlar arasında kalıcıdır.
- **🚀 Create Bootable USB (Rufus DD Motoru):**
  - USB bellek tespiti ve otomatik hedef seçimi.
  - Windows ISO'ları için Standart Yükleme / Windows To Go desteği.
  - GPT (UEFI) ve MBR (BIOS) bölüm düzeni seçenekleri.
  - SHA-256 sağlama toplamı (Checksum) hesaplama.
  - 32MB sektör hizalı Win32 `FILE_FLAG_NO_BUFFERING` canlı yazma motoru.
- **↕️ Canlı Sürükle-Bırak Sıralama:** Disk kartlarını istediğiniz öncelik sırasına göre serbestçe sürükleyip bırakın (sınır korumalı ve animasyonlu).
- **👁️ Akıllı Sistem Bölümü Gizleme:** Göz ikonu ile küçük EFI bootloader, MSR ve kurtarma alanlarını tek tıkla gizleyin veya açın.
- **🌐 Çift Dil Desteği:** Tek tıkla anında Türkçe ve İngilizce arasında geçiş.
- **🔢 Otomatik Derleme Sayacı:** Her derlemede otomatik artan build ve versiyon takibi (`v1.0.X`).

---

## 🛠️ Mimari & Teknolojiler

- **Çatı:** .NET 8 (C# 12)
- **Arayüz:** WPF (Windows Presentation Foundation), [WPF-UI](https://github.com/lepoco/wpfui) Fluent v3.0.5
- **MVVM:** `CommunityToolkit.Mvvm` (Source Generators)
- **Veri Sağlayıcı:** WMI (`Win32_DiskDrive`, `Win32_DiskPartition`, `MSFT_PhysicalDisk`) + Win32 P/Invoke IOCTLs

---

## 🚀 Derleme & Çalıştırma

### Gereksinimler:
- Windows 10 (Build 19041+) veya Windows 11
- .NET 8.0 SDK

### Derleme:
```powershell
dotnet build "src/Disker.WpfUi/Disker.WpfUi.csproj" -c Release
```

### Çalıştırma:
```powershell
Start-Process "src/Disker.WpfUi/bin/ReleaseNew/Release/net8.0-windows10.0.19041.0/DiskerApp.exe"
```

---

## 📄 Lisans
Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.
