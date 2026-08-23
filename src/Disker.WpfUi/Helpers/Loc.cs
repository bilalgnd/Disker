using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Disker.App.Helpers
{
    public class Loc : INotifyPropertyChanged
    {
        public static Loc Instance { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private string _currentLanguage = "tr";
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged(string.Empty); // Refresh all bound UI elements
                }
            }
        }

        public bool IsTr => CurrentLanguage == "tr";
        public bool IsEn => CurrentLanguage == "en";

        // Loading Screen
        public string LoadingDisksTitle => IsTr ? "Sistem Diskleri Taranıyor..." : "Scanning System Disks...";
        public string LoadingDisksDesc => IsTr ? "NVMe, SSD ve HDD donanımları tespit ediliyor..." : "Detecting NVMe, SSD and HDD hardware...";

        // Navigation Tabs
        public string TabDisks => IsTr ? "Fiziksel Diskler" : "Physical Disks";
        public string TabHealth => IsTr ? "Sektör & S.M.A.R.T. Sağlık" : "Sector & S.M.A.R.T. Health";
        public string TabSettings => IsTr ? "Ayarlar" : "Settings";

        // Header Chips & Buttons
        public string ChipTotalDisks => IsTr ? "Toplam Disk: " : "Total Disks: ";
        public string ChipProtected => IsTr ? "Korumalı: " : "Protected: ";
        public string ChipResettable => IsTr ? "Sıfırlanabilir: " : "Resettable: ";
        public string BtnRefresh => IsTr ? "Yenile" : "Refresh";
        public string EyeShowTooltip => IsTr ? "Sistem bölümlerini gizle" : "Hide system partitions";
        public string EyeHideTooltip => IsTr ? "Sistem bölümlerini göster" : "Show system partitions";

        // Partition Sub-row
        public string TotalLabel => IsTr ? "Toplam: " : "Total: ";
        public string DragTooltip => IsTr ? "Basılı tutup yukarı/aşağı kaydırarak sırasını değiştirin" : "Hold and drag up/down to reorder";
        public string DeletePartitionTooltip => IsTr ? "Bu bölümü sil ve boşalan alanı ana bölüme kat" : "Delete this partition and merge space";
        public string ResetDiskTooltip => IsTr ? "Diski Sıfırla ve Biçimlendir" : "Reset & Format Disk";

        // Health View
        public string HealthStatusTitle => IsTr ? "Sağlık Durumu" : "Health Status";
        public string TemperatureTitle => IsTr ? "Sıcaklık" : "Temperature";
        public string FirmwareLabel => IsTr ? "Aygıt Yazılımı" : "Firmware";
        public string SerialLabel => IsTr ? "Seri Numarası" : "Serial Number";
        public string InterfaceLabel => IsTr ? "Arabirim" : "Interface";
        public string TransferModeLabel => IsTr ? "Aktarım Kipi" : "Transfer Mode";
        public string DriveLetterLabel => IsTr ? "Sürücü Harfi" : "Drive Letter";
        public string TotalReadsLabel => IsTr ? "Okunan Toplamı" : "Total Reads";
        public string TotalWritesLabel => IsTr ? "Yazılan Toplamı" : "Total Writes";
        public string RotationalSpeedLabel => IsTr ? "Çevirme Oranı" : "Rotational Speed";
        public string PowerOnCountLabel => IsTr ? "Çalıştırılma Sayısı" : "Power-On Count";
        public string PowerOnHoursLabel => IsTr ? "Çalışma Süresi" : "Power-On Hours";
        public string StandardLabel => IsTr ? "Standart:" : "Standard:";
        public string FeaturesLabel => IsTr ? "Özellikler:" : "Features:";
        public string SectorDiagTitle => IsTr ? "Sektör Geometrisi & Teşhis Raporu" : "Sector Geometry & Diagnostics";
        public string SmartTitle => IsTr ? "S.M.A.R.T. Sağlık Parametreleri" : "S.M.A.R.T. Health Attributes";
        public string TotalSectorsLabel => IsTr ? "Toplam Sektör" : "Total Sectors";
        public string PhysicalSectorLabel => IsTr ? "Fiziksel Sektör" : "Physical Sector";
        public string FormatArchitectureLabel => IsTr ? "Format Mimarisi" : "Format Architecture";
        public string RefreshScanBtn => IsTr ? "Taramayı Yenile" : "Refresh Scan";

        // Settings View
        public string SettingsHeaderTitle => IsTr ? "Uygulama Ayarları" : "Application Settings";
        public string LanguageSectionTitle => IsTr ? "Dil Seçimi (Language)" : "Language Selection";
        public string LanguageSectionDesc => IsTr ? "Uygulama arayüz dilini buradan dilediğiniz gibi belirleyebilirsiniz." : "Choose your preferred user interface language.";
        public string ColorSectionTitle => IsTr ? "Disk Paneli Renk Temaları" : "Disk Panel Color Themes";
        public string ColorSectionDesc => IsTr ? "Her diskin ve panelinin ana rengini buradan dilediğiniz gibi belirleyebilirsiniz." : "Customize the primary accent color for each disk panel.";

        // Modals
        public string ModalResetTitle => IsTr ? "Diski Sıfırla & Biçimlendir" : "Reset & Format Disk";
        public string ModalResetSubtitle => IsTr ? "Bölümleme tablosunu ve dosya sistemini belirleyip diski tek parça yapın." : "Configure partition table and file system to create a clean single volume.";
        public string ModalPartitionScheme => IsTr ? "BÖLÜMLEME TABLOSU (PARTITION SCHEME)" : "PARTITION SCHEME";
        public string ModalGptDesc => IsTr ? "Modern Windows / UEFI (Önerilen)" : "Modern Windows / UEFI (Recommended)";
        public string ModalMbrDesc => IsTr ? "Eski BIOS / 2TB altı uyumluluk" : "Legacy BIOS / Under 2TB compatibility";
        public string ModalFileSystem => IsTr ? "DOSYA SİSTEMİ (FILE SYSTEM)" : "FILE SYSTEM";
        public string ModalDriveLetter => IsTr ? "SÜRÜCÜ HARFİ" : "DRIVE LETTER";
        public string ModalVolumeLabel => IsTr ? "BİRİM ETİKETİ (VOLUME LABEL)" : "VOLUME LABEL";
        public string ModalResetWarning => IsTr ? "DİKKAT: Seçili disk üzerindeki tüm veriler, eski EFI ve kurtarma bölümleri dahil kalıcı olarak silinecektir!" : "WARNING: All data on the selected disk, including old EFI and recovery partitions, will be permanently deleted!";
        public string ModalCancel => IsTr ? "İptal" : "Cancel";
        public string ModalConfirmReset => IsTr ? "Diski Sıfırla & Biçimlendir" : "Reset & Format Disk";

        public string ModalDeletePartTitle => IsTr ? "Bölümü Sil & Birleştir" : "Delete & Merge Partition";
        public string ModalDeletePartSubtitle => IsTr ? "Yalnızca bu bölüm silinecek, diskin diğer verileri korunacaktır." : "Only this partition will be removed, all other disk data remains safe.";
        public string ModalDeletePartSafeTitle => IsTr ? "🛡️ DİĞER VERİLERİNİZ KORUNUR" : "🛡️ YOUR OTHER DATA REMAINS SAFE";
        public string ModalDeletePartSafeDesc => IsTr ? "Bu işlem yalnızca seçtiğiniz bu küçük bölümü siler. Diskteki diğer ana bölümünüz ve içerisindeki tüm dosyalarınız korunur ve boşalan alan ana bölüme katılır." : "This action only deletes this small partition. Your main partition and all files inside remain completely untouched, and freed space is merged.";
        public string ModalConfirmDeletePart => IsTr ? "Bölümü Sil & Birleştir" : "Delete & Merge Partition";

        // USB Flash Tab
        public string TabUsbFlash => IsTr ? "USB Flash" : "USB Flash";
        public string UsbFlashTitle => IsTr ? "ISO → Bootable USB" : "ISO → Bootable USB";
        public string UsbFlashSubtitle => IsTr ? "ISO dosyasını USB belleğe yazarak önyüklenebilir medya oluşturun" : "Write an ISO image to a USB drive to create bootable media";
        public string UsbFlashSelectIsoBtn => IsTr ? "ISO Dosyası Seç" : "Select ISO File";
        public string UsbFlashIsoLabel => IsTr ? "ISO Dosyası:" : "ISO File:";
        public string UsbFlashTargetLabel => IsTr ? "Hedef USB:" : "Target USB:";
        public string UsbFlashNoUsbFound => IsTr ? "Sisteme bağlı USB bellek bulunamadı. Lütfen bir USB bellek takın." : "No USB drives found. Please insert a USB flash drive.";
        public string UsbFlashOnlyUsb => IsTr ? "⚠️  Sadece USB bellekler listelenir — dahili diskler otomatik olarak korunur" : "⚠️  Only USB drives are listed — internal disks are protected automatically";
        public string UsbFlashStartBtn => IsTr ? "Yazmayı Başlat" : "Start Write";
        public string UsbFlashCancelBtn => IsTr ? "İptal Et" : "Cancel";

        // USB Flash Modal
        public string ModalIsoWriteTitle => IsTr ? "ISO'yu USB'ye Yaz" : "Write ISO to USB";
        public string ModalIsoWriteSubtitle => IsTr ? "Bu işlem USB bellekteki tüm verileri silecektir." : "This will erase all data on the USB drive.";
        public string ModalIsoWriteWarning => IsTr ? "DİKKAT: Seçili USB bellekteki TÜM VERİLER kalıcı olarak silinecek ve yerine ISO yazılacaktır!" : "WARNING: ALL DATA on the selected USB drive will be permanently erased and replaced with the ISO image!";
        public string ModalIsoWriteConfirm => IsTr ? "Evet, USB'ye Yaz" : "Yes, Write to USB";
        public string ModalIsoWriteCancel => IsTr ? "Vazgeç" : "Cancel";

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
