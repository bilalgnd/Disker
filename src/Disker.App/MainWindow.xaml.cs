using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;
using Disker.App.ViewModels;
using Disker.Core.Models;

namespace Disker.App
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new();

        private WindowsSystemDispatcherQueueHelper? _wsdqHelper;
        private MicaController? _micaController;
        private SystemBackdropConfiguration? _configurationSource;

        public MainWindow()
        {
            this.InitializeComponent();

            TrySetMicaBackdrop();

            // İlk açılışta diskleri yükle
            this.Activated += MainWindow_Activated;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            this.Activated -= MainWindow_Activated;
            await ViewModel.LoadDisksAsync();
        }

        private bool TrySetMicaBackdrop()
        {
            if (MicaController.IsSupported())
            {
                _wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                _wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                _configurationSource = new SystemBackdropConfiguration();
                _micaController = new MicaController { Kind = MicaKind.Base };

                _micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                _micaController.SetSystemBackdropConfiguration(_configurationSource);
                return true;
            }
            return false;
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                string tag = item.Tag?.ToString() ?? "";
                DisksView.Visibility = tag == "DisksPage" ? Visibility.Visible : Visibility.Collapsed;
                SafetyView.Visibility = tag == "SafetyPage" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void OnResetDiskClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PhysicalDiskInfo disk)
            {
                // Onay Diyaloğu
                var dialog = new ContentDialog
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = $"Disk #{disk.DiskNumber} Sıfırlama Onayı",
                    PrimaryButtonText = "Evet, Sıfırla ve Tek Bölüm Yap",
                    CloseButtonText = "İptal",
                    DefaultButton = ContentDialogButton.Close
                };

                var stack = new StackPanel { Spacing = 12 };
                stack.Children.Add(new TextBlock
                {
                    Text = $"UYARI: '{disk.FriendlyName}' (Disk #{disk.DiskNumber}) üzerindeki TÜM veriler, eski Windows/EFI ve kurtarma bölümleri dahil kalıcı olarak silinecektir!",
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed)
                });

                var labelBox = new TextBox
                {
                    Header = "Yeni Birim Etiketi (Volume Label):",
                    Text = disk.SizeBytes > 200UL * 1024 * 1024 * 1024 ? "Depolama500" : "Depolama120"
                };
                stack.Children.Add(labelBox);

                dialog.Content = stack;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    string newLabel = string.IsNullOrWhiteSpace(labelBox.Text) ? "Yeni Birim" : labelBox.Text.Trim();
                    var opResult = await ViewModel.ResetDiskToSingleVolumeAsync(disk, newLabel);

                    var infoDialog = new ContentDialog
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = opResult.Success ? "İşlem Başarılı" : "İşlem Başarısız",
                        Content = opResult.Message,
                        CloseButtonText = "Tamam"
                    };
                    await infoDialog.ShowAsync();
                }
            }
        }
    }

    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            public int dwSize;
            public int threadType;
            public int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object? dispatcherQueueController);

        object? m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null) return;

            DispatcherQueueOptions options;
            options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
            options.threadType = 2; // DQTYPE_THREAD_CURRENT
            options.apartmentType = 2; // DQTAT_COM_STA

            CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
        }
    }
}
