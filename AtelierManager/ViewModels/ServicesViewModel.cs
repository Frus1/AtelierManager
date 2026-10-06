using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using AtelierManager.Data;
using AtelierManager.Models;
using AtelierManager.Views;
using Microsoft.EntityFrameworkCore;

namespace AtelierManager.ViewModels
{
    public class ServicesViewModel : BaseViewModel
    {
        private readonly AtelierContext _db = new();

        public ObservableCollection<ExtraElement> SelectedServiceExtras { get; } = new();

        private ExtraElement? _selectedExtraElement;
        public ExtraElement? SelectedExtraElement
        {
            get => _selectedExtraElement;
            set
            {
                _selectedExtraElement = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Service> Services { get; } = new();
        public ICollectionView ServicesView { get; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                ServicesView.Refresh();
            }
        }

        private string _selectedTypeFilter = "Все типы";
        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (_selectedTypeFilter == value) return;
                _selectedTypeFilter = value;
                OnPropertyChanged();
                ServicesView.Refresh();
            }
        }

        private string _selectedActivityFilter = "Все";
        public string SelectedActivityFilter
        {
            get => _selectedActivityFilter;
            set
            {
                if (_selectedActivityFilter == value) return;
                _selectedActivityFilter = value;
                OnPropertyChanged();
                ServicesView.Refresh();
            }
        }

        public ObservableCollection<string> ServiceTypeFilters { get; } = new() { "Все типы" };
        public ObservableCollection<string> ServiceActivityFilters { get; } = new()
        {
            "Все",
            "Активные",
            "Неактивные"
        };


        private Service? _selected;
        public Service? Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                OnPropertyChanged();
                LoadSelectedServiceExtras();
            }
        }

        private void LoadSelectedServiceExtras()
        {
            SelectedServiceExtras.Clear();
            SelectedExtraElement = null;

            if (Selected == null) return;

            var extras = _db.ExtraElements
                .AsNoTracking()
                .Where(x => x.ServiceId == Selected.Id)
                .OrderBy(x => x.Name)
                .ToList();

            foreach (var item in extras)
                SelectedServiceExtras.Add(item);
        }

        public ServicesViewModel()
        {
            ServicesView = CollectionViewSource.GetDefaultView(Services);
            ServicesView.Filter = FilterService;
            LoadAsync();
        }

        private bool FilterService(object obj)
        {
            if (obj is not Service service)
                return false;

            var search = SearchText?.Trim();
            var matchesSearch = string.IsNullOrWhiteSpace(search)
                || service.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || service.ServiceType.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (service.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || service.BasePrice.ToString("0.##").Contains(search, StringComparison.OrdinalIgnoreCase);

            var matchesType = SelectedTypeFilter == "Все типы"
                || string.Equals(service.ServiceType, SelectedTypeFilter, StringComparison.OrdinalIgnoreCase);

            var matchesActivity = SelectedActivityFilter switch
            {
                "Активные" => service.IsActive,
                "Неактивные" => !service.IsActive,
                _ => true
            };

            return matchesSearch && matchesType && matchesActivity;
        }

        private async void LoadAsync()
        {
            Services.Clear();

            var items = await _db.Services
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            var currentTypeFilter = SelectedTypeFilter;
            ServiceTypeFilters.Clear();
            ServiceTypeFilters.Add("Все типы");

            foreach (var type in items
                         .Select(x => x.ServiceType)
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Distinct()
                         .OrderBy(x => x))
            {
                ServiceTypeFilters.Add(type);
            }

            if (!ServiceTypeFilters.Contains(currentTypeFilter))
                SelectedTypeFilter = "Все типы";

            foreach (var item in items)
                Services.Add(item);

            ServicesView.Refresh();
            Selected = null;
        }

        public void Refresh() => LoadAsync();

        public void AddService()
        {
            var vm = new ServiceWindowViewModel();
            var dlg = new ServiceWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };

            if (dlg.ShowDialog() == true)
            {
                var service = new Service
                {
                    Name = vm.Name,
                    ServiceType = vm.ServiceType,
                    BasePrice = vm.BasePrice,
                    IsActive = vm.IsActive
                };

                _db.Services.Add(service);
                _db.SaveChanges();
                Refresh();
            }
        }

        public void EditSelectedService()
        {
            if (Selected == null) return;

            var vm = new ServiceWindowViewModel
            {
                Name = Selected.Name,
                ServiceType = Selected.ServiceType,
                BasePrice = Selected.BasePrice,
                IsActive = Selected.IsActive
            };

            var dlg = new ServiceWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };

            if (dlg.ShowDialog() == true)
            {
                var service = _db.Services.First(x => x.Id == Selected.Id);

                service.Name = vm.Name;
                service.ServiceType = vm.ServiceType;
                service.BasePrice = vm.BasePrice;
                service.IsActive = vm.IsActive;

                _db.SaveChanges();
                Refresh();
            }
        }

        public void DeleteSelectedService()
        {
            if (Selected == null) return;

            var usedInOrdersCount = _db.OrderServices
                .AsNoTracking()
                .Where(x => x.ServiceId == Selected.Id)
                .Select(x => x.OrderId)
                .Distinct()
                .Count();

            if (usedInOrdersCount > 0)
            {
                UiMessages.Validation(
                    $"Услугу \"{Selected.Name}\" нельзя удалить, так как она используется в заказах: {usedInOrdersCount}.\n\n" +
                    "Это ограничение сохраняет историю заказов и корректность отчётов. " +
                    "Если услуга больше не актуальна, отключите её через поле активности.");
                return;
            }

            if (!UiMessages.ConfirmDelete($"Удалить услугу \"{Selected.Name}\"?"))
                return;

            try
            {
                var service = _db.Services.First(x => x.Id == Selected.Id);
                _db.Services.Remove(service);
                _db.SaveChanges();

                Refresh();
            }
            catch (DbUpdateException)
            {
                UiMessages.Error("Услугу не удалось удалить, потому что она связана с другими данными.");
            }
        }

        public void AddExtraElement()
        {
            if (Selected == null)
            {
                UiMessages.Validation("Сначала выберите услугу.");
                return;
            }

            var vm = new ExtraElementWindowViewModel();
            var dlg = new ExtraElementWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };

            if (dlg.ShowDialog() == true)
            {
                var extra = new ExtraElement
                {
                    ServiceId = Selected.Id,
                    Name = vm.Name,
                    DefaultPrice = vm.DefaultPrice,
                    IsActive = vm.IsActive
                };

                _db.ExtraElements.Add(extra);
                _db.SaveChanges();
                LoadSelectedServiceExtras();
            }
        }

        public void EditSelectedExtraElement()
        {
            if (SelectedExtraElement == null) return;

            var vm = new ExtraElementWindowViewModel
            {
                Name = SelectedExtraElement.Name,
                DefaultPrice = SelectedExtraElement.DefaultPrice,
                IsActive = SelectedExtraElement.IsActive
            };

            var dlg = new ExtraElementWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };

            if (dlg.ShowDialog() == true)
            {
                var extra = _db.ExtraElements.First(x => x.Id == SelectedExtraElement.Id);
                extra.Name = vm.Name;
                extra.DefaultPrice = vm.DefaultPrice;
                extra.IsActive = vm.IsActive;

                _db.SaveChanges();
                LoadSelectedServiceExtras();
            }
        }

        public void DeleteSelectedExtraElement()
        {
            if (SelectedExtraElement == null) return;

            var usedInOrdersCount = _db.OrderServiceExtras
                .AsNoTracking()
                .Where(x => x.ExtraElementId == SelectedExtraElement.Id)
                .Select(x => x.OrderService!.OrderId)
                .Distinct()
                .Count();

            if (usedInOrdersCount > 0)
            {
                UiMessages.Validation(
                    $"Доп. элемент \"{SelectedExtraElement.Name}\" нельзя удалить, так как он используется в заказах: {usedInOrdersCount}.\n\n" +
                    "Это ограничение сохраняет историю заказов и корректность отчётов. " +
                    "Если элемент больше не актуален, отключите его через поле активности.");
                return;
            }

            if (!UiMessages.ConfirmDelete($"Удалить доп. элемент \"{SelectedExtraElement.Name}\"?"))
                return;

            try
            {
                var extra = _db.ExtraElements.First(x => x.Id == SelectedExtraElement.Id);
                _db.ExtraElements.Remove(extra);
                _db.SaveChanges();

                LoadSelectedServiceExtras();
            }
            catch (DbUpdateException)
            {
                UiMessages.Error("Доп. элемент не удалось удалить, потому что он связан с другими данными.");
            }
        }
    }
}