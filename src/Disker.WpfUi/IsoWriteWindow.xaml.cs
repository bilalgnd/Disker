using System.Windows;
using System.Windows.Input;
using Disker.App.Helpers;
using Disker.App.ViewModels;
using Wpf.Ui.Controls;

namespace Disker.App
{
    public partial class IsoWriteWindow : FluentWindow
    {
        private readonly MainViewModel _vm;

        public IsoWriteWindow(MainViewModel viewModel)
        {
            _vm = viewModel;
            DataContext = viewModel;
            InitializeComponent();

            Closing += (_, e) =>
            {
                if (_vm.IsIsoWriting)
                {
                    _vm.CancelIsoWrite();
                }
            };
        }

        private void OnCloseWindowClick(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void OnSelectIsoClick(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Loc.Instance.IsTr ? "ISO veya Disk Yansıması Seç" : "Select ISO or Disk Image",
                Filter = "Disk / ISO Yansımaları (*.iso;*.img;*.vhd;*.raw)|*.iso;*.img;*.vhd;*.raw|Tüm Dosyalar (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this) == true)
            {
                _vm.SelectIsoFile(dialog.FileName);
            }
        }

        private async void OnCalculateChecksumClick(object sender, MouseButtonEventArgs e)
        {
            await _vm.CalculateIsoChecksumAsync();
        }

        private void OnToggleAdvancedDriveClick(object sender, MouseButtonEventArgs e)
        {
            _vm.ShowAdvancedDriveProperties = !_vm.ShowAdvancedDriveProperties;
        }

        private void OnToggleAdvancedFormatClick(object sender, MouseButtonEventArgs e)
        {
            _vm.ShowAdvancedFormatOptions = !_vm.ShowAdvancedFormatOptions;
        }

        private void OnStartWriteClick(object sender, MouseButtonEventArgs e)
        {
            if (!_vm.HasSelectedIso || _vm.SelectedUsbTarget == null) return;

            var confirm = new IsoWriteConfirmDialog(_vm, this);
            if (confirm.ShowDialog() == true)
            {
                _ = _vm.ExecuteIsoWriteAsync();
            }
        }

        private void OnCancelWriteClick(object sender, MouseButtonEventArgs e)
        {
            _vm.CancelIsoWrite();
        }

        private void OnViewLogClick(object sender, MouseButtonEventArgs e)
        {
            System.Windows.MessageBox.Show(
                this,
                $"Seçili ISO: {_vm.SelectedIsoPath}\nBoyut: {_vm.SelectedIsoSizeText}\nHedef USB: {_vm.SelectedUsbTarget?.PrimaryVolumeTitle}\nBölüm Düzeni: {_vm.IsoPartitionScheme}\nHedef Sistem: {_vm.IsoTargetSystem}\nDosya Sistemi: {_vm.IsoFileSystem}\nDurum: {_vm.IsoWriteStatusText}",
                "Disker / Rufus İşlem Kaydı",
                System.Windows.MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void OnAboutClick(object sender, MouseButtonEventArgs e)
        {
            System.Windows.MessageBox.Show(
                this,
                "Disker USB Flash Motoru\nRufus (Pete Batard) Win32 DD mimarisinden esinlenerek C# .NET 8 ile geliştirilmiştir.\n\n32MB Sektör Hizalamalı Ham Disk Yazma Motoru.",
                "Hakkında",
                System.Windows.MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
