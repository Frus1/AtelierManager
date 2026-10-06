using AtelierManager.Models;

namespace AtelierManager.Services
{
    public static class CurrentUserService
    {
        public static Employee? CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsAdmin => string.Equals(CurrentUser?.Role?.Name, "Admin", System.StringComparison.OrdinalIgnoreCase);
        public static bool IsWorker => string.Equals(CurrentUser?.Role?.Name, "Worker", System.StringComparison.OrdinalIgnoreCase);

        public static void SetUser(Employee employee)
        {
            CurrentUser = employee;
        }

        public static void Clear()
        {
            CurrentUser = null;
        }
    }
}
