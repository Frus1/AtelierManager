using System.Text.RegularExpressions;
using System.Windows;
using AtelierManager;
using AtelierManager.Models;

namespace AtelierManager.Views
{
    public partial class ClientWindow : Window
    {
        public Client Client { get; }

        public ClientWindow(Client client)
        {
            InitializeComponent();
            Client = client;
            DataContext = Client;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Client.FullName = Client.FullName?.Trim() ?? "";
            Client.Phone = Client.Phone?.Trim() ?? "";
            Client.Email = string.IsNullOrWhiteSpace(Client.Email) ? null : Client.Email.Trim();

            if (string.IsNullOrWhiteSpace(Client.FullName))
            {
                UiMessages.Validation("Введите ФИО клиента.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Client.Phone))
            {
                UiMessages.Validation("Введите телефон клиента.");
                return;
            }

            var normalizedPhone = Regex.Replace(Client.Phone, @"[^\d+]", "");

            if (normalizedPhone.Count(c => c == '+') > 1 ||
                (normalizedPhone.Contains('+') && !normalizedPhone.StartsWith("+")))
            {
                UiMessages.Validation("Телефон введён некорректно.");
                return;
            }

            var digitsOnly = Regex.Replace(normalizedPhone, @"[^\d]", "");
            if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
            {
                UiMessages.Validation("Телефон должен содержать от 10 до 15 цифр.");
                return;
            }

            Client.Phone = normalizedPhone;

            if (!string.IsNullOrWhiteSpace(Client.Email))
            {
                if (!Regex.IsMatch(Client.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    UiMessages.Validation("Email введён некорректно.");
                    return;
                }
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}