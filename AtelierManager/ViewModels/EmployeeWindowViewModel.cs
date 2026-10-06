using System.Collections.ObjectModel;
using System.Linq;
using AtelierManager.Data;
using AtelierManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AtelierManager.ViewModels
{
    public class EmployeeWindowViewModel : BaseViewModel
    {
        public ObservableCollection<Role> Roles { get; } = new();

        private string _fullName = "";
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        private string _login = "";
        public string Login
        {
            get => _login;
            set { _login = value; OnPropertyChanged(); }
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        private Role? _selectedRole;
        public Role? SelectedRole
        {
            get => _selectedRole;
            set { _selectedRole = value; OnPropertyChanged(); }
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public int? EditingEmployeeId { get; }

        public EmployeeWindowViewModel(AtelierContext db, Employee? employee = null)
        {
            foreach (var role in db.Roles.AsNoTracking().OrderBy(r => r.Id))
                Roles.Add(role);

            if (employee != null)
            {
                EditingEmployeeId = employee.Id;
                FullName = employee.FullName;
                Login = employee.Login;
                Password = employee.Password;
                IsActive = employee.IsActive;
                SelectedRole = Roles.FirstOrDefault(r => r.Id == employee.RoleId);
            }
            else
            {
                SelectedRole = Roles.FirstOrDefault(r => r.Name == "Worker") ?? Roles.FirstOrDefault();
            }
        }

        public bool Validate(AtelierContext db, out string error)
        {
            FullName = FullName?.Trim() ?? "";
            Login = Login?.Trim() ?? "";
            Password = Password?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(FullName))
            {
                error = "Введите ФИО работника.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Login))
            {
                error = "Введите логин.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                error = "Введите пароль.";
                return false;
            }

            if (SelectedRole == null)
            {
                error = "Выберите роль.";
                return false;
            }

            var exists = db.Employees.Any(e => e.Login == Login && (EditingEmployeeId == null || e.Id != EditingEmployeeId.Value));
            if (exists)
            {
                error = "Работник с таким логином уже существует.";
                return false;
            }

            error = "";
            return true;
        }
    }
}
