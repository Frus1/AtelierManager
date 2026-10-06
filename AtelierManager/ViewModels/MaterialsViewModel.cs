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
    public class MaterialsViewModel : BaseViewModel
    {
        private readonly AtelierContext _db = new();
        public ICollectionView MaterialsView { get; }
        public ObservableCollection<Material> Materials { get; } = new();

        public bool HasSelectedMaterial => Selected != null;

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                MaterialsView.Refresh();
            }
        }

        private string _selectedFilter = "Все";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                _selectedFilter = value;
                OnPropertyChanged(nameof(SelectedFilter));
                MaterialsView.Refresh();
            }
        }
        public ObservableCollection<string> MaterialFilters { get; } = new()
        {
            "Все",
            "Есть в наличии",
            "Закончились",
            "Мало осталось"
        };

        private Material? _selected;
        public Material? Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedMaterial));
            }
        }

        public MaterialsViewModel()
        {
            LoadAsync();
            MaterialsView = CollectionViewSource.GetDefaultView(Materials);
            MaterialsView.Filter = FilterMaterial;
        }

        private bool FilterMaterial(object obj)
        {
            if (obj is not Material material)
                return false;

            bool matchesSearch = string.IsNullOrWhiteSpace(SearchText)
                || material.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || material.Unit.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

            bool matchesFilter = SelectedFilter switch
            {
                "Есть в наличии" => material.Quantity > 0,
                "Закончились" => material.Quantity <= 0,
                "Мало осталось" => material.Quantity > 0 && material.Quantity <= 5,
                _ => true
            };

            return matchesSearch && matchesFilter;
        }

        private async void LoadAsync()
        {
            Materials.Clear();

            var items = await _db.Materials
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            foreach (var item in items)
                Materials.Add(item);

            Selected = null;
        }

        public void Refresh() => LoadAsync();

        public void AddMaterial()
        {
            var vm = new MaterialWindowViewModel();
            var dlg = new MaterialWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };

            if (dlg.ShowDialog() == true)
            {
                var material = new Material
                {
                    Name = vm.Name,
                    Quantity = vm.Quantity,
                    Unit = vm.Unit,
                    Price = vm.Price
                };

                _db.Materials.Add(material);
                _db.SaveChanges();
                Refresh();
            }
        }

        public void EditSelectedMaterial()
        {
            if (Selected == null) return;

            var vm = new MaterialWindowViewModel
            {
                Name = Selected.Name,
                Quantity = Selected.Quantity,
                Unit = Selected.Unit,
                Price = Selected.Price
            };

            var dlg = new MaterialWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };

            if (dlg.ShowDialog() == true)
            {
                var material = _db.Materials.First(x => x.Id == Selected.Id);
                material.Name = vm.Name;
                material.Quantity = vm.Quantity;
                material.Unit = vm.Unit;
                material.Price = vm.Price;

                _db.SaveChanges();
                Refresh();
            }
        }

        public void DeleteSelectedMaterial()
        {
            if (Selected == null) return;

            var usedInOrdersCount = _db.OrderMaterials
                .AsNoTracking()
                .Where(x => x.MaterialId == Selected.Id)
                .Select(x => x.OrderId)
                .Distinct()
                .Count();

            if (usedInOrdersCount > 0)
            {
                UiMessages.Validation(
                    $"Материал \"{Selected.Name}\" нельзя удалить, так как он используется в заказах: {usedInOrdersCount}.\n\n" +
                    "Это ограничение сохраняет историю заказов и корректность отчётов. " +
                    "Если материал больше не используется, измените его количество или переименуйте запись.");
                return;
            }

            if (!UiMessages.ConfirmDelete($"Удалить материал \"{Selected.Name}\"?"))
                return;

            try
            {
                var material = _db.Materials.First(x => x.Id == Selected.Id);
                _db.Materials.Remove(material);
                _db.SaveChanges();

                Refresh();
            }
            catch (DbUpdateException)
            {
                UiMessages.Error("Материал не удалось удалить, потому что он связан с другими данными.");
            }
        }
    }
}