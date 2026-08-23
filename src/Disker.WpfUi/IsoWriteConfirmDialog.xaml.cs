using System.Windows;
using System.Windows.Input;
using Disker.App.ViewModels;
using Wpf.Ui.Controls;

namespace Disker.App
{
    public partial class IsoWriteConfirmDialog : FluentWindow
    {
        public IsoWriteConfirmDialog(MainViewModel viewModel, Window owner)
        {
            DataContext = viewModel;
            Owner = owner;
            InitializeComponent();
        }

        private void OnConfirmClick(object sender, MouseButtonEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, MouseButtonEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
