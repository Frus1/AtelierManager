using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AtelierManager.Data;
using AtelierManager.Models;
using AtelierManager.Services;

namespace AtelierManager.ViewModels
{
    public class OrderWindowViewModel : BaseViewModel
    {
        private readonly AtelierContext _db;

        public ObservableCollection<Client> Clients { get; } = new();
        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<Status> Statuses { get; } = new();
        public ObservableCollection<Service> Services { get; } = new();
        public ObservableCollection<MeasurementSet> MeasurementSets { get; } = new();
        public ObservableCollection<MeasurementSet> SelectedLineMeasurementSets { get; } = new();
        public ObservableCollection<OrderService> Lines { get; } = new();
        public ObservableCollection<Material> Materials { get; } = new();
        public ObservableCollection<OrderMaterial> OrderMaterialsLines { get; } = new();

        public ObservableCollection<string> MaterialSources { get; } =
            new ObservableCollection<string> { "Клиент", "Ателье", "Смешанное" };

        private Service? _selectedServiceToAdd;
        public Service? SelectedServiceToAdd
        {
            get => _selectedServiceToAdd;
            set
            {
                _selectedServiceToAdd = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMeasurementsRequiredForNewLine));
                ReloadMeasurementSetsForNewLine();
            }
        }

        private Person? _selectedPersonToAdd;
        public Person? SelectedPersonToAdd
        {
            get => _selectedPersonToAdd;
            set
            {
                _selectedPersonToAdd = value;
                OnPropertyChanged();
                ReloadMeasurementSetsForNewLine();
            }
        }

        private MeasurementSet? _selectedMeasurementToAdd;
        public MeasurementSet? SelectedMeasurementToAdd
        {
            get => _selectedMeasurementToAdd;
            set { _selectedMeasurementToAdd = value; OnPropertyChanged(); }
        }

        private MeasurementSet? _selectedLineMeasurementSet;
        public MeasurementSet? SelectedLineMeasurementSet
        {
            get => _selectedLineMeasurementSet;
            set
            {
                _selectedLineMeasurementSet = value;
                OnPropertyChanged();

                if (SelectedLine != null)
                {
                    SelectedLine.MeasurementSet = value;
                    SelectedLine.MeasurementSetId = value?.Id;
                }
            }
        }

        public bool IsMeasurementsRequiredForSelectedLine => IsSewingService(SelectedLine?.Service);
        public bool IsMeasurementsRequiredForNewLine => IsSewingService(SelectedServiceToAdd);

        private OrderService? _selectedLine;
        public OrderService? SelectedLine
        {
            get => _selectedLine;
            set
            {
                _selectedLine = value;
                OnPropertyChanged();
                LoadAvailableExtras();
                RefreshSelectedExtras();
                ReloadMeasurementSetsForSelectedLine();
                OnPropertyChanged(nameof(IsMeasurementsRequiredForSelectedLine));
            }
        }

        public ObservableCollection<ExtraElement> AvailableExtrasForSelectedLine { get; } = new();

        private ExtraElement? _selectedExtraToAdd;
        public ExtraElement? SelectedExtraToAdd
        {
            get => _selectedExtraToAdd;
            set { _selectedExtraToAdd = value; OnPropertyChanged(); }
        }

        private OrderServiceExtra? _selectedExtra;
        public OrderServiceExtra? SelectedExtra
        {
            get => _selectedExtra;
            set { _selectedExtra = value; OnPropertyChanged(); }
        }

        private ObservableCollection<OrderServiceExtra> _selectedExtras = new();
        public ObservableCollection<OrderServiceExtra> SelectedExtras
        {
            get => _selectedExtras;
            private set { _selectedExtras = value; OnPropertyChanged(); }
        }

        private Client? _selectedClient;
        public Client? SelectedClient
        {
            get => _selectedClient;
            set
            {
                _selectedClient = value;
                OnPropertyChanged();
                LoadPersonsForClient();
            }
        }

        private Status? _selectedStatus;
        public Status? SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); }
        }

        private string _materialSource = "Клиент";
        public string MaterialSource
        {
            get => _materialSource;
            set
            {
                _materialSource = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NeedsAtelierMaterials));
                RecalcTotals();
            }
        }

        private OrderMaterial? _selectedMaterialLine;
        public OrderMaterial? SelectedMaterialLine
        {
            get => _selectedMaterialLine;
            set { _selectedMaterialLine = value; OnPropertyChanged(); }
        }

        public DateTime IntakeDate { get; set; } = DateTime.Today;
        public DateTime? DueDate { get; set; }
        public string? MaterialComment { get; set; }
        public string? Notes { get; set; }

        private double _totalCost;
        public double TotalCost
        {
            get => _totalCost;
            set { _totalCost = value; OnPropertyChanged(); }
        }

        public bool NeedsAtelierMaterials => MaterialSource == "Ателье" || MaterialSource == "Смешанное";
        public Order? EditingOrder { get; }

        public bool IsLimitedWorkerEdit => EditingOrder != null && CurrentUserService.IsWorker && !CurrentUserService.IsAdmin;
        public bool CanEditMainOrderData => !IsLimitedWorkerEdit;
        public string AccessModeHint => IsLimitedWorkerEdit
            ? "Режим работника: доступны статус, услуги, дополнительные элементы и материалы заказа. Клиент, даты и примечание защищены от изменения."
            : "";

        public OrderWindowViewModel(AtelierContext db, Order? editingOrder = null)
        {
            _db = db;
            EditingOrder = editingOrder;

            LoadLookups();

            if (editingOrder != null)
                LoadFromOrder(editingOrder);

            RecalcTotals();
        }

        private static bool IsSewingService(Service? service)
        {
            return (service?.ServiceType ?? "")
                .Trim()
                .StartsWith("Пошив", StringComparison.OrdinalIgnoreCase);
        }

        private void LoadLookups()
        {
            Clients.Clear();
            foreach (var c in _db.Clients.AsNoTracking().OrderBy(x => x.FullName))
                Clients.Add(c);

            Statuses.Clear();
            foreach (var s in _db.Statuses.AsNoTracking().OrderBy(x => x.SortOrder))
                Statuses.Add(s);

            Services.Clear();
            foreach (var s in _db.Services.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name))
                Services.Add(s);

            Materials.Clear();
            foreach (var m in _db.Materials.AsNoTracking().OrderBy(x => x.Name))
                Materials.Add(m);

            SelectedServiceToAdd = Services.FirstOrDefault();
            OnPropertyChanged(nameof(IsMeasurementsRequiredForNewLine));
            ReloadMeasurementSetsForNewLine();
        }

        private void LoadFromOrder(Order o)
        {
            SelectedClient = Clients.FirstOrDefault(x => x.Id == o.ClientId);
            SelectedStatus = Statuses.FirstOrDefault(x => x.Id == o.StatusId);

            IntakeDate = o.IntakeDate;
            DueDate = o.DueDate;
            MaterialSource = string.IsNullOrWhiteSpace(o.MaterialSource) ? "Клиент" : o.MaterialSource;
            MaterialComment = o.MaterialComment;
            Notes = o.Notes;

            Lines.Clear();
            var lines = _db.OrderServices.AsNoTracking()
                .Where(x => x.OrderId == o.Id)
                .Include(x => x.Service)
                .Include(x => x.Person)
                .Include(x => x.MeasurementSet)
                .Include(x => x.Extras).ThenInclude(e => e.ExtraElement)
                .ToList();

            foreach (var l in lines)
                Lines.Add(l);

            OrderMaterialsLines.Clear();
            var mats = _db.OrderMaterials.AsNoTracking()
                .Where(x => x.OrderId == o.Id)
                .Include(x => x.Material)
                .ToList();

            foreach (var m in mats)
            {
                FixMaterialLine(m);
                OrderMaterialsLines.Add(m);
            }

            SelectedLine = Lines.FirstOrDefault();
            RecalcTotals();

            OnPropertyChanged(nameof(IsLimitedWorkerEdit));
            OnPropertyChanged(nameof(CanEditMainOrderData));
            OnPropertyChanged(nameof(AccessModeHint));
        }

        private void LoadPersonsForClient()
        {
            Persons.Clear();
            MeasurementSets.Clear();
            SelectedPersonToAdd = null;
            SelectedMeasurementToAdd = null;

            if (SelectedClient == null) return;

            foreach (var p in _db.Persons.AsNoTracking()
                         .Where(x => x.ClientId == SelectedClient.Id)
                         .OrderBy(x => x.DisplayName))
                Persons.Add(p);

            SelectedPersonToAdd = Persons.FirstOrDefault();
            ReloadMeasurementSetsForNewLine();
        }

        private void ReloadMeasurementSetsForNewLine()
        {
            MeasurementSets.Clear();
            SelectedMeasurementToAdd = null;

            if (SelectedPersonToAdd == null) return;

            foreach (var m in _db.MeasurementSets.AsNoTracking()
                         .Where(x => x.PersonId == SelectedPersonToAdd.Id)
                         .OrderByDescending(x => x.TakenAt))
                MeasurementSets.Add(m);

            SelectedMeasurementToAdd = MeasurementSets.FirstOrDefault();
        }

        public void AddLine()
        {
            if (SelectedServiceToAdd == null || SelectedPersonToAdd == null)
                return;

            if (IsSewingService(SelectedServiceToAdd) && SelectedMeasurementToAdd == null)
                return;

            var svc = SelectedServiceToAdd;

            var line = new OrderService
            {
                ServiceId = svc.Id,
                Service = svc,
                PersonId = SelectedPersonToAdd.Id,
                Person = SelectedPersonToAdd,
                MeasurementSetId = IsSewingService(svc) ? SelectedMeasurementToAdd?.Id : null,
                MeasurementSet = IsSewingService(svc) ? SelectedMeasurementToAdd : null,
                BasePrice = svc.BasePrice,
                LineTotal = svc.BasePrice
            };

            Lines.Add(line);
            SelectedLine = line;
            RecalcTotals();
        }

        public void DeleteSelectedLine()
        {
            if (SelectedLine == null) return;
            Lines.Remove(SelectedLine);
            SelectedLine = Lines.FirstOrDefault();
            RecalcTotals();
        }

        public void AddMaterialLine()
        {
            var first = Materials.FirstOrDefault();
            if (first == null) return;

            var line = new OrderMaterial
            {
                MaterialId = first.Id,
                Material = first,
                QtyUsed = 1,
                UnitPrice = first.Price
            };

            FixMaterialLine(line);
            OrderMaterialsLines.Add(line);
            SelectedMaterialLine = line;
            RecalcTotals();
        }

        public void DeleteSelectedMaterialLine()
        {
            if (SelectedMaterialLine == null) return;
            OrderMaterialsLines.Remove(SelectedMaterialLine);
            SelectedMaterialLine = null;
            RecalcTotals();
        }

        public void RefreshMaterialsAndTotals()
        {
            RecalcTotals();
        }

        private void NormalizeMaterialLines()
        {
            foreach (var line in OrderMaterialsLines)
                FixMaterialLine(line);
        }

        private void FixMaterialLine(OrderMaterial line)
        {
            var materialWasChanged = line.Material == null || line.Material.Id != line.MaterialId;
            var mat = Materials.FirstOrDefault(m => m.Id == line.MaterialId)
                      ?? _db.Materials.AsNoTracking().FirstOrDefault(m => m.Id == line.MaterialId);

            line.Material = mat;

            if (materialWasChanged || System.Math.Abs(line.UnitPrice) < 0.000001)
                line.UnitPrice = mat?.Price ?? 0;

            line.Total = line.QtyUsed * line.UnitPrice;
        }

        private Dictionary<int, double> GetOldMaterialQuantities()
        {
            if (EditingOrder == null)
                return new Dictionary<int, double>();

            return _db.OrderMaterials
                .AsNoTracking()
                .Where(x => x.OrderId == EditingOrder.Id)
                .GroupBy(x => x.MaterialId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.QtyUsed));
        }

        private Dictionary<int, double> GetNewMaterialQuantities()
        {
            if (!NeedsAtelierMaterials)
                return new Dictionary<int, double>();

            return OrderMaterialsLines
                .Where(x => x.MaterialId > 0 && x.QtyUsed > 0)
                .GroupBy(x => x.MaterialId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.QtyUsed));
        }

        private bool TryValidateMaterialAvailability(out string error)
        {
            NormalizeMaterialLines();

            var newQuantities = GetNewMaterialQuantities();
            var oldQuantities = GetOldMaterialQuantities();

            foreach (var item in newQuantities)
            {
                var material = _db.Materials.AsNoTracking().FirstOrDefault(x => x.Id == item.Key);
                if (material == null)
                {
                    error = "Один из выбранных материалов не найден в справочнике.";
                    return false;
                }

                var oldQty = oldQuantities.TryGetValue(item.Key, out var oldValue) ? oldValue : 0;
                var availableQty = material.Quantity + oldQty;

                if (item.Value > availableQty + 0.000001)
                {
                    error = $"Недостаточно материала «{material.Name}». Доступно: {availableQty:0.##} {material.Unit}, требуется: {item.Value:0.##} {material.Unit}.";
                    return false;
                }
            }

            error = "";
            return true;
        }

        private void RefreshSelectedExtras()
        {
            if (SelectedLine?.Extras == null)
            {
                SelectedExtras = new ObservableCollection<OrderServiceExtra>();
                return;
            }

            SelectedExtras = new ObservableCollection<OrderServiceExtra>(SelectedLine.Extras);
        }

        private void LoadAvailableExtras()
        {
            AvailableExtrasForSelectedLine.Clear();
            SelectedExtraToAdd = null;

            if (SelectedLine == null || SelectedLine.ServiceId == 0)
                return;

            var all = _db.ExtraElements.AsNoTracking()
                .Where(e => e.ServiceId == SelectedLine.ServiceId && e.IsActive)
                .OrderBy(e => e.Name)
                .ToList();

            var usedIds = SelectedLine.Extras?
                .Select(x => x.ExtraElementId)
                .ToHashSet() ?? new HashSet<int>();

            foreach (var e in all)
                if (!usedIds.Contains(e.Id))
                    AvailableExtrasForSelectedLine.Add(e);

            SelectedExtraToAdd = AvailableExtrasForSelectedLine.FirstOrDefault();
        }

        public void AddExtra()
        {
            if (SelectedLine == null) return;

            var extra = SelectedExtraToAdd ?? AvailableExtrasForSelectedLine.FirstOrDefault();
            if (extra == null) return;

            var row = new OrderServiceExtra
            {
                ExtraElementId = extra.Id,
                ExtraElement = extra,
                Qty = 1,
                Price = extra.DefaultPrice,
                Total = extra.DefaultPrice
            };

            SelectedLine.Extras ??= new List<OrderServiceExtra>();
            SelectedLine.Extras.Add(row);
            SelectedExtras.Add(row);

            SelectedExtra = row;
            LoadAvailableExtras();
            RecalcTotals();
        }

        public void DeleteSelectedExtra()
        {
            if (SelectedLine == null || SelectedExtra == null) return;

            SelectedLine.Extras?.Remove(SelectedExtra);
            SelectedExtras.Remove(SelectedExtra);

            SelectedExtra = null;
            LoadAvailableExtras();
            RecalcTotals();
        }

        private void RecalcTotals()
        {
            foreach (var l in Lines)
            {
                double extrasSum = 0;

                if (l.Extras != null)
                {
                    foreach (var ex in l.Extras)
                    {
                        var qty = ex.Qty <= 0 ? 1 : ex.Qty;
                        ex.Total = ex.Price * qty;
                        extrasSum += ex.Total;
                    }
                }

                l.LineTotal = l.BasePrice + extrasSum;
            }

            if (NeedsAtelierMaterials)
                NormalizeMaterialLines();

            var servicesTotal = Lines.Sum(x => x.LineTotal);
            var materialsTotal = NeedsAtelierMaterials
                ? OrderMaterialsLines.Sum(m => m.Total)
                : 0;

            TotalCost = servicesTotal + materialsTotal;
        }

        public bool Validate(out string error)
        {
            RecalcTotals();

            if (SelectedClient == null)
            {
                error = "Выберите клиента.";
                return false;
            }

            if (SelectedStatus == null)
            {
                error = "Выберите статус.";
                return false;
            }

            if (DueDate != null && DueDate.Value.Date < IntakeDate.Date)
            {
                error = "Срок готовности не может быть раньше даты приёма.";
                return false;
            }

            if (Lines.Count == 0)
            {
                error = "Добавьте хотя бы одну услугу (позицию).";
                return false;
            }

            foreach (var l in Lines)
            {
                if (l.ServiceId <= 0)
                {
                    error = "В одной из позиций не выбрана услуга.";
                    return false;
                }

                if (l.PersonId <= 0)
                {
                    error = "В одной из позиций не выбрана персона.";
                    return false;
                }

                if (l.BasePrice < 0)
                {
                    error = "Цена услуги не может быть отрицательной.";
                    return false;
                }

                if (l.LineTotal < 0)
                {
                    error = "Итог по позиции не может быть отрицательным.";
                    return false;
                }

                var svc = l.Service ?? Services.FirstOrDefault(s => s.Id == l.ServiceId);
                if (IsSewingService(svc) && l.MeasurementSetId == null)
                {
                    error = "Для позиции с пошивом нужно выбрать мерки.";
                    return false;
                }
            }

            if (MaterialSource == "Клиент" && string.IsNullOrWhiteSpace(MaterialComment))
            {
                error = "Укажите материал клиента или комментарий.";
                return false;
            }

            if (NeedsAtelierMaterials)
            {
                if (OrderMaterialsLines.Count == 0)
                {
                    error = "Добавьте материалы ателье или смените источник материала.";
                    return false;
                }

                foreach (var m in OrderMaterialsLines)
                {
                    if (m.MaterialId <= 0)
                    {
                        error = "Есть материал без выбранного наименования.";
                        return false;
                    }

                    if (m.QtyUsed <= 0)
                    {
                        error = "Количество материала должно быть больше нуля.";
                        return false;
                    }

                    if (m.UnitPrice < 0)
                    {
                        error = "Цена материала не может быть отрицательной.";
                        return false;
                    }

                    if (m.Total < 0)
                    {
                        error = "Сумма материала не может быть отрицательной.";
                        return false;
                    }
                }

                if (!TryValidateMaterialAvailability(out error))
                    return false;
            }

            error = "";
            return true;
        }

        public void Save()
        {
            if (SelectedClient == null || SelectedStatus == null)
                throw new InvalidOperationException("Не заполнены обязательные поля.");

            RecalcTotals();

            if (NeedsAtelierMaterials && !TryValidateMaterialAvailability(out var stockError))
                throw new InvalidOperationException(stockError);

            var oldMaterialQuantities = GetOldMaterialQuantities();
            var newMaterialQuantities = GetNewMaterialQuantities();

            using var transaction = _db.Database.BeginTransaction();

            Order order;
            if (EditingOrder == null)
            {
                order = new Order();
                _db.Orders.Add(order);
            }
            else
            {
                order = _db.Orders.First(x => x.Id == EditingOrder.Id);
            }

            order.ClientId = SelectedClient.Id;
            order.StatusId = SelectedStatus.Id;
            if (EditingOrder == null)
                order.EmployeeId = CurrentUserService.CurrentUser?.Id;
            order.IntakeDate = IntakeDate;
            order.DueDate = DueDate;
            order.MaterialSource = MaterialSource;
            order.MaterialComment = MaterialComment;
            order.Notes = Notes;
            order.TotalCost = TotalCost;

            _db.SaveChanges();

            if (EditingOrder != null)
            {
                var oldExtras = _db.OrderServiceExtras
                    .Where(x => _db.OrderServices.Any(os => os.Id == x.OrderServiceId && os.OrderId == order.Id))
                    .ToList();
                _db.OrderServiceExtras.RemoveRange(oldExtras);

                var oldLines = _db.OrderServices.Where(x => x.OrderId == order.Id).ToList();
                _db.OrderServices.RemoveRange(oldLines);

                var oldMats = _db.OrderMaterials.Where(x => x.OrderId == order.Id).ToList();
                _db.OrderMaterials.RemoveRange(oldMats);

                _db.SaveChanges();
            }

            foreach (var materialId in oldMaterialQuantities.Keys.Union(newMaterialQuantities.Keys))
            {
                var oldQty = oldMaterialQuantities.TryGetValue(materialId, out var oldValue) ? oldValue : 0;
                var newQty = newMaterialQuantities.TryGetValue(materialId, out var newValue) ? newValue : 0;
                var delta = newQty - oldQty;

                if (System.Math.Abs(delta) < 0.000001)
                    continue;

                var material = _db.Materials.FirstOrDefault(x => x.Id == materialId);
                if (material == null)
                    throw new InvalidOperationException("Один из материалов заказа не найден в справочнике.");

                material.Quantity -= delta;

                if (material.Quantity < -0.000001)
                    throw new InvalidOperationException($"Недостаточно материала «{material.Name}» на складе.");
            }

            foreach (var l in Lines)
            {
                var line = new OrderService
                {
                    OrderId = order.Id,
                    ServiceId = l.ServiceId,
                    PersonId = l.PersonId,
                    MeasurementSetId = l.MeasurementSetId,
                    BasePrice = l.BasePrice,
                    LineTotal = l.LineTotal
                };

                _db.OrderServices.Add(line);
                _db.SaveChanges();

                if (l.Extras != null)
                {
                    foreach (var ex in l.Extras)
                    {
                        var qty = ex.Qty <= 0 ? 1 : ex.Qty;
                        var row = new OrderServiceExtra
                        {
                            OrderServiceId = line.Id,
                            ExtraElementId = ex.ExtraElementId,
                            Qty = qty,
                            Price = ex.Price,
                            Total = ex.Price * qty
                        };
                        _db.OrderServiceExtras.Add(row);
                    }
                    _db.SaveChanges();
                }
            }

            if (NeedsAtelierMaterials)
            {
                foreach (var m in OrderMaterialsLines)
                {
                    FixMaterialLine(m);

                    var row = new OrderMaterial
                    {
                        OrderId = order.Id,
                        MaterialId = m.MaterialId,
                        QtyUsed = m.QtyUsed,
                        UnitPrice = m.UnitPrice,
                        Total = m.QtyUsed * m.UnitPrice
                    };
                    _db.OrderMaterials.Add(row);
                }
            }

            order.TotalCost = TotalCost;
            _db.SaveChanges();
            transaction.Commit();
        }

        private void ReloadMeasurementSetsForSelectedLine()
        {
            SelectedLineMeasurementSets.Clear();
            SelectedLineMeasurementSet = null;

            if (SelectedLine == null) return;
            if (SelectedLine.PersonId <= 0) return;

            var list = _db.MeasurementSets.AsNoTracking()
                .Where(x => x.PersonId == SelectedLine.PersonId)
                .OrderByDescending(x => x.TakenAt)
                .ToList();

            foreach (var m in list)
                SelectedLineMeasurementSets.Add(m);

            if (SelectedLine.MeasurementSetId != null)
            {
                SelectedLineMeasurementSet = SelectedLineMeasurementSets
                    .FirstOrDefault(x => x.Id == SelectedLine.MeasurementSetId.Value);
            }
            else
            {
                SelectedLineMeasurementSet = SelectedLineMeasurementSets.FirstOrDefault();
                if (IsMeasurementsRequiredForSelectedLine && SelectedLineMeasurementSet != null)
                {
                    SelectedLine.MeasurementSetId = SelectedLineMeasurementSet.Id;
                    SelectedLine.MeasurementSet = SelectedLineMeasurementSet;
                }
            }
        }
    }
}
