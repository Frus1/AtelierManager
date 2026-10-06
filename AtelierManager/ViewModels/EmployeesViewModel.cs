using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using AtelierManager.Data;
using AtelierManager.Models;
using AtelierManager.Services;
using AtelierManager.Views;
using Microsoft.EntityFrameworkCore;

namespace AtelierManager.ViewModels
{
    public class EmployeesViewModel : BaseViewModel
    {
        private readonly AtelierContext _db = new();
        public ObservableCollection<Employee> Employees { get; } = new();

        private Employee? _selected;
        public Employee? Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedEmployee));
            }
        }

        public bool HasSelectedEmployee => Selected != null;

        public EmployeesViewModel()
        {
            Refresh();
        }

        public void Refresh()
        {
            Employees.Clear();
            var list = _db.Employees
                .AsNoTracking()
                .Include(e => e.Role)
                .OrderBy(e => e.FullName)
                .ToList();

            foreach (var employee in list)
                Employees.Add(employee);

            Selected = null;
        }

        public void AddEmployee()
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Недостаточно прав для управления работниками.");
                return;
            }

            var vm = new EmployeeWindowViewModel(_db);
            var dlg = new EmployeeWindow(vm) { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() != true) return;

            var employee = new Employee
            {
                FullName = vm.FullName,
                Login = vm.Login,
                Password = vm.Password,
                RoleId = vm.SelectedRole!.Id,
                IsActive = vm.IsActive
            };

            _db.Employees.Add(employee);
            _db.SaveChanges();
            Refresh();
        }

        public void EditSelectedEmployee()
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Недостаточно прав для управления работниками.");
                return;
            }

            if (Selected == null) return;

            var employee = _db.Employees.First(e => e.Id == Selected.Id);
            var vm = new EmployeeWindowViewModel(_db, employee);
            var dlg = new EmployeeWindow(vm) { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() != true) return;

            employee.FullName = vm.FullName;
            employee.Login = vm.Login;
            employee.Password = vm.Password;
            employee.RoleId = vm.SelectedRole!.Id;
            employee.IsActive = vm.IsActive;

            _db.SaveChanges();
            Refresh();
        }

        public void ToggleActiveSelectedEmployee()
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Недостаточно прав для управления работниками.");
                return;
            }

            if (Selected == null) return;

            if (Selected.Id == CurrentUserService.CurrentUser?.Id)
            {
                UiMessages.Error("Нельзя отключить текущего пользователя.");
                return;
            }

            var employee = _db.Employees.First(e => e.Id == Selected.Id);
            employee.IsActive = !employee.IsActive;
            _db.SaveChanges();
            Refresh();
        }

        public void DeleteSelectedEmployee()
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Недостаточно прав для управления работниками.");
                return;
            }

            if (Selected == null) return;

            if (Selected.Id == CurrentUserService.CurrentUser?.Id)
            {
                UiMessages.Error("Нельзя удалить текущего пользователя.");
                return;
            }

            if (!UiMessages.ConfirmDelete("Удалить работника? Если с ним связаны заказы, он будет отключён, а не удалён."))
                return;

            var employee = _db.Employees.First(e => e.Id == Selected.Id);
            var hasOrders = _db.Orders.Any(o => o.EmployeeId == employee.Id);

            if (hasOrders)
            {
                employee.IsActive = false;
            }
            else
            {
                _db.Employees.Remove(employee);
            }

            _db.SaveChanges();
            Refresh();
        }
    }
}
