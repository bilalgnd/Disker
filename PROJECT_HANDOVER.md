# Disker - Proje Durum ve Devir Raporu (Project Handover & Documentation)

Bu belge, **Disker (Modern Windows Disk Yönetim ve Sağlık Uygulaması)** projesinde bugüne kadar yapılan tüm geliştirmeleri, mimari yapıyı, bileşenleri ve çalışma prensiplerini içermektedir. Yeni bir oturumda yapay zeka veya geliştirici bu belgeyi okuyarak projenin mevcut tüm durumuna eksiksiz hakim olabilir.

---

## 1. Proje Özeti & Vizyonu
* **Teknoloji:** .NET 8 (C# 12), WPF, WPF-UI (v3.0.5 Fluent UI), CommunityToolkit.Mvvm (v8.3.2).
* **Amaç:** Windows'un karmaşık `Disk Yönetimi (diskmgmt.msc)` ve `Diskpart` araçlarına modern, güvenli, renkli, akıcı animasyonlu ve çift dilli (Türkçe / İngilizce) bir alternatif sunmak.
* **Tasarım:** Windows 11 Fluent Dark Theme, özel disk renk temaları, kart kaydırma/reorder animasyonları, modal dialoglar.

---

## 2. Çözüm Mimarisi & Dosya Yapısı

```
c:\Users\bilal\Desktop\disker\
├── Disker.sln
├── PROJECT_HANDOVER.md                     <-- (Bu Belge)
├── src/
│   ├── Disker.Core/                        <-- Çekirdek İş Mantığı & Servisler
│   │   ├── Models/
│   │   │   └── DiskModels.cs               <-- PhysicalDiskInfo, PartitionInfo, SmartAttribute, OperationResult
│   │   ├── Wmi/
│   │   │   └── WmiStorageProvider.cs       <-- Win32_DiskDrive, MSFT_PhysicalDisk, Partition/Volume analizi
│   │   ├── Operations/
│   │   │   └── DiskOperationService.cs     <-- Disk Sıfırlama, Tek Bölüm Silme, Genişletme (PowerShell/Storage)
│   │   ├── Diagnostics/
│   │   │   └── DiskHealthAnalysisService.cs<-- S.M.A.R.T. Teşhis, Sıcaklık, Sektör Geometrisi Raporlama
│   │   ├── Safety/
│   │   │   └── SafetyGuardService.cs       <-- C: ve Sistem Sürücüsü Koruma Mantığı
│   │   └── Settings/
│   │       └── UserSettingsService.cs      <-- %AppData%\Disker\disker_settings.json Kalıcılık Servisi
│   │
│   └── Disker.WpfUi/                       <-- WPF Kullanıcı Arayüzü & MVVM
│       ├── Assets/
│       │   └── app.ico                     <-- Uygulama İkonu (red1.ico)
│       ├── Helpers/
│       │   ├── Loc.cs                      <-- Canlı İki Dilli (TR / EN) Localization Singleton Motoru
│       │   └── Converters.cs               <-- WPF Converter'ları (Progress Bar vb.)
│       ├── ViewModels/
│       │   └── MainViewModel.cs            <-- Ana ViewModel, Rufus & Disk Operasyon State'leri
│       ├── IsoWriteWindow.xaml / .cs       <-- Rufus Tarzı Bağımsız ISO → Bootable USB Popup Penceresi
│       ├── IsoWriteConfirmDialog.xaml / .cs<-- Veri Kaybı Onay Penceresi
│       ├── MainWindow.xaml / .cs           <-- Ana Arayüz (Fluent Window, Canlı Sürükle-Bırak, USB Hot-Plug)
│       ├── build_number.txt                <-- Otomatik Artan Derleme Sayacı (Build Counter)
│       └── Disker.WpfUi.csproj             <-- Çıktı: bin\ReleaseNew\Release\net8.0-windows10.0.19041.0\DiskerApp.exe
```

---

## 3. Yapılan Tüm İşler ve Geliştirilen Özellikler (Chronological Changelog)

### 1. Canlı Sürükle-Bırak Sıralama & Sınır Koruması (Boundary Clamping)
* **Problem:** Disk kartları yukarı veya aşağı sürüklenirken pencere dışına taşabiliyordu.
* **Çözüm:** `MainWindow.xaml.cs` içinde `OnDragGripPreviewMouseMove` metodu güncellendi. `_currentDragIndex == 0` iken yukarı çekme, `_currentDragIndex == Count - 1` iken aşağı çekme sınırlandırıldı (clamped). Sürüklenen karta `CardScale = 1.015`, `ZIndex = 999`, `CardOpacity = 0.94` canlı görsel efektleri uygulandı.

### 2. Sade ve Temiz Bölüm Bilgi Satırları
* Alt bölüm satırlarında kalabalık oluşturan yüzde metinleri `(%72.4)` kaldırıldı.
* Format: `Dolu: 674,25 GB  •  Boş: 256,31 GB` şeklinde net ve minimalist hale getirildi.

### 3. Modern Dahili Sıfırlama & Biçimlendirme Modalı (In-App Reset Modal)
* Eski Win32 MessageBox yerine Fluent Dark Modal dialog tasarlandı.
* **Özellikler:**
  * **Bölümleme Tablosu:** GPT (Modern UEFI) veya MBR (Eski BIOS).
  * **Dosya Sistemi:** NTFS, exFAT, FAT32.
  * **Sürücü Harfi Seçimi:** Sistemdeki boş harfleri (`D:` - `Z:`) otomatik keşfeden dropdown listesi.
  * **Birim Etiketi:** Özel isim girme kutusu.
  * **Güvenlik Uyarısı:** Kırmızı uyarı kutusu ve adım adım canlı durum/ilerleme metni.

### 4. Tek Bölüm Silme ve Birleştirme (Single Partition Delete & Merge)
* Diskin ana verilerine (örn. `D:`) dokunmadan, yetim kalmış `100 MB EFI` veya kurtarma bölümlerini silmek için satır sonuna `🗑️` çöp kutusu butonu ve onay modalı eklendi.
* `DiskOperationService.DeletePartitionAndExtendAdjacentAsync` servisi ile güvenli silme yapıldı.
* *Not:* Windows Storage API kuralları gereği bitişik boş alan yalnızca sağa doğru birleştirilebilir; sola doğru alan birleştirmelerde bilgilendirme yapılır.

### 5. Göz İkonu ile Sistem Bölümlerini Gizleme/Gösterme (`👁️` / `🙈`)
* Üst navigasyon çubuğunda `Ayarlar` sekmesinin hemen sağına dinamik göz butonu eklendi.
* **Göz Kapalı (`🙈`):** `GPT: System #1 (100 MB)`, `GPT: Unknown #3 (856 MB)` ve `MSR` bölümleri anında gizlenir; disklerde yalnızca kullanıcıya ait ana depolama sürücüleri (`C:`, `D:`, `F:`, vb.) listelenir.
* **Göz Açık (`👁️`):** Tüm gizli ve sistem bölümleri detaylarıyla geri görünür.
* Göz durumu `%AppData%\Disker\disker_settings.json` içinde kalıcı olarak saklanır.

### 6. Tek Tıkla Dosya Gezgini'nde Açma & Canlı Hover Efektleri
* Disk ana başlıklarına (`Lenore (C:) 📁`, `Carmilla (D:) 📁`) veya bölüm satırlarına tıklandığında ilgili sürücü doğrudan Windows Dosya Gezgini (`explorer.exe`) ile açılır.
* **Hover:** Fare satır üzerine geldiğinde satır gök mavisi (`#38BDF8`) ışıma yapar ve ana başlıkta klasör ikonu belirir.

### 7. Otomatik Canlı USB Hot-Plug Algılama (Live USB Detection)
* `MainWindow.xaml.cs` içerisine `HwndSource.AddHook` ile native Win32 `WM_DEVICECHANGE` (`0x0219`), `DBT_DEVICEARRIVAL` ve `DBT_DEVICEREMOVECOMPLETE` mesaj dinleyicileri eklendi.
* Uygulama açıkken bilgisayara USB bellek takıldığında veya çıkarıldığında, kullanıcı hiçbir butona basmadan uygulama 1 saniye içinde kendini arka planda otomatik yeniler.
* USB bellekler turkuaz renkli `USB` medya rozetiyle listelenir.

### 8. Tam Çift Dilli (TR / EN) Yerelleştirme Motoru (`Loc.cs`)
* Üst sekme `Ayarlar` olarak sadeleştirildi.
* Ayarlar sayfası içine **`🇹🇷 Türkçe (TR)`** ve **`🇬🇧 English (EN)`** dil seçicisi eklendi.
* Tek tıkla tüm sekmeler, sayaçlar, butonlar, modallar ve donanım terimleri anında seçilen dile çevrilir ve JSON ayarlarında saklanır.

### 9. Merkezi Modern Yükleme (Loading Splash Screen)
* Uygulama ilk açıldığında veya yenilendiğinde, donanım taraması bitene kadar ekranın tam ortasında dönen neon mavi halka ve `Sistem Diskleri Taranıyor...` / `Scanning System Disks...` bilgi kartı gösterilir.
* Tarama kilitlenmeleri (`_isCurrentlyScanning`) çözüldü; tarama tamamlandığında kart anında kapanır.

### 10. Üst Çubuk Sayaç Rozetlerinin Hizalanması
* `Toplam Disk`, `Korumalı` ve `Sıfırlanabilir` sayaç rozetleri üst çubuğun en sağına, `Yenile` butonunun yanına hizalandı.

---

## 4. Kullanıcı Ayarları Dosyası Formatı (`%AppData%\Disker\disker_settings.json`)
```json
{
  "Language": "tr",
  "ShowSystemPartitions": false,
  "DiskOrder": [
    "KINGSTON SNV2S1000G_50026B76865EEAEF",
    "HyperX Fury 3D 120GB_50026B7682D7FE43",
    "ST3160815AS_9RA7L5KZ",
    "WDC WDS120G2G0A-00JH30_204186801993",
    "ST500DM002-1BD142_W3T61XDE"
  ],
  "DiskColors": {
    "KINGSTON SNV2S1000G_50026B76865EEAEF": "#8B5CF6",
    "HyperX Fury 3D 120GB_50026B7682D7FE43": "#06B6D4",
    "ST3160815AS_9RA7L5KZ": "#10B981",
    "WDC WDS120G2G0A-00JH30_204186801993": "#F59E0B",
    "ST500DM002-1BD142_W3T61XDE": "#EC4899"
  }
}
```

---

## 5. Projeyi Derleme ve Çalıştırma

### Derleme (Release):
```powershell
dotnet build "c:\Users\bilal\Desktop\disker\src\Disker.WpfUi\Disker.WpfUi.csproj" -c Release
```

### Çalıştırma:
```powershell
Start-Process "c:\Users\bilal\Desktop\disker\src\Disker.WpfUi\bin\ReleaseNew\Release\net8.0-windows10.0.19041.0\DiskerApp.exe"
```

---
*Rapor Oluşturulma Tarihi: 24 Ağustos 2026*
