using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using AtelierManager;
using AtelierManager.Data;
using AtelierManager.Models;
using AtelierManager.Services;
using AtelierManager.Views;
using Microsoft.EntityFrameworkCore;

namespace AtelierManager.ViewModels
{
    public class ClientsViewModel : BaseViewModel
    {
        private readonly AtelierContext _db = new();

        public ObservableCollection<Client> Clients { get; } = new();
        public ObservableCollection<Person> ClientPersons { get; } = new();
        public ObservableCollection<MeasurementSet> SelectedPersonMeasurementSets { get; } = new();
        public bool HasSelectedClient => Selected != null;
        public bool HasSelectedPerson => SelectedPerson != null;
        public bool HasSelectedMeasurementSet => SelectedMeasurementSet != null;
        public bool CanManageClientData => CurrentUserService.IsAdmin;
        public Visibility ClientManagementVisibility => CanManageClientData ? Visibility.Visible : Visibility.Collapsed;
        public bool HasClientPersons => ClientPersons.Count > 0;
        public bool HasSelectedPersonMeasurements => SelectedPersonMeasurementSets.Count > 0;

        private Client? _selected;
        public Client? Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedClient));
                LoadPersonsForSelectedClient();
            }
        }

        private Person? _selectedPerson;
        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedPerson));
                LoadMeasurementsForSelectedPerson();
            }
        }

        private MeasurementSet? _selectedMeasurementSet;
        public MeasurementSet? SelectedMeasurementSet
        {
            get => _selectedMeasurementSet;
            set
            {
                _selectedMeasurementSet = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedMeasurementSet));
            }
        }

        private string _search = "";
        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                OnPropertyChanged();
                LoadAsync();
            }
        }

        private string _selectedFilter = "Все клиенты";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (_selectedFilter == value) return;
                _selectedFilter = value;
                OnPropertyChanged();
                LoadAsync();
            }
        }

        public ObservableCollection<string> ClientFilters { get; } = new()
        {
            "Все клиенты",
            "С заказами",
            "Без заказов",
            "Есть телефон",
            "Есть email"
        };

        public ClientsViewModel()
        {
            LoadAsync();
        }

        public async void LoadAsync()
        {
            Clients.Clear();

            var query = _db.Clients.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                query = query.Where(c =>
                    EF.Functions.Like(c.FullName, $"%{Search}%") ||
                    EF.Functions.Like(c.Phone ?? "", $"%{Search}%") ||
                    EF.Functions.Like(c.Email ?? "", $"%{Search}%") ||
                    _db.Persons.Any(p => p.ClientId == c.Id &&
                        (EF.Functions.Like(p.DisplayName, $"%{Search}%") ||
                         EF.Functions.Like(p.Gender ?? "", $"%{Search}%"))));
            }

            query = SelectedFilter switch
            {
                "С заказами" => query.Where(c => _db.Orders.Any(o => o.ClientId == c.Id)),
                "Без заказов" => query.Where(c => !_db.Orders.Any(o => o.ClientId == c.Id)),
                "Есть телефон" => query.Where(c => c.Phone != null && c.Phone != ""),
                "Есть email" => query.Where(c => c.Email != null && c.Email != ""),
                _ => query
            };

            var list = await query.OrderByDescending(x => x.Id).ToListAsync();
            foreach (var c in list)
                Clients.Add(c);

            if (Selected != null)
                Selected = Clients.FirstOrDefault(x => x.Id == Selected.Id);
        }

        private void LoadPersonsForSelectedClient()
        {
            ClientPersons.Clear();
            SelectedPersonMeasurementSets.Clear();
            SelectedPerson = null;
            SelectedMeasurementSet = null;

            if (Selected == null) return;

            var persons = _db.Persons.AsNoTracking()
                .Where(p => p.ClientId == Selected.Id)
                .OrderBy(p => p.DisplayName)
                .ToList();

            foreach (var p in persons)
                ClientPersons.Add(p);

            SelectedPerson = ClientPersons.FirstOrDefault();
            OnPropertyChanged(nameof(HasClientPersons));
            OnPropertyChanged(nameof(HasSelectedPerson));
            OnPropertyChanged(nameof(HasSelectedMeasurementSet));

        }

        private void LoadMeasurementsForSelectedPerson()
        {
            SelectedPersonMeasurementSets.Clear();
            SelectedMeasurementSet = null;

            if (SelectedPerson == null) return;

            var sets = _db.MeasurementSets.AsNoTracking()
                .Where(m => m.PersonId == SelectedPerson.Id)
                .OrderByDescending(m => m.TakenAt)
                .ToList();

            foreach (var m in sets)
                SelectedPersonMeasurementSets.Add(m);

            SelectedMeasurementSet = SelectedPersonMeasurementSets.FirstOrDefault();
            OnPropertyChanged(nameof(HasSelectedPersonMeasurements));
            OnPropertyChanged(nameof(HasSelectedMeasurementSet));
        }

        private bool EnsureCanManageClientData()
        {
            if (CanManageClientData)
                return true;

            UiMessages.Validation("Недостаточно прав. Работник может просматривать клиентов, персон и мерки, но не может изменять эти данные.");
            return false;
        }

        public void Add()
        {
            if (!EnsureCanManageClientData()) return;

            var client = new Client();

            var dialog = new ClientWindow(client)
            {
                Owner = Application.Current.MainWindow
            };

            var result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    _db.Clients.Add(client);
                    _db.SaveChanges();

                    Clients.Insert(0, client);
                    Selected = client;
                }
                catch (Exception ex)
                {
                    UiMessages.Error($"Ошибка при добавлении клиента: {ex.Message}");
                }
            }
        }

        public void DeleteSelected()
        {
            if (!EnsureCanManageClientData()) return;

            if (Selected == null)
                return;

            var ordersCount = _db.Orders
                .AsNoTracking()
                .Count(x => x.ClientId == Selected.Id);

            if (ordersCount > 0)
            {
                UiMessages.Validation(
                    $"Клиента \"{Selected.FullName}\" нельзя удалить, так как у него есть заказы: {ordersCount}.\n\n" +
                    "Это ограничение сохраняет историю заказов и корректность отчётов.");
                return;
            }

            if (!UiMessages.ConfirmDelete("Удалить выбранного клиента? Вместе с клиентом будут удалены его персоны и наборы мерок."))
                return;

            try
            {
                var client = _db.Clients.First(x => x.Id == Selected.Id);
                _db.Clients.Remove(client);
                _db.SaveChanges();

                Clients.Remove(Selected);
                Selected = null;
            }
            catch (DbUpdateException)
            {
                UiMessages.Error("Клиента не удалось удалить, потому что он связан с другими данными.");
            }
            catch (Exception ex)
            {
                UiMessages.Error($"Ошибка при удалении клиента: {ex.Message}");
            }
        }

        public void EditSelectedClient()
        {
            if (!EnsureCanManageClientData()) return;

            if (Selected == null) return;

            var clientFromDb = _db.Clients.First(x => x.Id == Selected.Id);

            var copy = new Client
            {
                Id = clientFromDb.Id,
                FullName = clientFromDb.FullName,
                Phone = clientFromDb.Phone,
                Email = clientFromDb.Email,
                CreatedAt = clientFromDb.CreatedAt
            };

            var dialog = new ClientWindow(copy)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                clientFromDb.FullName = copy.FullName;
                clientFromDb.Phone = copy.Phone;
                clientFromDb.Email = copy.Email;

                _db.SaveChanges();
                LoadAsync();
            }
        }

        public void AddPerson()
        {
            if (!EnsureCanManageClientData()) return;

            if (Selected == null)
            {
                UiMessages.Validation("Сначала выберите клиента.");
                return;
            }

            var person = new Person
            {
                ClientId = Selected.Id,
                DisplayName = "Персона"
            };

            var dialog = new PersonWindow(person)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                if (string.IsNullOrWhiteSpace(person.DisplayName))
                    person.DisplayName = "Персона";

                _db.Persons.Add(person);
                _db.SaveChanges();

                LoadPersonsForSelectedClient();
                SelectedPerson = ClientPersons.FirstOrDefault(x => x.Id == person.Id);
            }
        }

        public void EditSelectedPerson()
        {
            if (!EnsureCanManageClientData()) return;

            if (SelectedPerson == null) return;

            var personFromDb = _db.Persons.First(x => x.Id == SelectedPerson.Id);

            var copy = new Person
            {
                Id = personFromDb.Id,
                ClientId = personFromDb.ClientId,
                DisplayName = personFromDb.DisplayName,
                Gender = personFromDb.Gender
            };

            var dialog = new PersonWindow(copy)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                personFromDb.DisplayName = string.IsNullOrWhiteSpace(copy.DisplayName) ? "Персона" : copy.DisplayName;
                personFromDb.Gender = copy.Gender;

                _db.SaveChanges();
                LoadPersonsForSelectedClient();
                SelectedPerson = ClientPersons.FirstOrDefault(x => x.Id == personFromDb.Id);
            }
        }

        public void DeleteSelectedPerson()
        {
            if (!EnsureCanManageClientData()) return;

            if (SelectedPerson == null) return;

            var usedInOrdersCount = _db.OrderServices
                .AsNoTracking()
                .Where(x => x.PersonId == SelectedPerson.Id)
                .Select(x => x.OrderId)
                .Distinct()
                .Count();

            if (usedInOrdersCount > 0)
            {
                UiMessages.Validation(
                    $"Персону \"{SelectedPerson.DisplayName}\" нельзя удалить, так как она используется в заказах: {usedInOrdersCount}.\n\n" +
                    "Это ограничение сохраняет историю заказов, связанные мерки и корректность отчётов.");
                return;
            }

            if (!UiMessages.ConfirmDelete($"Удалить персону \"{SelectedPerson.DisplayName}\"? Вместе с персоной будут удалены её наборы мерок."))
                return;

            try
            {
                var person = _db.Persons.First(x => x.Id == SelectedPerson.Id);
                _db.Persons.Remove(person);
                _db.SaveChanges();

                LoadPersonsForSelectedClient();
            }
            catch (DbUpdateException)
            {
                UiMessages.Error("Персону не удалось удалить, потому что она связана с другими данными.");
            }
        }

        public void AddMeasurementSet()
        {
            if (!EnsureCanManageClientData()) return;

            if (SelectedPerson == null)
            {
                UiMessages.Validation("Сначала выберите персону.");
                return;
            }

            var measurement = new MeasurementSet
            {
                PersonId = SelectedPerson.Id,
                TakenAt = DateTime.Today
            };

            var dialog = new MeasurementWindow(measurement)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                _db.MeasurementSets.Add(measurement);
                _db.SaveChanges();

                LoadMeasurementsForSelectedPerson();
                SelectedMeasurementSet = SelectedPersonMeasurementSets.FirstOrDefault(x => x.Id == measurement.Id);
            }
        }

        public void EditSelectedMeasurementSet()
        {
            if (!EnsureCanManageClientData()) return;

            if (SelectedMeasurementSet == null) return;

            var setFromDb = _db.MeasurementSets.First(x => x.Id == SelectedMeasurementSet.Id);

            var copy = new MeasurementSet
            {
                Id = setFromDb.Id,
                PersonId = setFromDb.PersonId,
                TakenAt = setFromDb.TakenAt,

                Height = setFromDb.Height,
                Chest = setFromDb.Chest,
                Waist = setFromDb.Waist,
                Hips = setFromDb.Hips,
                ShoulderWidth = setFromDb.ShoulderWidth,
                Sleeve = setFromDb.Sleeve,
                Inseam = setFromDb.Inseam,
                Outseam = setFromDb.Outseam,
                ProductLength = setFromDb.ProductLength,

                Neck = setFromDb.Neck,
                Wrist = setFromDb.Wrist,
                Bicep = setFromDb.Bicep,
                Thigh = setFromDb.Thigh,
                Knee = setFromDb.Knee,
                Ankle = setFromDb.Ankle,
                Rise = setFromDb.Rise,

                Comment = setFromDb.Comment
            };

            var dialog = new MeasurementWindow(copy)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                setFromDb.TakenAt = copy.TakenAt;
                setFromDb.Height = copy.Height;
                setFromDb.Chest = copy.Chest;
                setFromDb.Waist = copy.Waist;
                setFromDb.Hips = copy.Hips;
                setFromDb.ShoulderWidth = copy.ShoulderWidth;
                setFromDb.Sleeve = copy.Sleeve;
                setFromDb.Inseam = copy.Inseam;
                setFromDb.Outseam = copy.Outseam;
                setFromDb.ProductLength = copy.ProductLength;

                setFromDb.Neck = copy.Neck;
                setFromDb.Wrist = copy.Wrist;
                setFromDb.Bicep = copy.Bicep;
                setFromDb.Thigh = copy.Thigh;
                setFromDb.Knee = copy.Knee;
                setFromDb.Ankle = copy.Ankle;
                setFromDb.Rise = copy.Rise;

                setFromDb.Comment = copy.Comment;

                _db.SaveChanges();
                LoadMeasurementsForSelectedPerson();
                SelectedMeasurementSet = SelectedPersonMeasurementSets.FirstOrDefault(x => x.Id == setFromDb.Id);
            }
        }

        public void DeleteSelectedMeasurementSet()
        {
            if (!EnsureCanManageClientData()) return;

            if (SelectedMeasurementSet == null) return;

            var usedInOrdersCount = _db.OrderServices
                .AsNoTracking()
                .Where(x => x.MeasurementSetId == SelectedMeasurementSet.Id)
                .Select(x => x.OrderId)
                .Distinct()
                .Count();

            if (usedInOrdersCount > 0)
            {
                UiMessages.Validation(
                    $"Выбранный набор мерок нельзя удалить, так как он используется в заказах: {usedInOrdersCount}.\n\n" +
                    "Это ограничение не позволяет оставить заказ на пошив без связанных мерок.");
                return;
            }

            if (!UiMessages.ConfirmDelete("Удалить выбранный набор мерок?"))
                return;

            try
            {
                var set = _db.MeasurementSets.First(x => x.Id == SelectedMeasurementSet.Id);
                _db.MeasurementSets.Remove(set);
                _db.SaveChanges();

                LoadMeasurementsForSelectedPerson();
            }
            catch (DbUpdateException)
            {
                UiMessages.Error("Набор мерок не удалось удалить, потому что он связан с другими данными.");
            }
        }
    }
}