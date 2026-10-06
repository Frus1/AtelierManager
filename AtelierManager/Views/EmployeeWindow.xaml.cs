using System.Windows;
using AtelierManager.Data;
using AtelierManager.ViewModels;

namespace AtelierManager.Views
{
    public partial class EmployeeWindow : Window
    {
        public EmployeeWindow(EmployeeWindowViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not EmployeeWindowViewModel vm) return;

            using var db = new AtelierContext();
            if (!vm.Validate(db, out var error))
            {
                UiMessages.Validation(error);
                return;
            }

            DialogResult = true;
        }
    }
}
