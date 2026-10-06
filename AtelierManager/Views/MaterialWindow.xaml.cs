using System.Windows;
using AtelierManager;
using AtelierManager.ViewModels;

namespace AtelierManager.Views
{
    public partial class MaterialWindow : Window
    {
        public MaterialWindow(MaterialWindowViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MaterialWindowViewModel vm) return;

            if (!vm.Validate(out var error))
            {
                UiMessages.Validation(error);
                return;
            }

            DialogResult = true;
        }
    }
}