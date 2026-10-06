using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using AtelierManager.Data;
using AtelierManager.Models;
using AtelierManager.Views;
using AtelierManager.Services;
using Microsoft.EntityFrameworkCore;

namespace AtelierManager.ViewModels
{
    public class OrdersViewModel : BaseViewModel
    {
        private readonly AtelierContext _db = new();

        public ObservableCollection<Order> Orders { get; } = new();
        public ICollectionView OrdersView { get; }

        public bool CanAddOrder => CurrentUserService.IsAdmin;
        public Visibility OrderManagementVisibility => CanAddOrder ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<OrderService> SelectedOrderServices { get; } = new();
        public ObservableCollection<OrderServiceExtra> SelectedOrderServiceExtras { get; } = new();
        public ObservableCollection<OrderMaterial> SelectedOrderMaterials { get; } = new();
        public bool HasSelectedOrderMaterials => SelectedOrderMaterials.Count > 0;
        public bool HasSelectedOrderServiceExtras => SelectedOrderServiceExtras.Count > 0;

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                OrdersView.Refresh();
            }
        }

        private string _selectedStatusFilter = "Все статусы";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (_selectedStatusFilter == value) return;
                _selectedStatusFilter = value;
                OnPropertyChanged();
                OrdersView.Refresh();
            }
        }

        private string _selectedMaterialSourceFilter = "Все источники";
        public string SelectedMaterialSourceFilter
        {
            get => _selectedMaterialSourceFilter;
            set
            {
                if (_selectedMaterialSourceFilter == value) return;
                _selectedMaterialSourceFilter = value;
                OnPropertyChanged();
                OrdersView.Refresh();
            }
        }

        private string _selectedOrderStateFilter = "Все заказы";
        public string SelectedOrderStateFilter
        {
            get => _selectedOrderStateFilter;
            set
            {
                if (_selectedOrderStateFilter == value) return;
                _selectedOrderStateFilter = value;
                OnPropertyChanged();
                OrdersView.Refresh();
            }
        }

        private OrderEmployeeFilterItem? _selectedEmployeeFilter;
        public OrderEmployeeFilterItem? SelectedEmployeeFilter
        {
            get => _selectedEmployeeFilter;
            set
            {
                if (_selectedEmployeeFilter == value) return;
                _selectedEmployeeFilter = value;
                OnPropertyChanged();
                OrdersView.Refresh();
            }
        }

        public ObservableCollection<string> StatusFilters { get; } = new() { "Все статусы" };
        public ObservableCollection<string> MaterialSourceFilters { get; } = new()
        {
            "Все источники",
            "Клиент",
            "Ателье",
            "Смешанное"
        };
        public ObservableCollection<string> OrderStateFilters { get; } = new()
        {
            "Все заказы",
            "Активные",
            "Завершённые",
            "Просроченные"
        };

        public ObservableCollection<OrderEmployeeFilterItem> EmployeeFilters { get; } = new();

        public bool CanDeleteOrders => CurrentUserService.IsAdmin;

        private bool _isLoading;

        private Order? _selected;
        public Order? Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedOrder));

                LoadSelectedOrderDetailsAsync();
            }
        }

        public bool HasSelectedOrder => Selected != null;

        private OrderService? _selectedOrderService;
        public OrderService? SelectedOrderService
        {
            get => _selectedOrderService;
            set
            {
                if (_selectedOrderService == value) return;
                _selectedOrderService = value;
                OnPropertyChanged();

                LoadSelectedServiceExtras();
                OnPropertyChanged(nameof(IsMeasurementsNeededForSelectedLine));
                OnPropertyChanged(nameof(SelectedLineMeasurementSummary));
            }
        }
        public bool IsMeasurementsNeededForSelectedLine
        {
            get
            {
                if (SelectedOrderService?.Service == null) return false;
                if (!string.Equals(SelectedOrderService.Service.ServiceType, "Пошив", StringComparison.OrdinalIgnoreCase))
                    return false;

                return SelectedOrderService.MeasurementSet != null;
            }
        }
        public string SelectedLineMeasurementSummary
        {
            get
            {
                if (!IsMeasurementsNeededForSelectedLine) return "—";

                var ms = SelectedOrderService!.MeasurementSet!;
                var sb = new StringBuilder();

                sb.AppendLine($"Сняты: {ms.TakenAt:dd.MM.yyyy}");

                if (!string.IsNullOrWhiteSpace(ms.Summary)) sb.AppendLine(ms.Summary);
                if (!string.IsNullOrWhiteSpace(ms.TopText)) sb.AppendLine("Верх: " + ms.TopText);
                if (!string.IsNullOrWhiteSpace(ms.BottomText)) sb.AppendLine("Низ: " + ms.BottomText);
                if (!string.IsNullOrWhiteSpace(ms.ProductText)) sb.AppendLine("Изделие: " + ms.ProductText);
                if (!string.IsNullOrWhiteSpace(ms.Comment)) sb.AppendLine("Комментарий: " + ms.Comment);

                return sb.ToString().Trim();
            }
        }

        public OrdersViewModel()
        {
            OrdersView = CollectionViewSource.GetDefaultView(Orders);
            OrdersView.Filter = FilterOrder;
            LoadAsync();
        }

        private bool FilterOrder(object obj)
        {
            if (obj is not Order order)
                return false;

            var search = SearchText?.Trim();
            var matchesSearch = string.IsNullOrWhiteSpace(search)
                || order.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                || (order.Client?.FullName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (order.Status?.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (order.Employee?.FullName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (order.Notes?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (order.MaterialSource?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (order.MaterialComment?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || order.IntakeDate.ToString("dd.MM.yyyy").Contains(search, StringComparison.OrdinalIgnoreCase)
                || (order.DueDate?.ToString("dd.MM.yyyy").Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);

            var matchesStatus = SelectedStatusFilter == "Все статусы"
                || string.Equals(order.Status?.Name, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase);

            var matchesMaterialSource = SelectedMaterialSourceFilter == "Все источники"
                || string.Equals(order.MaterialSource, SelectedMaterialSourceFilter, StringComparison.OrdinalIgnoreCase);

            var matchesState = SelectedOrderStateFilter switch
            {
                "Активные" => order.Status?.IsFinal != true,
                "Завершённые" => order.Status?.IsFinal == true,
                "Просроченные" => order.DueDate.HasValue
                    && order.DueDate.Value.Date < DateTime.Today
                    && order.Status?.IsFinal != true,
                _ => true
            };

            var matchesEmployee = SelectedEmployeeFilter?.Id switch
            {
                null => true,
                0 => order.EmployeeId == null,
                int employeeId => order.EmployeeId == employeeId
            };

            return matchesSearch && matchesStatus && matchesMaterialSource && matchesState && matchesEmployee;
        }

        private async void LoadAsync()
        {
            Orders.Clear();

            var currentStatusFilter = SelectedStatusFilter;
            StatusFilters.Clear();
            StatusFilters.Add("Все статусы");

            var statuses = await _db.Statuses
                .AsNoTracking()
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .Select(s => s.Name)
                .ToListAsync();

            foreach (var status in statuses)
                if (!StatusFilters.Contains(status))
                    StatusFilters.Add(status);

            if (!StatusFilters.Contains(currentStatusFilter))
                SelectedStatusFilter = "Все статусы";

            var currentEmployeeFilterId = SelectedEmployeeFilter?.Id;
            EmployeeFilters.Clear();
            EmployeeFilters.Add(new OrderEmployeeFilterItem { Id = null, FullName = "Все работники" });
            EmployeeFilters.Add(new OrderEmployeeFilterItem { Id = 0, FullName = "Не указан" });

            var employees = await _db.Employees
                .AsNoTracking()
                .Where(e => e.IsActive)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            foreach (var employee in employees)
                EmployeeFilters.Add(new OrderEmployeeFilterItem { Id = employee.Id, FullName = employee.FullName });

            SelectedEmployeeFilter = EmployeeFilters.FirstOrDefault(x => x.Id == currentEmployeeFilterId)
                                     ?? EmployeeFilters.FirstOrDefault();

            var orders = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Client)
                .Include(o => o.Status)
                .Include(o => o.Employee)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            var serviceRows = await _db.OrderServices.AsNoTracking()
                .Select(x => new { x.Id, x.OrderId, x.BasePrice })
                .ToListAsync();

            var extraRows = await _db.OrderServiceExtras.AsNoTracking()
                .Select(x => new { x.OrderServiceId, x.Price, x.Qty })
                .ToListAsync();

            var extraDict = extraRows
                .GroupBy(x => x.OrderServiceId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Price * (x.Qty <= 0 ? 1 : x.Qty)));

            var serviceDict = serviceRows
                .GroupBy(x => x.OrderId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.BasePrice + (extraDict.TryGetValue(x.Id, out var extras) ? extras : 0)));

            var materialRows = await _db.OrderMaterials.AsNoTracking()
                .Select(x => new { x.OrderId, x.QtyUsed, x.UnitPrice })
                .ToListAsync();

            var materialDict = materialRows
                .GroupBy(x => x.OrderId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.QtyUsed * x.UnitPrice));

            foreach (var o in orders)
            {
                var services = serviceDict.TryGetValue(o.Id, out var st) ? st : 0;
                var materials = materialDict.TryGetValue(o.Id, out var mt) ? mt : 0;
                o.TotalCost = services + materials;
                Orders.Add(o);
            }

            OrdersView.Refresh();
            Selected = null;
        }

        public void RefreshOrders() => LoadAsync();

        public void AddOrder()
        {
            var vm = new OrderWindowViewModel(_db);
            var dlg = new OrderWindow(vm) { Owner = Application.Current.MainWindow };

            try
            {
                if (dlg.ShowDialog() == true)
                {
                    vm.Save();
                    RefreshOrders();
                }
            }
            catch (Exception ex)
            {
                UiMessages.Error(ex.ToString());
            }
        }

        public void EditSelectedOrder()
        {
            if (Selected == null) return;

            var vm = new OrderWindowViewModel(_db, Selected);
            var dlg = new OrderWindow(vm) { Owner = Application.Current.MainWindow };

            try
            {
                if (dlg.ShowDialog() == true)
                {
                    vm.Save();
                    RefreshOrders();
                }
            }
            catch (Exception ex)
            {
                UiMessages.Error($"Ошибка при сохранении заказа: {ex.Message}");
            }
        }

        public void DeleteSelectedOrder()
        {
            if (!CurrentUserService.IsAdmin)
            {
                UiMessages.Error("Удалять заказы может только администратор.");
                return;
            }

            if (Selected == null) return;

            if (!UiMessages.ConfirmDelete("Удалить заказ? Вместе с заказом будут удалены его позиции, доп. элементы и использованные материалы."))
                return;

            try
            {
                using var transaction = _db.Database.BeginTransaction();

                var order = _db.Orders.First(x => x.Id == Selected.Id);

                var usedMaterials = _db.OrderMaterials
                    .Where(x => x.OrderId == order.Id)
                    .GroupBy(x => x.MaterialId)
                    .Select(g => new { MaterialId = g.Key, Qty = g.Sum(x => x.QtyUsed) })
                    .ToList();

                foreach (var item in usedMaterials)
                {
                    var material = _db.Materials.FirstOrDefault(x => x.Id == item.MaterialId);
                    if (material != null)
                        material.Quantity += item.Qty;
                }

                _db.Orders.Remove(order);
                _db.SaveChanges();
                transaction.Commit();

                Orders.Remove(Selected);
                Selected = null;
            }
            catch (DbUpdateException)
            {
                UiMessages.Error("Заказ не удалось удалить, потому что он связан с другими данными.");
            }
            catch (Exception ex)
            {
                UiMessages.Error($"Ошибка при удалении заказа: {ex.Message}");
            }
        }

        private async void LoadSelectedOrderDetailsAsync()
        {
            if (_isLoading) return;

            _isLoading = true;
            try
            {
                SelectedOrderServices.Clear();
                SelectedOrderServiceExtras.Clear();
                SelectedOrderMaterials.Clear();

                _selectedOrderService = null;
                OnPropertyChanged(nameof(SelectedOrderService));
                OnPropertyChanged(nameof(IsMeasurementsNeededForSelectedLine));
                OnPropertyChanged(nameof(SelectedLineMeasurementSummary));

                if (Selected == null) return;

                var lines = await _db.OrderServices
                    .AsNoTracking()
                    .Where(x => x.OrderId == Selected.Id)
                    .Include(x => x.Service)
                    .Include(x => x.Person)
                    .Include(x => x.MeasurementSet)
                    .Include(x => x.Extras).ThenInclude(e => e.ExtraElement)
                    .ToListAsync();

                double orderTotal = 0;

                foreach (var line in lines)
                {
                    double extrasSum = 0;
                    foreach (var ex in line.Extras)
                    {
                        ex.Total = ex.Price * ex.Qty;
                        extrasSum += ex.Total;
                    }

                    line.LineTotal = line.BasePrice + extrasSum;
                    orderTotal += line.LineTotal;

                    SelectedOrderServices.Add(line);
                }

                var mats = await _db.OrderMaterials
                    .AsNoTracking()
                    .Where(x => x.OrderId == Selected.Id)
                    .Include(x => x.Material)
                    .OrderBy(x => x.Material!.Name)
                    .ToListAsync();

                double materialsTotal = 0;
                foreach (var m in mats)
                {
                    if (System.Math.Abs(m.UnitPrice) < 0.000001 && m.Material != null)
                        m.UnitPrice = m.Material.Price;

                    m.Total = m.QtyUsed * m.UnitPrice;
                    materialsTotal += m.Total;
                    SelectedOrderMaterials.Add(m);
                }

                Selected.TotalCost = orderTotal + materialsTotal;
                OnPropertyChanged(nameof(Selected));
                OnPropertyChanged(nameof(HasSelectedOrderMaterials));
                OnPropertyChanged(nameof(HasSelectedOrderServiceExtras));

                SelectedOrderService = SelectedOrderServices.FirstOrDefault();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadSelectedServiceExtras()
        {
            SelectedOrderServiceExtras.Clear();

            if (SelectedOrderService?.Extras == null) return;

            foreach (var ex in SelectedOrderService.Extras)
                SelectedOrderServiceExtras.Add(ex);
        }
    }

    public class OrderEmployeeFilterItem
    {
        public int? Id { get; set; }
        public string FullName { get; set; } = "";
    }

}