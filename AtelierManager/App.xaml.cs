using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using AtelierManager.Services;
using AtelierManager.Views;

namespace AtelierManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var culture = new CultureInfo("ru-RU");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            base.OnStartup(e);

            try
            {
                DatabaseBackupService.ApplyPendingRestoreIfExists();
                DatabaseSchemaUpdateService.EnsureUpdated();
                StockMaintenanceService.NormalizeInitialMaterialStockIfNeeded();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось подготовить базу данных.\n\n{ex.Message}",
                    "Подготовка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new LoginWindow();
            var loginResult = loginWindow.ShowDialog();

            if (loginResult != true)
            {
                Shutdown();
                return;
            }

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
