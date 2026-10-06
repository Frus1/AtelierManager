using System.Windows;
using System.Windows.Controls;
using AtelierManager;
using AtelierManager.ViewModels;
using AtelierManager.Services;

namespace AtelierManager.Views
{
    public partial class MaterialsView : UserControl
    {
        public MaterialsView()
        {
            InitializeComponent();
            DataContext = new MaterialsViewModel();

            if (!CurrentUserService.IsAdmin)
            {
                AddMaterialButton.Visibility = Visibility.Collapsed;
                EditMaterialButton.Visibility = Visibility.Collapsed;
                DeleteMaterialButton.Visibility = Visibility.Collapsed;
            }
        }

        private void AddMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Изменять справочник материалов может только администратор.");
                return;
            }
            if (DataContext is MaterialsViewModel vm) vm.AddMaterial();
        }

        private void EditMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Изменять справочник материалов может только администратор.");
                return;
            }
            if (DataContext is MaterialsViewModel vm) vm.EditSelectedMaterial();
        }

        private void DeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Удалять материалы может только администратор.");
                return;
            }
            if (DataContext is MaterialsViewModel vm) vm.DeleteSelectedMaterial();
        }

        private void RefreshMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MaterialsViewModel vm) vm.Refresh();
        }
    }
}