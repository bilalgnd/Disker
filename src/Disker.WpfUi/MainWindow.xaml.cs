using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Disker.App.Helpers;
using Disker.App.ViewModels;
using Disker.Core.Diagnostics;
using Disker.Core.Models;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace Disker.App
{
    public partial class MainWindow : FluentWindow
    {
        public MainViewModel ViewModel { get; } = new();

        private static readonly Brush ActiveTabBg = new SolidColorBrush(Color.FromRgb(37, 99, 235)); // #2563EB
        private static readonly Brush InactiveTabBg = Brushes.Transparent;
        private static readonly Brush ActiveTabText = Brushes.White;
        private static readonly Brush InactiveTabText = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // #94A3B8

        private static readonly Brush SelectedOptionBg = new SolidColorBrush(Color.FromRgb(37, 99, 235));
        private static readonly Brush SelectedOptionBorder = new SolidColorBrush(Color.FromRgb(59, 130, 246));
        private static readonly Brush UnselectedOptionBg = new SolidColorBrush(Color.FromRgb(30, 41, 59));
        private static readonly Brush UnselectedOptionBorder = new SolidColorBrush(Color.FromRgb(51, 65, 85));

        // Canlı Etkileşimli Sürükleme State'i
        private bool _isInteractiveDragging = false;
        private PhysicalDiskInfo? _activeDraggedDisk = null;
        private double _dragStartMouseY = 0;
        private int _currentDragIndex = 0;
        private UIElement? _capturedGripElement = null;

        public MainWindow()
        {
            DataContext = ViewModel;
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Windows Donanım Değişikliği (USB Hot-Plug) Dinleyicisi
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DEVICECHANGE = 0x0219;
            const int DBT_DEVICEARRIVAL = 0x8000;
            const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

            if (msg == WM_DEVICECHANGE)
            {
                int wp = wParam.ToInt32();
                if (wp == DBT_DEVICEARRIVAL || wp == DBT_DEVICEREMOVECOMPLETE)
                {
                    // USB bellek veya harici disk takıldığında/çıkarıldığında otomatik canlı yenile
                    _ = Dispatcher.InvokeAsync(async () =>
                    {
                        await Task.Delay(1200); // Windows'un yeni aygıtı ve harfini kaydetmesi için kısa bekleme
                        await ViewModel.LoadDisksAsync();

                        // USB çıkarıldıysa ve USB Flash sekmesindeyse → Diskler'e dön
                        if (UsbFlashView?.Visibility == Visibility.Visible && !ViewModel.HasUsbDrives)
                        {
                            NavigateToDisksPage();
                        }
                    });
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>Diskler sekmesine geçer ve tab görsellerini günceller.</summary>
        private void NavigateToDisksPage()
        {
            if (DisksView != null) DisksView.Visibility = Visibility.Visible;
            if (HealthView != null) HealthView.Visibility = Visibility.Collapsed;
            if (SettingsView != null) SettingsView.Visibility = Visibility.Collapsed;
            if (UsbFlashView != null) UsbFlashView.Visibility = Visibility.Collapsed;

            UpdateTabVisual(TabDisksBorder, true);
            UpdateTabVisual(TabHealthBorder, false);
            UpdateTabVisual(TabSettingsBorder, false);
            UpdateTabVisual(TabUsbFlashBorder, false);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLanguageVisuals();
            await ViewModel.LoadDisksAsync();
        }

        private void OnNavTabClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem)
            {
                string tag = elem.Tag?.ToString() ?? "";

                if (tag == "UsbFlashPage")
                {
                    OpenIsoWritePopup();
                    return;
                }

                // View Visibility
                if (DisksView != null) DisksView.Visibility = tag == "DisksPage" ? Visibility.Visible : Visibility.Collapsed;
                if (HealthView != null) HealthView.Visibility = tag == "HealthPage" ? Visibility.Visible : Visibility.Collapsed;
                if (SettingsView != null) SettingsView.Visibility = tag == "SettingsPage" ? Visibility.Visible : Visibility.Collapsed;
                if (UsbFlashView != null) UsbFlashView.Visibility = Visibility.Collapsed;

                // Tab Visual State
                UpdateTabVisual(TabDisksBorder, tag == "DisksPage");
                UpdateTabVisual(TabHealthBorder, tag == "HealthPage");
                UpdateTabVisual(TabSettingsBorder, tag == "SettingsPage");
            }
        }

        private void OpenIsoWritePopup(PhysicalDiskInfo? targetDisk = null)
        {
            if (targetDisk != null)
            {
                ViewModel.SelectedUsbTarget = targetDisk;
            }

            var window = new IsoWriteWindow(ViewModel)
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private static void UpdateTabVisual(Border? tabBorder, bool isActive)
        {
            if (tabBorder == null) return;
            tabBorder.Background = isActive ? ActiveTabBg : InactiveTabBg;
            if (tabBorder.Child is StackPanel sp)
            {
                foreach (var child in sp.Children)
                {
                    if (child is System.Windows.Controls.TextBlock tb)
                    {
                        tb.Foreground = isActive ? ActiveTabText : InactiveTabText;
                    }
                }
            }
        }

        private void OnToggleEyeVisibilityClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel.ToggleSystemPartitionsVisibility();
        }

        // --- DİL SEÇİMİ (TR / EN) ---
        private void OnSelectLanguageClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem)
            {
                string lang = elem.Tag?.ToString() ?? "tr";
                ViewModel.SetLanguage(lang);
                UpdateLanguageVisuals();
            }
        }

        private void UpdateLanguageVisuals()
        {
            if (LangTrBorder != null && LangEnBorder != null)
            {
                bool isTr = Loc.Instance.IsTr;

                LangTrBorder.Background = isTr ? SelectedOptionBg : UnselectedOptionBg;
                LangTrBorder.BorderBrush = isTr ? SelectedOptionBorder : UnselectedOptionBorder;

                LangEnBorder.Background = !isTr ? SelectedOptionBg : UnselectedOptionBg;
                LangEnBorder.BorderBrush = !isTr ? SelectedOptionBorder : UnselectedOptionBorder;
            }
        }

        // --- AKICI, SINIR KORUMALI VE CANLI SLIDE ANİMASYONLU SÜRÜKLEME ---
        private void OnDragGripPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is PhysicalDiskInfo disk)
            {
                _isInteractiveDragging = true;
                _activeDraggedDisk = disk;
                _capturedGripElement = elem;
                _currentDragIndex = ViewModel.Disks.IndexOf(disk);
                _dragStartMouseY = e.GetPosition(DisksItemsControl).Y;

                disk.IsDragging = true;
                disk.ZIndex = 999;
                disk.CardScale = 1.015;
                disk.CardOpacity = 0.94;

                elem.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnDragGripPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isInteractiveDragging && _activeDraggedDisk != null && DisksItemsControl != null)
            {
                double currentMouseY = e.GetPosition(DisksItemsControl).Y;
                double deltaY = currentMouseY - _dragStartMouseY;

                if (_currentDragIndex == 0 && deltaY < 0)
                {
                    _activeDraggedDisk.DragOffsetY = Math.Max(deltaY * 0.08, -5.0);
                    e.Handled = true;
                    return;
                }

                if (_currentDragIndex == ViewModel.Disks.Count - 1 && deltaY > 0)
                {
                    _activeDraggedDisk.DragOffsetY = Math.Min(deltaY * 0.08, 5.0);
                    e.Handled = true;
                    return;
                }

                const double SwapThreshold = 45.0;

                if (deltaY > SwapThreshold && _currentDragIndex < ViewModel.Disks.Count - 1)
                {
                    int targetIndex = _currentDragIndex + 1;
                    ViewModel.Disks.Move(_currentDragIndex, targetIndex);
                    _currentDragIndex = targetIndex;
                    _dragStartMouseY = currentMouseY;
                    _activeDraggedDisk.DragOffsetY = 0;
                }
                else if (deltaY < -SwapThreshold && _currentDragIndex > 0)
                {
                    int targetIndex = _currentDragIndex - 1;
                    ViewModel.Disks.Move(_currentDragIndex, targetIndex);
                    _currentDragIndex = targetIndex;
                    _dragStartMouseY = currentMouseY;
                    _activeDraggedDisk.DragOffsetY = 0;
                }
                else
                {
                    _activeDraggedDisk.DragOffsetY = Math.Clamp(deltaY, -30.0, 30.0);
                }

                e.Handled = true;
            }
        }

        private void OnDragGripPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isInteractiveDragging && _activeDraggedDisk != null)
            {
                _isInteractiveDragging = false;
                _capturedGripElement?.ReleaseMouseCapture();
                _capturedGripElement = null;

                _activeDraggedDisk.DragOffsetY = 0;
                _activeDraggedDisk.IsDragging = false;
                _activeDraggedDisk.ZIndex = 0;
                _activeDraggedDisk.CardScale = 1.0;
                _activeDraggedDisk.CardOpacity = 1.0;

                ViewModel.SaveUserSettings();
                _activeDraggedDisk = null;

                e.Handled = true;
            }
        }

        private void OnToggleProtectionClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is PhysicalDiskInfo disk)
            {
                ViewModel.ToggleProtection(disk);
            }
        }

        private void OnSettingColorChoiceClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem)
            {
                string tag = elem.Tag?.ToString() ?? "";
                if (tag.StartsWith("Color:") && elem.DataContext is PhysicalDiskInfo disk)
                {
                    string hex = tag.Substring("Color:".Length);
                    ViewModel.SetDiskThemeColor(disk, hex);
                }
            }
        }

        private async void OnRefreshClick(object sender, MouseButtonEventArgs e)
        {
            await ViewModel.LoadDisksAsync();
        }

        private void OnPillButtonClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is DiskHealthReport report)
            {
                ViewModel.SelectHealthReport(report);
            }
        }

        // --- MODERN DİSK SIFIRLAMA & BİÇİMLENDİRME MODAL İŞLEMLERİ ---
        private void OnOpenResetModalClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is PhysicalDiskInfo disk)
            {
                ViewModel.OpenResetModal(disk);
                UpdateFormatOptionsVisuals();
            }
        }

        private void OnCloseResetModalClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel.CloseResetModal();
        }

        private void OnSelectPartitionSchemeClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem)
            {
                string scheme = elem.Tag?.ToString() ?? "GPT";
                ViewModel.ResetPartitionScheme = scheme;
                UpdateFormatOptionsVisuals();
            }
        }

        private void OnSelectFileSystemClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem)
            {
                string fs = elem.Tag?.ToString() ?? "NTFS";
                ViewModel.ResetFileSystem = fs;
                UpdateFormatOptionsVisuals();
            }
        }

        private void UpdateFormatOptionsVisuals()
        {
            if (SchemeGptBorder != null && SchemeMbrBorder != null)
            {
                bool isGpt = ViewModel.ResetPartitionScheme == "GPT";
                SchemeGptBorder.Background = isGpt ? SelectedOptionBg : UnselectedOptionBg;
                SchemeGptBorder.BorderBrush = isGpt ? SelectedOptionBorder : UnselectedOptionBorder;

                SchemeMbrBorder.Background = !isGpt ? SelectedOptionBg : UnselectedOptionBg;
                SchemeMbrBorder.BorderBrush = !isGpt ? SelectedOptionBorder : UnselectedOptionBorder;
            }

            if (FsNtfsBorder != null && FsExFatBorder != null && FsFat32Border != null)
            {
                FsNtfsBorder.Background = ViewModel.ResetFileSystem == "NTFS" ? SelectedOptionBg : UnselectedOptionBg;
                FsNtfsBorder.BorderBrush = ViewModel.ResetFileSystem == "NTFS" ? SelectedOptionBorder : UnselectedOptionBorder;

                FsExFatBorder.Background = ViewModel.ResetFileSystem == "exFAT" ? SelectedOptionBg : UnselectedOptionBg;
                FsExFatBorder.BorderBrush = ViewModel.ResetFileSystem == "exFAT" ? SelectedOptionBorder : UnselectedOptionBorder;

                FsFat32Border.Background = ViewModel.ResetFileSystem == "FAT32" ? SelectedOptionBg : UnselectedOptionBg;
                FsFat32Border.BorderBrush = ViewModel.ResetFileSystem == "FAT32" ? SelectedOptionBorder : UnselectedOptionBorder;
            }
        }

        private async void OnExecuteResetModalClick(object sender, MouseButtonEventArgs e)
        {
            await ViewModel.ExecuteResetModalAsync();
        }

        // --- TEK BÖLÜM SİLME VE BİRLEŞTİRME MODAL İŞLEMLERİ ---
        private void OnOpenDeletePartitionModalClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is PartitionInfo part)
            {
                var parentDisk = ViewModel.Disks.FirstOrDefault(d => d.Partitions.Contains(part));
                if (parentDisk != null)
                {
                    ViewModel.OpenDeletePartitionModal(parentDisk, part);
                }
            }
        }

        private void OnCloseDeletePartitionModalClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel.CloseDeletePartitionModal();
        }

        private async void OnExecuteDeletePartitionModalClick(object sender, MouseButtonEventArgs e)
        {
            await ViewModel.ExecuteDeletePartitionModalAsync();
        }

        // --- DİSKE / BÖLÜME TIKLANDIĞINDA DOSYA GEZGİNİ'NDE AÇMA ---
        private void OnOpenDriveInExplorerClick(object sender, MouseButtonEventArgs e)
        {
            string? drivePath = null;

            if (sender is FrameworkElement elem)
            {
                if (elem.Tag is PartitionInfo part && !string.IsNullOrWhiteSpace(part.DriveLetter))
                {
                    drivePath = part.DriveLetter.TrimEnd('\\') + "\\";
                }
                else if (elem.Tag is PhysicalDiskInfo disk)
                {
                    var p = disk.Partitions.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.DriveLetter));
                    if (p != null)
                    {
                        drivePath = p.DriveLetter.TrimEnd('\\') + "\\";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(drivePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = drivePath,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        // --- USB FLASH (ISO → BOOTABLE USB) OLAY İŞLEYİCİLERİ ---

        /// <summary>
        /// Disk kartındaki 💿 butonuna tıklandığında: o USB'yi hedef olarak seçer
        /// ve USB Flash sekmesine geçer.
        /// </summary>
        private void OnOpenUsbFlashFromCardClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is PhysicalDiskInfo disk)
            {
                OpenIsoWritePopup(disk);
            }
        }

        private void OnSelectIsoFileClick(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Loc.Instance.IsTr ? "ISO Dosyası Seç" : "Select ISO File",
                Filter = "ISO / Disk Görüntüsü (*.iso;*.img)|*.iso;*.img|Tüm Dosyalar (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                ViewModel.SelectIsoFile(dialog.FileName);
            }
        }

        private void OnStartIsoWriteClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel.OpenIsoWriteModal();
        }

        private void OnCloseIsoWriteModalClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel.CloseIsoWriteModal();
        }

        private async void OnConfirmIsoWriteClick(object sender, MouseButtonEventArgs e)
        {
            await ViewModel.ExecuteIsoWriteAsync();
        }

        private void OnCancelIsoWriteClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel.CancelIsoWrite();
        }
    }
}