using System.Windows;
using AtelierManager;
using AtelierManager.ViewModels;

namespace AtelierManager.Views
{
    public partial class ServiceWindow : Window
    {
        public ServiceWindow(ServiceWindowViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ServiceWindowViewModel vm) return;

            if (!vm.Validate(out var error))
            {
                UiMessages.Validation(error);
                return;
            }

            DialogResult = true;
        }
    }
}