using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AtelierManager.Models
{
    public class Order : INotifyPropertyChanged
    {
        private double _totalCost;

        public int Id { get; set; }
        public int ClientId { get; set; }
        public int StatusId { get; set; }
        public int? EmployeeId { get; set; }

        public DateTime IntakeDate { get; set; }
        public DateTime? DueDate { get; set; }

        public string? Notes { get; set; }

        public string MaterialSource { get; set; } = "Клиент";
        public string? MaterialComment { get; set; }

        public double TotalCost
        {
            get => _totalCost;
            set
            {
                if (Math.Abs(_totalCost - value) < 0.000001) return;
                _totalCost = value;
                OnPropertyChanged();
            }
        }

        public Client? Client { get; set; }
        public Status? Status { get; set; }
        public Employee? Employee { get; set; }

        public ICollection<OrderService> Lines { get; set; } = new List<OrderService>();
        public ICollection<OrderMaterial> OrderMaterials { get; set; } = new List<OrderMaterial>();

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
