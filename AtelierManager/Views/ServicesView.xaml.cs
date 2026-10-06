using System.Windows;
using System.Windows.Controls;
using AtelierManager;
using AtelierManager.ViewModels;
using AtelierManager.Services;

namespace AtelierManager.Views
{
    public partial class ServicesView : UserControl
    {
        public ServicesView()
        {
            InitializeComponent();
            DataContext = new ServicesViewModel();

            if (!CurrentUserService.IsAdmin)
            {
                AddServiceButton.Visibility = Visibility.Collapsed;
                EditServiceButton.Visibility = Visibility.Collapsed;
                DeleteServiceButton.Visibility = Visibility.Collapsed;
                AddExtraButton.Visibility = Visibility.Collapsed;
                EditExtraButton.Visibility = Visibility.Collapsed;
                DeleteExtraButton.Visibility = Visibility.Collapsed;
            }
        }

        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Изменять справочник услуг может только администратор.");
                return;
            }
            if (DataContext is ServicesViewModel vm) vm.AddService();
        }

        private void EditService_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Изменять справочник услуг может только администратор.");
                return;
            }
            if (DataContext is ServicesViewModel vm) vm.EditSelectedService();
        }

        private void DeleteService_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Удалять услуги может только администратор.");
                return;
            }
            if (DataContext is ServicesViewModel vm) vm.DeleteSelectedService();
        }

        private void RefreshService_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ServicesViewModel vm) vm.Refresh();
        }

        private void AddExtra_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Изменять дополнительные элементы может только администратор.");
                return;
            }
            if (DataContext is ServicesViewModel vm) vm.AddExtraElement();
        }

        private void EditExtra_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Изменять дополнительные элементы может только администратор.");
                return;
            }
            if (DataContext is ServicesViewModel vm) vm.EditSelectedExtraElement();
        }

        private void DeleteExtra_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Удалять дополнительные элементы может только администратор.");
                return;
            }
            if (DataContext is ServicesViewModel vm) vm.DeleteSelectedExtraElement();
        }
    }
}