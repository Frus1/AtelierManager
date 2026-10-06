using System.Windows;
using System.Windows.Controls;
using AtelierManager.ViewModels;

namespace AtelierManager.Views
{
    public partial class EmployeesView : UserControl
    {
        public EmployeesView()
        {
            InitializeComponent();
            DataContext = new EmployeesViewModel();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EmployeesViewModel vm) vm.AddEmployee();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EmployeesViewModel vm) vm.EditSelectedEmployee();
        }

        private void ToggleActive_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EmployeesViewModel vm) vm.ToggleActiveSelectedEmployee();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EmployeesViewModel vm) vm.DeleteSelectedEmployee();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EmployeesViewModel vm) vm.Refresh();
        }
    }
}
