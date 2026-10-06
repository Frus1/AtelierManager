using System.Windows;
using AtelierManager.Services;
using AtelierManager.ViewModels;

namespace AtelierManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new OrdersViewModel();
            ApplyRoleRestrictions();
        }

        private void ApplyRoleRestrictions()
        {
            var user = CurrentUserService.CurrentUser;
            if (user != null)
            {
                Title = $"Управление ателье — {user.FullName} ({user.Role?.DisplayName})";
            }

            if (!CurrentUserService.IsAdmin)
            {
                ReportsTab.Visibility = Visibility.Collapsed;
                DataTab.Visibility = Visibility.Collapsed;
                EmployeesTab.Visibility = Visibility.Collapsed;
            }
        }
    }
}
