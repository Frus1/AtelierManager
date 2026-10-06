using System.Windows;
using AtelierManager.Models;

namespace AtelierManager.Views
{
    public partial class PersonWindow : Window
    {
        public PersonWindow(Person person)
        {
            InitializeComponent();
            DataContext = person;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not Person person) return;

            person.DisplayName = person.DisplayName?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(person.DisplayName))
                person.DisplayName = "Персона";

            if (!string.IsNullOrWhiteSpace(person.Gender))
                person.Gender = person.Gender.Trim();

            DialogResult = true;
        }
    }
}