using System.Windows;
using System.Windows.Controls;
using AtelierManager.ViewModels;

namespace AtelierManager.Views
{
    public partial class ClientsView : UserControl
    {
        public ClientsView()
        {
            InitializeComponent();
            DataContext = new ClientsViewModel();
        }

        private void AddClient_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientsViewModel vm)
                vm.Add();
        }

        private void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientsViewModel vm)
                vm.DeleteSelected();
        }

        private void EditClient_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientsViewModel vm)
                vm.EditSelectedClient();
        }


        private void RefreshClient_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientsViewModel vm)
                vm.LoadAsync();
        }

        private void AddPerson_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientsViewModel vm)
                vm.AddPerson();
        }

        private void EditPerson_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientsViewModel vm)
                vm.EditSelectedPerson();
        }

        private void DeletePerson_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientsViewModel vm)
                vm.DeleteSelectedPerson();
        }

        private void AddMeasurement_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientsViewModel vm)
                vm.AddMeasurementSet();
        }

        private void EditMeasurement_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientsViewModel vm)
                vm.EditSelectedMeasurementSet();
        }

        private void DeleteMeasurement_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientsViewModel vm)
                vm.DeleteSelectedMeasurementSet();
        }
    }
}