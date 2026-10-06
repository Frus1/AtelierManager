using System.Linq;
using System.Windows;
using System.Windows.Input;
using AtelierManager.Data;
using AtelierManager.Services;
using Microsoft.EntityFrameworkCore;

namespace AtelierManager.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => LoginTextBox.Focus();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            TryLogin();
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                TryLogin();
        }

        private void TryLogin()
        {
            var login = LoginTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                UiMessages.Validation("Введите логин и пароль.");
                return;
            }

            using var db = new AtelierContext();
            var employee = db.Employees
                .Include(e => e.Role)
                .FirstOrDefault(e => e.Login == login && e.Password == password && e.IsActive);

            if (employee == null)
            {
                UiMessages.Error("Неверный логин или пароль, либо работник отключён.");
                return;
            }

            CurrentUserService.SetUser(employee);
            DialogResult = true;
        }
    }
}
