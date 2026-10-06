using System.Windows;

namespace AtelierManager
{
    public static class UiMessages
    {
        public static void Validation(string text)
        {
            MessageBox.Show(
                text,
                "Проверка",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        public static void Error(string text)
        {
            MessageBox.Show(
                text,
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        public static void Info(string text)
        {
            MessageBox.Show(
                text,
                "Информация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public static bool ConfirmDelete(string text)
        {
            return MessageBox.Show(
                text,
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public static bool Confirm(string text)
        {
            return MessageBox.Show(
                text,
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}