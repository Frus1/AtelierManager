namespace AtelierManager.ViewModels
{
    public class MaterialWindowViewModel : BaseViewModel
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private double _quantity;
        public double Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); }
        }

        private string _unit = "шт";
        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }

        private double _price;
        public double Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        public bool Validate(out string error)
        {
            Name = Name?.Trim() ?? "";
            Unit = Unit?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(Name))
            {
                error = "Введите название материала.";
                return false;
            }

            if (Quantity < 0)
            {
                error = "Количество не может быть отрицательным.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Unit))
            {
                error = "Укажите единицу измерения.";
                return false;
            }

            if (Price < 0)
            {
                error = "Цена не может быть отрицательной.";
                return false;
            }

            error = "";
            return true;
        }
    }
}