using System.Windows;
using AtelierManager;
using AtelierManager.ViewModels;

namespace AtelierManager.Views
{
    public partial class ExtraElementWindow : Window
    {
        public ExtraElementWindow(ExtraElementWindowViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ExtraElementWindowViewModel vm) return;

            if (!vm.Validate(out var error))
            {
                UiMessages.Validation(error);
                return;
            }

            DialogResult = true;
        }
    }
}