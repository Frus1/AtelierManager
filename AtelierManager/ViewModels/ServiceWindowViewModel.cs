using System.Collections.ObjectModel;

namespace AtelierManager.ViewModels
{
    public class ServiceWindowViewModel : BaseViewModel
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private string _serviceType = "Ремонт";
        public string ServiceType
        {
            get => _serviceType;
            set { _serviceType = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> ServiceTypes { get; } =
            new ObservableCollection<string> { "Пошив", "Ремонт" };

        private double _basePrice;
        public double BasePrice
        {
            get => _basePrice;
            set { _basePrice = value; OnPropertyChanged(); }
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public bool Validate(out string error)
        {
            Name = Name?.Trim() ?? "";
            ServiceType = ServiceType?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(Name))
            {
                error = "Введите название услуги.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ServiceType))
            {
                error = "Выберите тип услуги.";
                return false;
            }

            if (BasePrice < 0)
            {
                error = "Базовая цена не может быть отрицательной.";
                return false;
            }

            error = "";
            return true;
        }
    }
}