using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Disker.App
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogAndShowError("Dispatcher Hatası", e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogAndShowError("Kritik Çalışma Zamanı Hatası", ex);
            }
        }

        private static void LogAndShowError(string title, Exception ex)
        {
            string logText = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {title}:\n{ex}\n\n";
            try
            {
                File.AppendAllText("disker_error.log", logText);
            }
            catch { }

            MessageBox.Show(
                $"Uygulama çalışırken bir hata oluştu:\n\n{ex.Message}\n\nAyrıntılar 'disker_error.log' dosyasına kaydedildi.",
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
