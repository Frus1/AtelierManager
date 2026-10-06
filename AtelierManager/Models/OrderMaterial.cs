using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AtelierManager.Models
{
    public class OrderMaterial : INotifyPropertyChanged
    {
        private int _materialId;
        private double _qtyUsed;
        private double _unitPrice;
        private double _total;
        private Material? _material;

        public int Id { get; set; }
        public int OrderId { get; set; }

        public int MaterialId
        {
            get => _materialId;
            set
            {
                if (_materialId == value) return;
                _materialId = value;
                OnPropertyChanged();
            }
        }

        public double QtyUsed
        {
            get => _qtyUsed;
            set
            {
                if (System.Math.Abs(_qtyUsed - value) < 0.000001) return;
                _qtyUsed = value;
                OnPropertyChanged();
                RecalcTotal();
            }
        }

        public double UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (System.Math.Abs(_unitPrice - value) < 0.000001) return;
                _unitPrice = value;
                OnPropertyChanged();
                RecalcTotal();
            }
        }

        public double Total
        {
            get => _total;
            set
            {
                if (System.Math.Abs(_total - value) < 0.000001) return;
                _total = value;
                OnPropertyChanged();
            }
        }

        public Order? Order { get; set; }

        public Material? Material
        {
            get => _material;
            set
            {
                if (_material == value) return;
                _material = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaterialName));
            }
        }

        public string MaterialName => Material?.Name ?? "—";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void RecalcTotal()
        {
            Total = QtyUsed * UnitPrice;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
