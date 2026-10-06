using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierManager.Models
{
    public class OrderServiceExtra
    {
        public int Id { get; set; }
        public int OrderServiceId { get; set; }
        public int ExtraElementId { get; set; }

        public int Qty { get; set; } = 1;
        public double Price { get; set; }
        public double Total { get; set; }

        public OrderService? OrderService { get; set; }
        public ExtraElement? ExtraElement { get; set; }
    }
}
