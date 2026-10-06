using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierManager.ViewModels
{
    public class ExtraElementWindowViewModel : BaseViewModel
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private double _defaultPrice;
        public double DefaultPrice
        {
            get => _defaultPrice;
            set { _defaultPrice = value; OnPropertyChanged(); }
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

            if (string.IsNullOrWhiteSpace(Name))
            {
                error = "Введите название доп. элемента.";
                return false;
            }

            if (DefaultPrice < 0)
            {
                error = "Цена не может быть отрицательной.";
                return false;
            }

            error = "";
            return true;
        }
    }
}
