using System.Windows;
using System.Windows.Controls;
using AtelierManager.ViewModels;

namespace AtelierManager.Views
{
    public partial class DataManagementView : UserControl
    {
        public DataManagementView()
        {
            InitializeComponent();
            DataContext = new DataManagementViewModel();
        }

        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.CreateBackup();
        }

        private void RestoreSelectedBackup_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.RestoreSelectedBackup();
        }

        private void RestoreBackupFromFile_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.RestoreBackupFromFile();
        }

        private void DeleteSelectedBackup_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.DeleteSelectedBackup();
        }

        private void RefreshBackups_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.RefreshBackups();
        }

        private void ExportClients_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ExportClients();
        }

        private void ImportClients_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ImportClients();
        }

        private void ExportMaterials_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ExportMaterials();
        }

        private void ImportMaterials_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ImportMaterials();
        }

        private void ExportServices_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ExportServices();
        }

        private void ImportServices_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ImportServices();
        }

        private void ExportPersons_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ExportPersons();
        }

        private void ImportPersons_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ImportPersons();
        }

        private void ExportMeasurementSets_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ExportMeasurementSets();
        }

        private void ImportMeasurementSets_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ImportMeasurementSets();
        }

        private void ExportOrders_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ExportOrders();
        }

        private void ImportOrders_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ImportOrders();
        }

        private void ExportAllTables_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataManagementViewModel vm) vm.ExportAllTables();
        }
    }
}
